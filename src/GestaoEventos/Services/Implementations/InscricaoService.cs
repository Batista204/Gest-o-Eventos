using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Services.Implementations;

public class InscricaoService : IInscricaoService
{
    private readonly AppDbContext _db;

    public InscricaoService(AppDbContext db) => _db = db;

    // ---------- Consultas ----------

    public async Task<List<EventoDisponivelViewModel>> ListarEventosAbertosAsync(string usuarioId)
    {
        return await _db.Eventos
            .AsNoTracking()
            .Where(e => e.Status == StatusEvento.Aberto)
            .OrderBy(e => e.DataEvento).ThenBy(e => e.HoraInicio)
            .Select(e => new EventoDisponivelViewModel
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Local = e.Local,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                LimiteVagas = e.LimiteVagas,
                VagasOcupadas = e.Inscricoes.Count(i => i.Status == StatusInscricao.Ativa),
                JaInscrito = e.Inscricoes.Any(i => i.UsuarioId == usuarioId && i.Status == StatusInscricao.Ativa)
            })
            .ToListAsync();
    }

    public async Task<EventoDisponivelDetalhesViewModel?> ObterEventoAsync(int eventoId, string usuarioId)
    {
        var evento = await _db.Eventos.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventoId);
        if (evento is null)
            return null;

        var minha = await _db.Inscricoes.AsNoTracking()
            .FirstOrDefaultAsync(i => i.EventoId == eventoId && i.UsuarioId == usuarioId);

        // RN26: o participante só vê eventos abertos ou aqueles em que já se inscreveu
        if (evento.Status != StatusEvento.Aberto && minha is null)
            return null;

        var ocupadas = await _db.Inscricoes
            .CountAsync(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa);

        var agora = DateTime.Now;
        var jaInscrito = minha?.Status == StatusInscricao.Ativa;
        var bloqueio = jaInscrito ? null : MotivoBloqueio(evento, ocupadas, agora);

        return new EventoDisponivelDetalhesViewModel
        {
            Id = evento.Id,
            Titulo = evento.Titulo,
            Descricao = evento.Descricao,
            Local = evento.Local,
            DataEvento = evento.DataEvento,
            HoraInicio = evento.HoraInicio,
            HoraFim = evento.HoraFim,
            CargaHoraria = evento.CargaHoraria,
            LimiteVagas = evento.LimiteVagas,
            VagasOcupadas = ocupadas,
            InicioInscricao = evento.InicioInscricao,
            FimInscricao = evento.FimInscricao,
            Status = evento.Status,
            JaInscrito = jaInscrito,
            InscricaoId = jaInscrito ? minha!.Id : null,
            PodeInscrever = !jaInscrito && bloqueio is null,
            PodeCancelar = jaInscrito && PodeCancelarInscricao(evento, agora),
            Aviso = bloqueio
        };
    }

    public async Task<List<MinhaInscricaoViewModel>> ListarMinhasAsync(string usuarioId)
    {
        var lista = await _db.Inscricoes
            .AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId)
            .OrderByDescending(i => i.Evento.DataEvento).ThenByDescending(i => i.Evento.HoraInicio)
            .Select(i => new MinhaInscricaoViewModel
            {
                InscricaoId = i.Id,
                EventoId = i.EventoId,
                Titulo = i.Evento.Titulo,
                Local = i.Evento.Local,
                DataEvento = i.Evento.DataEvento,
                HoraInicio = i.Evento.HoraInicio,
                StatusEvento = i.Evento.Status,
                StatusInscricao = i.Status,
                DataInscricao = i.DataInscricao,
                FimInscricao = i.Evento.FimInscricao,
                Presente = i.Presenca == null ? (bool?)null : i.Presenca.Presente
            })
            .ToListAsync();

        var agora = DateTime.Now;
        foreach (var item in lista)
        {
            item.PodeCancelar = item.StatusInscricao == StatusInscricao.Ativa
                && item.StatusEvento == StatusEvento.Aberto
                && agora <= item.FimInscricao;
        }

        return lista;
    }

    // ---------- Inscrição ----------

    public async Task<Resultado> InscreverAsync(int eventoId, string usuarioId)
    {
        const int maxTentativas = 3;

        for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
        {
            var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == eventoId);
            if (evento is null)
                return Resultado.Falha("Evento não encontrado.");

            var ocupadas = await _db.Inscricoes
                .CountAsync(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa);

            var existente = await _db.Inscricoes
                .FirstOrDefaultAsync(i => i.EventoId == eventoId && i.UsuarioId == usuarioId);

            // RN08: uma inscrição ativa por evento
            if (existente?.Status == StatusInscricao.Ativa)
                return Resultado.Falha("Você já está inscrito neste evento.");

            // RN07 e RN09: evento aberto, dentro do período e com vaga
            var bloqueio = MotivoBloqueio(evento, ocupadas, DateTime.Now);
            if (bloqueio is not null)
                return Resultado.Falha(bloqueio);

            if (existente is null)
            {
                _db.Inscricoes.Add(new Inscricao
                {
                    EventoId = eventoId,
                    UsuarioId = usuarioId,
                    DataInscricao = DateTime.UtcNow,
                    Status = StatusInscricao.Ativa
                });
            }
            else
            {
                // RN11: reativa a inscrição cancelada
                existente.Status = StatusInscricao.Ativa;
                existente.DataCancelamento = null;
                existente.DataInscricao = DateTime.UtcNow;
            }

            // Força um UPDATE no evento. Como o RowVersion é verificado nesse UPDATE, só uma
            // de duas inscrições simultâneas passa; a outra é desfeita por completo (RNF08).
            _db.Entry(evento).Property(e => e.Titulo).IsModified = true;

            try
            {
                await _db.SaveChangesAsync();
                return Resultado.Ok();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Outra inscrição aconteceu ao mesmo tempo: recontamos as vagas e tentamos de novo
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateException)
            {
                _db.ChangeTracker.Clear();
                return Resultado.Falha("Não foi possível concluir a inscrição. Verifique se você já está inscrito e tente novamente.");
            }
        }

        return Resultado.Falha("O sistema está com muitos acessos agora. Tente novamente em instantes.");
    }

    // ---------- Cancelamento ----------

    public async Task<Resultado> CancelarAsync(int inscricaoId, string usuarioId)
    {
        // RN23: só enxerga inscrições do próprio usuário
        var inscricao = await _db.Inscricoes
            .Include(i => i.Evento)
            .FirstOrDefaultAsync(i => i.Id == inscricaoId && i.UsuarioId == usuarioId);

        if (inscricao is null)
            return Resultado.Falha("Inscrição não encontrada.");

        if (inscricao.Status != StatusInscricao.Ativa)
            return Resultado.Falha("Esta inscrição já foi cancelada.");

        // RN10: só até o fim do período de inscrição
        if (!PodeCancelarInscricao(inscricao.Evento, DateTime.Now))
            return Resultado.Falha("O prazo para cancelar a inscrição terminou.");

        inscricao.Status = StatusInscricao.Cancelada;
        inscricao.DataCancelamento = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Resultado.Ok();
    }

    // ---------- Regras auxiliares (usadas na tela e na gravação, para nunca divergirem) ----------

    private static string? MotivoBloqueio(Evento evento, int ocupadas, DateTime agora)
    {
        if (evento.Status != StatusEvento.Aberto)
            return "As inscrições para este evento não estão abertas.";
        if (agora < evento.InicioInscricao)
            return $"As inscrições começam em {evento.InicioInscricao:dd/MM/yyyy HH:mm}.";
        if (agora > evento.FimInscricao)
            return "O período de inscrição foi encerrado.";
        if (ocupadas >= evento.LimiteVagas)
            return "Vagas esgotadas.";
        return null;
    }

    private static bool PodeCancelarInscricao(Evento evento, DateTime agora)
        => evento.Status == StatusEvento.Aberto && agora <= evento.FimInscricao;
}