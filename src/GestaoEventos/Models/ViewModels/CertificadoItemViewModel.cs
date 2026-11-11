namespace GestaoEventos.Models.ViewModels;

public class CertificadoItemViewModel
{
    public int InscricaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Matricula { get; set; }

    // null = presença não registrada
    public bool? Presente { get; set; }

    // Preenchidos depois da liberação
    public string? Codigo { get; set; }
    public DateTime? DataEmissao { get; set; }

    // RN16 (o status do evento é conferido no resumo)
    public bool Elegivel => Presente == true;
}