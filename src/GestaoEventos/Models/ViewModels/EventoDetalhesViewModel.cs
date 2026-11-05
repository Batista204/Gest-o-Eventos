using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class EventoDetalhesViewModel
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Local { get; set; } = string.Empty;
    public DateOnly DataEvento { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFim { get; set; }
    public decimal CargaHoraria { get; set; }
    public int LimiteVagas { get; set; }
    public DateTime InicioInscricao { get; set; }
    public DateTime FimInscricao { get; set; }
    public StatusEvento Status { get; set; }
    public string OrganizadorNome { get; set; } = string.Empty;

    public int VagasOcupadas { get; set; }       // inscrições ativas
    public int TotalInscricoes { get; set; }     // inclui canceladas (histórico, RN05)

    public bool PodeEditar => Status is StatusEvento.Planejado or StatusEvento.Aberto;
    public bool PodeExcluir => TotalInscricoes == 0;
}