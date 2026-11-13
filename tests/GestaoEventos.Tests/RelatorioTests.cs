using System.Text;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;
using GestaoEventos.Services.Implementations;
using Xunit;

namespace GestaoEventos.Tests;

public class RelatorioTests
{
    [Fact]
    public async Task Csv_neutraliza_formulas_para_nao_executar_no_excel()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Encerrado, hoje: true);
        cenario.Participantes[0].NomeCompleto = "=1+1";
        await Cenario.InscreverAsync(db, cenario, true);

        var arquivo = await new RelatorioService(db).GerarCsvAsync(cenario.Evento.Id);

        Assert.NotNull(arquivo);
        Assert.Equal(0xEF, arquivo!.Conteudo[0]);   // UTF-8 com BOM: o Excel mostra os acentos certos

        var texto = Encoding.UTF8.GetString(arquivo.Conteudo);
        Assert.Contains("'=1+1;", texto);           // o apóstrofo impede a execução da fórmula (RN36)
    }

    [Fact]
    public async Task Historico_lista_so_eventos_encerrados_e_soma_horas_de_certificados_validos()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        // O mesmo participante em quatro eventos
        var e1 = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Encerrado, hoje: true);
        var e2 = await Cenario.CriarEventoAsync(db, 5, 0, StatusEvento.Encerrado, hoje: true);
        var e3 = await Cenario.CriarEventoAsync(db, 5, 0, StatusEvento.Aberto, hoje: true);
        var e4 = await Cenario.CriarEventoAsync(db, 5, 0, StatusEvento.Encerrado, hoje: true);
        var usuario = e1.Participantes[0];

        var insc1 = (await Cenario.InscreverAsync(db, e1, true))[0];
        var insc2 = new Inscricao { EventoId = e2.Evento.Id, UsuarioId = usuario.Id, Status = StatusInscricao.Ativa };
        var insc3 = new Inscricao { EventoId = e3.Evento.Id, UsuarioId = usuario.Id, Status = StatusInscricao.Ativa };
        var insc4 = new Inscricao
        {
            EventoId = e4.Evento.Id, UsuarioId = usuario.Id,
            Status = StatusInscricao.Cancelada, DataCancelamento = DateTime.UtcNow
        };
        db.Inscricoes.AddRange(insc2, insc3, insc4);
        await db.SaveChangesAsync();

        db.Certificados.Add(new Certificado { InscricaoId = insc1.Id, CodigoVerificacao = "AAAA-AAAA-AAAA", Valido = true });
        db.Certificados.Add(new Certificado { InscricaoId = insc2.Id, CodigoVerificacao = "BBBB-BBBB-BBBB", Valido = false });
        await db.SaveChangesAsync();

        var historico = await new RelatorioService(db).HistoricoAsync(usuario.Id);

        Assert.Equal(2, historico.Itens.Count);      // só o e1 e o e2: encerrados e com inscrição ativa (RN35)
        Assert.Equal(1, historico.CertificadosEmitidos);
        Assert.Equal(2m, historico.HorasCertificadas);   // só o certificado válido conta (2 h)
    }
}