namespace GestaoEventos.Models.ViewModels;

public class CertificadoValidacaoViewModel
{
    public string? Codigo { get; set; }
    public bool Consultado { get; set; }
    public bool Encontrado { get; set; }

    // Só o necessário para conferir a autenticidade (RN34)
    public string? Nome { get; set; }
    public string? Evento { get; set; }
    public DateOnly? DataEvento { get; set; }
    public decimal? CargaHoraria { get; set; }
    public DateTime? DataEmissao { get; set; }   // UTC
}