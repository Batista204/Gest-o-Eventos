using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class EventoDisponivelDetalhesViewModel
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
    public int VagasOcupadas { get; set; }
    public DateTime InicioInscricao { get; set; }
    public DateTime FimInscricao { get; set; }
    public StatusEvento Status { get; set; }

    // Situação do usuário logado em relação ao evento
    public bool JaInscrito { get; set; }
    public int? InscricaoId { get; set; }
    public bool PodeInscrever { get; set; }
    public bool PodeCancelar { get; set; }
    public string? Aviso { get; set; }   // motivo pelo qual não pode se inscrever
}