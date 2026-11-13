using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestaoEventos.Tests;

public class PresencaTests
{
    [Fact]
    public async Task Nao_permite_registrar_presenca_antes_da_data_do_evento()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Aberto, hoje: false);
        var inscricoes = await Cenario.InscreverAsync(db, cenario);

        var itens = new List<PresencaItemViewModel> { new() { InscricaoId = inscricoes[0].Id, Presente = true } };
        var resultado = await new PresencaService(db).SalvarAsync(cenario.Evento.Id, itens, cenario.Organizador.Id);

        Assert.False(resultado.Sucesso);
        Assert.Contains("a partir da data", resultado.Erros[0].Mensagem);
        Assert.Equal(0, await db.Presencas.CountAsync());
    }

    [Fact]
    public async Task Nao_permite_alterar_presenca_depois_de_encerrado()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var cenario = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Encerrado, hoje: true);
        var inscricoes = await Cenario.InscreverAsync(db, cenario, false);

        var itens = new List<PresencaItemViewModel> { new() { InscricaoId = inscricoes[0].Id, Presente = true } };
        var resultado = await new PresencaService(db).SalvarAsync(cenario.Evento.Id, itens, cenario.Organizador.Id);

        Assert.False(resultado.Sucesso);
        Assert.Contains("encerrado", resultado.Erros[0].Mensagem);

        await using var leitura = banco.NovoContexto();
        Assert.False((await leitura.Presencas.AsNoTracking().SingleAsync()).Presente);   // continua ausente
    }

    [Fact]
    public async Task Registra_a_presenca_e_ignora_inscricoes_de_outro_evento()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        await using var db = banco.NovoContexto();

        var a = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Aberto, hoje: true);
        var b = await Cenario.CriarEventoAsync(db, 5, 1, StatusEvento.Aberto, hoje: true);
        var inscA = await Cenario.InscreverAsync(db, a);
        var inscB = await Cenario.InscreverAsync(db, b);

        // O formulário "adulterado" inclui a inscrição de outro evento
        var itens = new List<PresencaItemViewModel>
        {
            new() { InscricaoId = inscA[0].Id, Presente = true },
            new() { InscricaoId = inscB[0].Id, Presente = true }
        };

        var resultado = await new PresencaService(db).SalvarAsync(a.Evento.Id, itens, a.Organizador.Id);
        Assert.True(resultado.Sucesso);

        await using var leitura = banco.NovoContexto();
        Assert.True(await leitura.Presencas.AnyAsync(p => p.InscricaoId == inscA[0].Id && p.Presente));
        Assert.False(await leitura.Presencas.AnyAsync(p => p.InscricaoId == inscB[0].Id));   // intocada
    }
}