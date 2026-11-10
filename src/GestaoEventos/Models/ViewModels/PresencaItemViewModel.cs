namespace GestaoEventos.Models.ViewModels;

public class PresencaItemViewModel
{
    public int InscricaoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Matricula { get; set; }

    // Situação gravada: null = ainda sem registro (usado na tela só de consulta)
    public bool? PresenteRegistrado { get; set; }

    // Valor do interruptor no formulário
    public bool Presente { get; set; }
}