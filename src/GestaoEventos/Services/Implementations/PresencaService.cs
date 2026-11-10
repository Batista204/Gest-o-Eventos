using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Services.Implementations;

public class PresencaService : IPresencaService
{
    private readonly AppDbContext _db;

    public PresencaService(AppDbContext db) => _db = db;

    // ---------- Consulta ----------

    public async Task<PresencaListaViewModel?> ObterListaAsync(int eventoId)
    {
        var evento = await _db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento is null)
            return null;

        // RN12: só inscrições ativas entram na lista
        var itens = await _db.Inscricoes
            .AsNoTracking()
            .Where(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa)
            .OrderBy(i => i.Usuario.NomeCompleto)
            .Select(i => new PresencaItemViewModel
            {
                InscricaoId = i.Id,
                Nome = i.Usuario.NomeCompleto,
                Email = i.Usuario.Email ?? string.Empty,
                Matricula = i.Usuario.Matricula,
                PresenteRegistrado = i.Presenca == null ? (bool?)null : i.Presenca.Presente,
                Presente = i.Presenca != null && i.Presenca.Presente
            })
            .ToListAsync();

        var aviso = MotivoBloqueio(evento);

        return new PresencaListaViewModel
        {
            EventoId = evento.Id,
            Titulo = evento.Titulo,
            DataEvento = evento.DataEvento,
            HoraInicio = evento.HoraInicio,
            Status = evento.Status,
            PodeRegistrar = aviso is null,
            Aviso = aviso,
            Itens = itens
        };
    }

    // ---------- Gravação ----------

    public async Task<Resultado> SalvarAsync(
        int eventoId, IReadOnlyList<PresencaItemViewModel> itens, string registradoPorId)
    {
        var evento = await _db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento is null)
            return Resultado.Falha("Evento não encontrado.");

        // RN15 e RN29: a regra é conferida de novo na hora de gravar, e não só na tela
        var bloqueio = MotivoBloqueio(evento);
        if (bloqueio is not null)
            return Resultado.Falha(bloqueio);

        // Só considera inscrições ativas DESTE evento, ignorando qualquer id estranho do formulário
        var inscricoes = await _db.Inscricoes
            .Include(i => i.Presenca)
            .Where(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa)
            .ToListAsync();

        // Se o mesmo id vier duas vezes, vale o último
        var enviados = itens
            .GroupBy(i => i.InscricaoId)
            .ToDictionary(g => g.Key, g => g.Last().Presente);

        var agora = DateTime.UtcNow;

        foreach (var inscricao in inscricoes)
        {
            // Inscrição feita depois que a tela foi aberta: mantém o estado atual
            if (!enviados.TryGetValue(inscricao.Id, out var presente))
                continue;

            if (inscricao.Presenca is null)
            {
                _db.Presencas.Add(new Presenca
                {
                    InscricaoId = inscricao.Id,
                    Presente = presente,
                    DataRegistro = agora,
                    RegistradaPorId = registradoPorId
                });
            }
            else if (inscricao.Presenca.Presente != presente)
            {
                inscricao.Presenca.Presente = presente;
                inscricao.Presenca.DataRegistro = agora;
                inscricao.Presenca.RegistradaPorId = registradoPorId;
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Ocorre, por exemplo, se dois administradores salvarem ao mesmo tempo
            return Resultado.Falha("Não foi possível salvar as presenças. Recarregue a página e tente novamente.");
        }

        return Resultado.Ok();
    }

    // ---------- Regras (usadas na tela e na gravação, para nunca divergirem) ----------

    private static string? MotivoBloqueio(Evento evento)
    {
        return evento.Status switch
        {
            StatusEvento.Cancelado => "O evento foi cancelado: a presença não pode ser registrada.",
            StatusEvento.Encerrado => "O evento foi encerrado: a presença não pode mais ser alterada.",
            StatusEvento.Planejado => "O evento ainda não foi aberto.",
            _ when evento.DataEvento > DateOnly.FromDateTime(DateTime.Today)
                => $"A presença só pode ser registrada a partir da data do evento ({evento.DataEvento:dd/MM/yyyy}).",
            _ => null
        };
    }
}