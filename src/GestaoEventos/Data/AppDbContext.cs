using GestaoEventos.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GestaoEventos.Data;

public class AppDbContext : IdentityDbContext<Usuario>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Evento> Eventos => Set<Evento>();
    public DbSet<Inscricao> Inscricoes => Set<Inscricao>();
    public DbSet<Presenca> Presencas => Set<Presenca>();
    public DbSet<Certificado> Certificados => Set<Certificado>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // configura as tabelas do Identity

        // Carrega automaticamente todas as classes de Data/Configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}