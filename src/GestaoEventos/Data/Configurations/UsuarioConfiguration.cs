using GestaoEventos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEventos.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.Property(u => u.NomeCompleto).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Matricula).HasMaxLength(30);

        // Garante unicidade do e-mail também no banco (RN21, RNF07).
        // O Identity já cria esse índice, aqui apenas o tornamos único.
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
    }
}