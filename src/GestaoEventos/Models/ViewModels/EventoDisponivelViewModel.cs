namespace GestaoEventos.Models.ViewModels;

public class EventoDisponivelViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public int LimiteVagas { get; set; }
    public int VagasOcupadas { get; set; }
    public bool JaInscrito { get; set; }

    public int VagasDisponiveis => Math.Max(0, LimiteVagas - VagasOcupadas);
}