using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class PresencaListaViewModel
{
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public StatusEvento Status { get; set; }

    public bool PodeRegistrar { get; set; }
    public string? Aviso { get; set; }   // motivo pelo qual a presença está bloqueada

    public List<PresencaItemViewModel> Itens { get; set; } = new();

    public int TotalInscritos => Itens.Count;
    public int Presentes => Itens.Count(i => i.PresenteRegistrado == true);
    public int Ausentes => Itens.Count(i => i.PresenteRegistrado == false);
    public int SemRegistro => Itens.Count(i => i.PresenteRegistrado is null);
}