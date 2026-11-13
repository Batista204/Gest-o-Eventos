using System.Text;
using System.Text.RegularExpressions;
using GestaoEventos.Models.Enums;
using GestaoEventos.Services;
using GestaoEventos.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Infrastructure;
using Xunit;

namespace GestaoEventos.Tests;

public class CertificadoTests
{
    // 31 símbolos, sem 0, O, 1, I e L (RN32)
    private static readonly Regex FormatoCodigo =
        new(@"^[A-HJKMNP-Z2-9]{4}-[A-HJKMNP-Z2-9]{4}-[A-HJKMNP-Z2-9]{4}$");

    private static CertificadoService NovoServico(GestaoEventos.Data.AppDbContext db) =>
        new(db, Options.Create(new InstituicaoOptions { Nome = "UNIDESC" }), new AmbienteFalso());

    [Fact]
    public async Task Libera_somente_para_presentes_com_codigos_unicos_e_no_formato_correto()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 10, 3, StatusEvento.Encerrado, hoje: true);
        var inscricoes = await Cenario.InscreverAsync(db, cenario, true, true, false);   // o terceiro está ausente

        var servico = NovoServico(db);
        var resultado = await servico.LiberarAsync(cenario.Evento.Id);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.Quantidade);

        await using var leitura = banco.NovoContexto();
        var certificados = await leitura.Certificados.AsNoTracking().ToListAsync();

        Assert.Equal(2, certificados.Count);
        Assert.All(certificados, c => Assert.Matches(FormatoCodigo, c.CodigoVerificacao));
        Assert.Equal(2, certificados.Select(c => c.CodigoVerificacao).Distinct().Count());
        Assert.DoesNotContain(certificados, c => c.InscricaoId == inscricoes[2].Id);     // o ausente não recebe

        // RN31: não libera uma segunda vez
        var segunda = await servico.LiberarAsync(cenario.Evento.Id);
        Assert.False(segunda.Sucesso);
        Assert.Contains("já foram liberados", segunda.Erros[0].Mensagem);
        Assert.Equal(2, await leitura.Certificados.CountAsync());
    }

    [Fact]
    public async Task Nao_libera_certificados_de_evento_que_nao_foi_encerrado()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 10, 1, StatusEvento.Aberto, hoje: true);
        await Cenario.InscreverAsync(db, cenario, true);

        var resultado = await NovoServico(db).LiberarAsync(cenario.Evento.Id);

        Assert.False(resultado.Sucesso);
        Assert.Contains("depois que o evento for encerrado", resultado.Erros[0].Mensagem);
        Assert.Equal(0, await db.Certificados.CountAsync());
    }

    [Fact]
    public async Task Nao_libera_quando_ninguem_esta_presente()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 10, 2, StatusEvento.Encerrado, hoje: true);
        await Cenario.InscreverAsync(db, cenario, false, null);   // ausente e sem registro

        var resultado = await NovoServico(db).LiberarAsync(cenario.Evento.Id);

        Assert.False(resultado.Sucesso);
        Assert.Contains("elegível", resultado.Erros[0].Mensagem);
    }

    [Fact]
    public void Gera_um_pdf_valido()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var dados = new DadosCertificado
        {
            Nome = "Maria da Silva",
            Titulo = "Semana Acadêmica",
            DataEvento = new DateOnly(2026, 11, 6),
            CargaHoraria = 4,
            Codigo = "ABCD-EFGH-JKMN",
            DataEmissao = DateTime.UtcNow
        };

        var pdf = CertificadoPdfGenerator.Gerar(
            dados, new InstituicaoOptions { Nome = "UNIDESC" }, null, "http://localhost/Certificado/Validar");

        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task Validacao_publica_aceita_o_codigo_digitado_de_varios_jeitos_e_nega_o_invalido()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 10, 1, StatusEvento.Encerrado, hoje: true);
        await Cenario.InscreverAsync(db, cenario, true);

        var servico = NovoServico(db);
        Assert.True((await servico.LiberarAsync(cenario.Evento.Id)).Sucesso);

        var codigo = (await db.Certificados.AsNoTracking().SingleAsync()).CodigoVerificacao;

        // Minúsculas e sem hífens também funcionam
        var valido = await servico.ValidarAsync(codigo.Replace("-", "").ToLowerInvariant());
        Assert.True(valido.Encontrado);
        Assert.Equal("Participante 01", valido.Nome);

        // Código inexistente, vazio e lixo recebem o mesmo resultado
        Assert.False((await servico.ValidarAsync("AAAA-BBBB-CCCC")).Encontrado);
        Assert.False((await servico.ValidarAsync("")).Encontrado);
        Assert.False((await servico.ValidarAsync("'; DROP TABLE Certificados;--")).Encontrado);

        // Certificado invalidado deixa de validar
        await db.Certificados.ExecuteUpdateAsync(s => s.SetProperty(c => c.Valido, false));
        Assert.False((await servico.ValidarAsync(codigo)).Encontrado);
    }
}