using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class CertificadoResumoViewModel
{
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public StatusEvento Status { get; set; }

    public DateTime? LiberadosEm { get; set; }   // UTC
    public bool PodeLiberar { get; set; }
    public string? Aviso { get; set; }

    public List<CertificadoItemViewModel> Itens { get; set; } = new();

    public int TotalInscritos => Itens.Count;
    public int Elegiveis => Itens.Count(i => i.Elegivel);
    public int NaoElegiveis => Itens.Count(i => !i.Elegivel);
    public int Emitidos => Itens.Count(i => i.Codigo is not null);
}