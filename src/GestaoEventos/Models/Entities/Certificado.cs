namespace GestaoEventos.Models.Entities;

public class Certificado
{
    public int Id { get; set; }

    public int InscricaoId { get; set; }
    public Inscricao Inscricao { get; set; } = null!;

    // Código aleatório e único, usado na validação pública (RN19)
    public string CodigoVerificacao { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;

    // false = certificado invalidado (RN20)
    public bool Valido { get; set; } = true;
}