namespace GestaoEventos.Models.Entities;

public class Presenca
{
    public int Id { get; set; }

    public int InscricaoId { get; set; }
    public Inscricao Inscricao { get; set; } = null!;

    public bool Presente { get; set; }
    public DateTime DataRegistro { get; set; } = DateTime.UtcNow;

    // Quem registrou (o Admin) - rastreabilidade
    public string RegistradaPorId { get; set; } = string.Empty;
    public Usuario RegistradaPor { get; set; } = null!;
}