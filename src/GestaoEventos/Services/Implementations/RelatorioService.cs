using System.Text;
using GestaoEventos.Data;
using GestaoEventos.Models.Enums;
using GestaoEventos.Models.ViewModels;
using GestaoEventos.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Services.Implementations;

public class RelatorioService : IRelatorioService
{
    private readonly AppDbContext _db;

    public RelatorioService(AppDbContext db) => _db = db;

    // ---------- Relatório geral (RF25 a RF27) ----------

    public async Task<List<RelatorioEventoLinhaViewModel>> ResumoEventosAsync(StatusEvento? status)
    {
        var consulta = _db.Eventos.AsNoTracking();

        if (status.HasValue)
            consulta = consulta.Where(e => e.Status == status.Value);

        return await consulta
            .OrderByDescending(e => e.DataEvento).ThenByDescending(e => e.HoraInicio)
            .Select(e => new RelatorioEventoLinhaViewModel
            {
                EventoId = e.Id,
                Titulo = e.Titulo,
                DataEvento = e.DataEvento,
                Status = e.Status,
                LimiteVagas = e.LimiteVagas,
                InscritosAtivos = e.Inscricoes.Count(i => i.Status == StatusInscricao.Ativa),
                Cancelados = e.Inscricoes.Count(i => i.Status == StatusInscricao.Cancelada),
                Presentes = e.Inscricoes.Count(i =>
                    i.Status == StatusInscricao.Ativa && i.Presenca != null && i.Presenca.Presente),
                Ausentes = e.Inscricoes.Count(i =>
                    i.Status == StatusInscricao.Ativa && i.Presenca != null && !i.Presenca.Presente),
                CertificadosEmitidos = e.Inscricoes.Count(i => i.Certificado != null && i.Certificado.Valido)
            })
            .ToListAsync();
    }

    // ---------- Relatório de um evento ----------

    public async Task<RelatorioEventoDetalheViewModel?> DetalheEventoAsync(int eventoId)
    {
        var evento = await _db.Eventos.AsNoTracking()
            .Where(e => e.Id == eventoId)
            .Select(e => new RelatorioEventoDetalheViewModel
            {
                EventoId = e.Id,
                Titulo = e.Titulo,
                Local = e.Local,
                DataEvento = e.DataEvento,
                HoraInicio = e.HoraInicio,
                HoraFim = e.HoraFim,
                CargaHoraria = e.CargaHoraria,
                LimiteVagas = e.LimiteVagas,
                Status = e.Status,
                Cancelados = e.Inscricoes.Count(i => i.Status == StatusInscricao.Cancelada)
            })
            .FirstOrDefaultAsync();

        if (evento is null)
            return null;

        // RN12: só inscrições ativas entram na lista
        evento.Itens = await _db.Inscricoes.AsNoTracking()
            .Where(i => i.EventoId == eventoId && i.Status == StatusInscricao.Ativa)
            .OrderBy(i => i.Usuario.NomeCompleto)
            .Select(i => new CertificadoItemViewModel
            {
                InscricaoId = i.Id,
                Nome = i.Usuario.NomeCompleto,
                Email = i.Usuario.Email ?? string.Empty,
                Matricula = i.Usuario.Matricula,
                Presente = i.Presenca == null ? (bool?)null : i.Presenca.Presente,
                Codigo = i.Certificado != null && i.Certificado.Valido ? i.Certificado.CodigoVerificacao : null,
                DataEmissao = i.Certificado == null ? (DateTime?)null : i.Certificado.DataEmissao
            })
            .ToListAsync();

        return evento;
    }

    // ---------- Exportação CSV ----------

    public async Task<ArquivoCsv?> GerarCsvAsync(int eventoId)
    {
        var detalhe = await DetalheEventoAsync(eventoId);
        if (detalhe is null)
            return null;

        var sb = new StringBuilder();
        sb.AppendLine("Nome;E-mail;Matrícula;Presença;Certificado");

        foreach (var item in detalhe.Itens)
        {
            var presenca = item.Presente switch
            {
                true => "Presente",
                false => "Ausente",
                _ => "Sem registro"
            };

            sb.AppendLine(string.Join(';',
                CelulaCsv(item.Nome),
                CelulaCsv(item.Email),
                CelulaCsv(item.Matricula ?? string.Empty),
                CelulaCsv(presenca),
                CelulaCsv(item.Codigo ?? string.Empty)));
        }

        // UTF-8 com BOM: o Excel reconhece os acentos
        var preambulo = new UTF8Encoding(true).GetPreamble();
        var corpo = Encoding.UTF8.GetBytes(sb.ToString());
        var bytes = new byte[preambulo.Length + corpo.Length];
        preambulo.CopyTo(bytes, 0);
        corpo.CopyTo(bytes, preambulo.Length);

        return new ArquivoCsv(bytes, $"evento-{eventoId}-participantes.csv");
    }

    // RN36: neutraliza fórmulas (=, +, -, @) e protege aspas, separador e quebras de linha
    private static string CelulaCsv(string valor)
    {
        if (valor.Length > 0 && "=+-@\t\r".Contains(valor[0]))
            valor = "'" + valor;

        if (valor.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
            valor = "\"" + valor.Replace("\"", "\"\"") + "\"";

        return valor;
    }

    // ---------- Histórico do participante (RF28) ----------

    public async Task<HistoricoViewModel> HistoricoAsync(string usuarioId)
    {
        // RN23 e RN35: só os próprios dados, de eventos encerrados com inscrição ativa
        var itens = await _db.Inscricoes.AsNoTracking()
            .Where(i => i.UsuarioId == usuarioId
                        && i.Status == StatusInscricao.Ativa
                        && i.Evento.Status == StatusEvento.Encerrado)
            .OrderByDescending(i => i.Evento.DataEvento).ThenByDescending(i => i.Evento.HoraInicio)
            .Select(i => new HistoricoItemViewModel
            {
                InscricaoId = i.Id,
                EventoId = i.EventoId,
                Titulo = i.Evento.Titulo,
                Local = i.Evento.Local,
                DataEvento = i.Evento.DataEvento,
                CargaHoraria = i.Evento.CargaHoraria,
                Presente = i.Presenca == null ? (bool?)null : i.Presenca.Presente,
                CertificadosLiberados = i.Evento.CertificadosLiberadosEm != null,
                CertificadoCodigo = i.Certificado != null && i.Certificado.Valido
                    ? i.Certificado.CodigoVerificacao : null
            })
            .ToListAsync();

        return new HistoricoViewModel { Itens = itens };
    }
}