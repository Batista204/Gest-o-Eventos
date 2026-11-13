using GestaoEventos.Models.Enums;
using GestaoEventos.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestaoEventos.Tests;

public class VagasSimultaneasTests
{
    private static readonly string[] FalhasAceitas =
    {
        "Vagas esgotadas.",
        "O sistema está com muitos acessos agora. Tente novamente em instantes."
    };

    [Fact]
    public async Task Dez_pessoas_disputando_tres_vagas_nunca_ultrapassam_o_limite()
    {
        await using var banco = await BancoDeTestes.CriarAsync();
        const int vagas = 3;

        await using var preparo = banco.NovoContexto();
        var cenario = await Cenario.CriarEventoAsync(preparo, vagas, participantes: 10);

        // As 10 inscrições disparam ao mesmo tempo, cada uma com seu contexto
        var tarefas = cenario.Participantes.Select(p => Task.Run(async () =>
        {
            await using var db = banco.NovoContexto();
            return await new InscricaoService(db).InscreverAsync(cenario.Evento.Id, p.Id);
        })).ToList();

        var resultados = await Task.WhenAll(tarefas);

        await using var leitura = banco.NovoContexto();
        var ativas = await leitura.Inscricoes.CountAsync(i =>
            i.EventoId == cenario.Evento.Id && i.Status == StatusInscricao.Ativa);

        // O limite nunca pode ser ultrapassado
        Assert.Equal(vagas, ativas);
        Assert.Equal(vagas, resultados.Count(r => r.Sucesso));

        // Quem ficou de fora recebeu uma recusa esperada, e não um erro inesperado
        Assert.All(resultados.Where(r => !r.Sucesso),
            r => Assert.Contains(r.Erros[0].Mensagem, FalhasAceitas));
    }

    [Fact]
    public async Task Mesmo_usuario_clicando_varias_vezes_gera_uma_unica_inscricao()
    {
        await using var banco = await BancoDeTestes.CriarAsync();

        await using var preparo = banco.NovoContexto();
        var cenario = await Cenario.CriarEventoAsync(preparo, vagas: 10, participantes: 1);
        var usuarioId = cenario.Participantes[0].Id;

        var tarefas = Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            await using var db = banco.NovoContexto();
            return await new InscricaoService(db).InscreverAsync(cenario.Evento.Id, usuarioId);
        })).ToList();

        var resultados = await Task.WhenAll(tarefas);

        await using var leitura = banco.NovoContexto();
        var total = await leitura.Inscricoes.CountAsync(i => i.EventoId == cenario.Evento.Id);

        Assert.Equal(1, total);
        Assert.Equal(1, resultados.Count(r => r.Sucesso));
    }

    [Fact]
    public async Task Cancelar_e_reinscrever_reativa_o_mesmo_registro()
    {
        await using var banco = await BancoDeTestes.CriarAsync();

        await using var preparo = banco.NovoContexto();
        var cenario = await Cenario.CriarEventoAsync(preparo, vagas: 5, participantes: 1);
        var usuarioId = cenario.Participantes[0].Id;

        await using var db = banco.NovoContexto();
        var servico = new InscricaoService(db);

        Assert.True((await servico.InscreverAsync(cenario.Evento.Id, usuarioId)).Sucesso);

        var inscricaoId = (await db.Inscricoes.AsNoTracking().SingleAsync()).Id;
        Assert.True((await servico.CancelarAsync(inscricaoId, usuarioId)).Sucesso);
        Assert.True((await servico.InscreverAsync(cenario.Evento.Id, usuarioId)).Sucesso);

        await using var leitura = banco.NovoContexto();
        var linhas = await leitura.Inscricoes.AsNoTracking().ToListAsync();

        Assert.Single(linhas);                                    // continua uma linha só (RN11)
        Assert.Equal(StatusInscricao.Ativa, linhas[0].Status);
        Assert.Null(linhas[0].DataCancelamento);
    }
}