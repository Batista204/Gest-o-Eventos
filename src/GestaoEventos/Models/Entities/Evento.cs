using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.Entities;

public class Evento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;

    // Evento de dia único (premissa P1)
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }

    public decimal CargaHoraria { get; set; }   // em horas
    public int LimiteVagas { get; set; }
    public DateTime InicioInscricao { get; set; }
    public DateTime FimInscricao { get; set; }
    public StatusEvento Status { get; set; } = StatusEvento.Planejado;

    // Preenchido quando o Admin libera os certificados (RN17)
    public DateTime? CertificadosLiberadosEm { get; set; }

    public string OrganizadorId { get; set; } = string.Empty;
    public Usuario Organizador { get; set; } = null!;

    // Controle de concorrência para as vagas (RNF08)
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();
}