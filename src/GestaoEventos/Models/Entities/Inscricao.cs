using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.Entities;

public class Inscricao
{
    public int Id { get; set; }

    public int EventoId { get; set; }
    public Evento Evento { get; set; } = null!;

    public string UsuarioId { get; set; } = string.Empty;
    public Usuario Usuario { get; set; } = null!;

    public DateTime DataInscricao { get; set; } = DateTime.UtcNow;
    public StatusInscricao Status { get; set; } = StatusInscricao.Ativa;
    public DateTime? DataCancelamento { get; set; }

    public Presenca? Presenca { get; set; }
    public Certificado? Certificado { get; set; }
}