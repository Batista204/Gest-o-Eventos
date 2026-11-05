using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class EventoListaViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int LimiteVagas { get; set; }
    public int VagasOcupadas { get; set; }
    public StatusEvento Status { get; set; }
}