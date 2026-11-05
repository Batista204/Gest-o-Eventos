using GestaoEventos.Data;
using GestaoEventos.Models.Entities;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Services.Implementations;

public class EventoService : IEventoService
{
    private readonly AppDbContext _db;

    public EventoService(AppDbContext db) => _db = db;

    // ---------- Consultas ----------

    public async Task<List<EventoListaViewModel>> ListarAsync(StatusEvento? status)
    {
        var consulta = _db.Eventos.AsNoTracking();

        if (status.HasValue)
            consulta = consulta.Where(e => e.Status == status.Value);

        return await consulta
            .OrderByDescending(e => e.DataEvento)
            .ThenByDescending(e => e.HoraInicio)
            .Select(e => new EventoListaViewModel
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Local = e.Local,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                LimiteVagas = e.LimiteVagas,
                Status = e.Status,
                // Vagas ocupadas é calculado, nunca armazenado
                VagasOcupadas = e.Inscricoes.Count(i => i.Status == StatusInscricao.Ativa)
            })
            .ToListAsync();
    }

    public async Task<EventoDetalhesViewModel?> ObterDetalhesAsync(int id)
    {
        return await _db.Eventos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventoDetalhesViewModel
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Descricao = e.Descricao,
                Local = e.Local,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                CargaHoraria = e.CargaHoraria,
                LimiteVagas = e.LimiteVagas,
                InicioInscricao = e.InicioInscricao,
                FimInscricao = e.FimInscricao,
                Status = e.Status,
                OrganizadorNome = e.Organizador.NomeCompleto,
                VagasOcupadas = e.Inscricoes.Count(i => i.Status == StatusInscricao.Ativa),
                TotalInscricoes = e.Inscricoes.Count()
            })
            .FirstOrDefaultAsync();
    }

    public async Task<EventoFormViewModel?> ObterParaEdicaoAsync(int id)
    {
        return await _db.Eventos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EventoFormViewModel
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Descricao = e.Descricao,
                Local = e.Local,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                CargaHoraria = e.CargaHoraria,
                LimiteVagas = e.LimiteVagas,
                InicioInscricao = e.InicioInscricao,
                FimInscricao = e.FimInscricao,
                Status = e.Status,
                RowVersion = e.RowVersion
            })
            .FirstOrDefaultAsync();
    }

    // ---------- Criação ----------

    public async Task<Resultado> CriarAsync(EventoFormViewModel modelo, string organizadorId)
    {
        var resultado = ValidarRegras(modelo);

        if (modelo.DataEvento is { } data && data < DateOnly.FromDateTime(DateTime.Today))
            resultado.Adicionar("A data do evento não pode estar no passado.", nameof(modelo.DataEvento));

        if (!resultado.Sucesso)
            return resultado;

        var evento = new Evento
        {
            OrganizadorId = organizadorId,
            Status = StatusEvento.Planejado   // todo evento nasce planejado (RN04)
        };
        Aplicar(evento, modelo);

        _db.Eventos.Add(evento);
        await _db.SaveChangesAsync();

        return Resultado.Ok(evento.Id);
    }

    // ---------- Edição ----------

    public async Task<Resultado> AtualizarAsync(EventoFormViewModel modelo)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == modelo.Id);
        if (evento is null)
            return Resultado.Falha("Evento não encontrado.");

        // RN25: eventos encerrados ou cancelados não são editáveis
        if (evento.Status is StatusEvento.Encerrado or StatusEvento.Cancelado)
            return Resultado.Falha("Eventos encerrados ou cancelados não podem ser editados.");

        var resultado = ValidarRegras(modelo);

        // RN03: o limite não pode ficar abaixo das inscrições ativas
        if (modelo.LimiteVagas is { } limite)
        {
            var ativas = await _db.Inscricoes.CountAsync(i =>
                i.EventoId == evento.Id && i.Status == StatusInscricao.Ativa);

            if (limite < ativas)
                resultado.Adicionar(
                    $"O limite de vagas não pode ser menor que o número de inscrições ativas ({ativas}).",
                    nameof(modelo.LimiteVagas));
        }

        if (!resultado.Sucesso)
            return resultado;

        Aplicar(evento, modelo);

        // Se alguém alterou o evento depois que o formulário foi aberto, o EF detecta e recusa
        if (modelo.RowVersion is not null)
            _db.Entry(evento).Property(e => e.RowVersion).OriginalValue = modelo.RowVersion;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Resultado.Falha(
                "Este evento foi alterado por outra pessoa enquanto você editava. Recarregue a página e tente novamente.");
        }

        return Resultado.Ok(evento.Id);
    }

    // ---------- Status ----------

    public async Task<Resultado> AlterarStatusAsync(int id, StatusEvento novoStatus)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
            return Resultado.Falha("Evento não encontrado.");

        if (!TransicaoPermitida(evento.Status, novoStatus))
            return Resultado.Falha($"Não é possível alterar o status de {evento.Status} para {novoStatus}.");

        // RN24: só se encerra um evento a partir da data de realização
        if (novoStatus == StatusEvento.Encerrado && evento.DataEvento > DateOnly.FromDateTime(DateTime.Today))
            return Resultado.Falha("O evento só pode ser encerrado a partir da data de sua realização.");

        evento.Status = novoStatus;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Resultado.Falha("O evento foi alterado por outra pessoa. Recarregue a página e tente novamente.");
        }

        return Resultado.Ok(evento.Id);
    }

    // ---------- Exclusão ----------

    public async Task<Resultado> ExcluirAsync(int id)
    {
        var evento = await _db.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
            return Resultado.Falha("Evento não encontrado.");

        // RN05: com qualquer inscrição (inclusive cancelada), só é possível cancelar o evento
        var temInscricoes = await _db.Inscricoes.AnyAsync(i => i.EventoId == id);
        if (temInscricoes)
            return Resultado.Falha(
                "Este evento possui inscrições e não pode ser excluído. Use o cancelamento para preservar o histórico.");

        _db.Eventos.Remove(evento);
        await _db.SaveChangesAsync();

        return Resultado.Ok();
    }

    // ---------- Regras auxiliares ----------

    // Regras que dependem da combinação de campos (RN01 e RN02)
    private static Resultado ValidarRegras(EventoFormViewModel m)
    {
        if (m.DataEvento is null || m.HoraInicio is null || m.HoraFim is null ||
            m.InicioInscricao is null || m.FimInscricao is null ||
            m.CargaHoraria is null || m.LimiteVagas is null)
            return Resultado.Falha("Preencha todos os campos obrigatórios.");

        var resultado = new Resultado();

        if (m.HoraFim <= m.HoraInicio)
            resultado.Adicionar("O horário de término deve ser posterior ao de início.", nameof(m.HoraFim));

        if (m.FimInscricao < m.InicioInscricao)
            resultado.Adicionar("O fim das inscrições não pode ser anterior ao início.", nameof(m.FimInscricao));

        var inicioDoEvento = m.DataEvento.Value.ToDateTime(m.HoraInicio.Value);
        if (m.FimInscricao > inicioDoEvento)
            resultado.Adicionar("O período de inscrição deve terminar até o início do evento.", nameof(m.FimInscricao));

        return resultado;
    }

    // Ciclo de vida do evento (RN04)
    private static bool TransicaoPermitida(StatusEvento atual, StatusEvento novo) => (atual, novo) switch
    {
        (StatusEvento.Planejado, StatusEvento.Aberto) => true,
        (StatusEvento.Planejado, StatusEvento.Cancelado) => true,
        (StatusEvento.Aberto, StatusEvento.Encerrado) => true,
        (StatusEvento.Aberto, StatusEvento.Cancelado) => true,
        _ => false
    };

    // Copia os dados do formulário para a entidade (campos já validados)
    private static void Aplicar(Evento evento, EventoFormViewModel m)
    {
        evento.Titulo = m.Titulo.Trim();
        evento.Descricao = m.Descricao.Trim();
        evento.Local = m.Local.Trim();
        evento.DataEvento = m.DataEvento!.Value;
        evento.HoraInicio = m.HoraInicio!.Value;
        evento.HoraFim = m.HoraFim!.Value;
        evento.CargaHoraria = m.CargaHoraria!.Value;
        evento.LimiteVagas = m.LimiteVagas!.Value;
        evento.InicioInscricao = m.InicioInscricao!.Value;
        evento.FimInscricao = m.FimInscricao!.Value;
    }
}