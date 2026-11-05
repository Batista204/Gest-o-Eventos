using GestaoEventos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEventos.Data.Configurations;

public class PresencaConfiguration : IEntityTypeConfiguration<Presenca>
{
    public void Configure(EntityTypeBuilder<Presenca> builder)
    {
        builder.ToTable("Presencas");

        // Relação 1 para 0..1: o EF cria índice único em InscricaoId (RN14)
        builder.HasOne(p => p.Inscricao)
               .WithOne(i => i.Presenca)
               .HasForeignKey<Presenca>(p => p.InscricaoId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.RegistradaPor)
               .WithMany()
               .HasForeignKey(p => p.RegistradaPorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}