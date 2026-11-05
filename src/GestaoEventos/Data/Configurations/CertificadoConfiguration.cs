using GestaoEventos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEventos.Data.Configurations;

public class CertificadoConfiguration : IEntityTypeConfiguration<Certificado>
{
    public void Configure(EntityTypeBuilder<Certificado> builder)
    {
        builder.ToTable("Certificados");

        builder.Property(c => c.CodigoVerificacao)
               .IsRequired().HasMaxLength(32).IsUnicode(false);   // varchar(32)
        builder.HasIndex(c => c.CodigoVerificacao).IsUnique();     // RN19

        // No máximo um certificado por inscrição (RN18)
        builder.HasOne(c => c.Inscricao)
               .WithOne(i => i.Certificado)
               .HasForeignKey<Certificado>(c => c.InscricaoId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}