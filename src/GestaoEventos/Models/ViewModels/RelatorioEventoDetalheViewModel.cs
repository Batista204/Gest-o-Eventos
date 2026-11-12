using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class RelatorioEventoDetalheViewModel
{
    public int EventoId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public decimal CargaHoraria { get; set; }
    public int LimiteVagas { get; set; }
    public StatusEvento Status { get; set; }
    public int Cancelados { get; set; }

    public List<CertificadoItemViewModel> Itens { get; set; } = new();

    public int TotalInscritos => Itens.Count;
    public int Presentes => Itens.Count(i => i.Presente == true);
    public int Ausentes => Itens.Count(i => i.Presente == false);
    public int SemRegistro => Itens.Count(i => i.Presente is null);
    public int Emitidos => Itens.Count(i => i.Codigo is not null);
}