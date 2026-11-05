using Microsoft.AspNetCore.Identity;

namespace GestaoEventos.Models.Entities;

public class Usuario : IdentityUser
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string? Matricula { get; set; }   // opcional (decisão P5)
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

    public ICollection<Evento> EventosOrganizados { get; set; } = new List<Evento>();
    public ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();
}