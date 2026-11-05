using GestaoEventos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEventos.Data.Configurations;

public class InscricaoConfiguration : IEntityTypeConfiguration<Inscricao>
{
    public void Configure(EntityTypeBuilder<Inscricao> builder)
    {
        builder.ToTable("Inscricoes");

        // Um participante só pode ter uma inscrição por evento (RN08)
        builder.HasIndex(i => new { i.EventoId, i.UsuarioId }).IsUnique();

        builder.HasOne(i => i.Evento)
               .WithMany(e => e.Inscricoes)
               .HasForeignKey(i => i.EventoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Usuario)
               .WithMany(u => u.Inscricoes)
               .HasForeignKey(i => i.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}