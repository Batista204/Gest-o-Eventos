using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class MinhaInscricaoViewModel
{
    public int InscricaoId { get; set; }
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public StatusEvento StatusEvento { get; set; }
    public StatusInscricao StatusInscricao { get; set; }
    public DateTime DataInscricao { get; set; }
    public DateTime FimInscricao { get; set; }
    public bool PodeCancelar { get; set; }

    // null = presença ainda não registrada
    public bool? Presente { get; set; }
}