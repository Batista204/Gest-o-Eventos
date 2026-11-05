using System.ComponentModel.DataAnnotations;
using GestaoEventos.Models.Enums;

namespace GestaoEventos.Models.ViewModels;

public class EventoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o título.")]
    [StringLength(150, MinimumLength = 5, ErrorMessage = "O título deve ter entre 5 e 150 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o local.")]
    [StringLength(200, ErrorMessage = "O local deve ter no máximo 200 caracteres.")]
    [Display(Name = "Local")]
    public string Local { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data do evento.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Data do evento")]
    public DateOnly? DataEvento { get; set; }

    [Required(ErrorMessage = "Informe o horário de início.")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Início")]
    public TimeOnly? HoraInicio { get; set; }

    [Required(ErrorMessage = "Informe o horário de término.")]
    [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Término")]
    public TimeOnly? HoraFim { get; set; }

    [Required(ErrorMessage = "Informe a carga horária.")]
    [Range(0.5, 200.0, ErrorMessage = "A carga horária deve estar entre 0,5 e 200 horas.")]
    [Display(Name = "Carga horária (horas)")]
    public decimal? CargaHoraria { get; set; }

    [Required(ErrorMessage = "Informe o limite de vagas.")]
    [Range(1, 10000, ErrorMessage = "O limite de vagas deve estar entre 1 e 10000.")]
    [Display(Name = "Limite de vagas")]
    public int? LimiteVagas { get; set; }

    [Required(ErrorMessage = "Informe o início das inscrições.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Início das inscrições")]
    public DateTime? InicioInscricao { get; set; }

    [Required(ErrorMessage = "Informe o fim das inscrições.")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Fim das inscrições")]
    public DateTime? FimInscricao { get; set; }

    // Somente leitura: usado para impedir a edição de eventos encerrados ou cancelados
    public StatusEvento Status { get; set; }

    // Controle de concorrência na edição
    public byte[]? RowVersion { get; set; }
}