using GestaoEventos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoEventos.Data.Configurations;

public class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        // Regras de integridade garantidas pelo próprio banco (RN01, RN02, RN03)
        builder.ToTable("Eventos", t =>
        {
            t.HasCheckConstraint("CK_Eventos_Horario", "[HoraFim] > [HoraInicio]");
            t.HasCheckConstraint("CK_Eventos_CargaHoraria", "[CargaHoraria] > 0");
            t.HasCheckConstraint("CK_Eventos_LimiteVagas", "[LimiteVagas] > 0");
            t.HasCheckConstraint("CK_Eventos_Inscricao", "[FimInscricao] >= [InicioInscricao]");
        });

        builder.Property(e => e.Titulo).IsRequired().HasMaxLength(150);
        builder.Property(e => e.Descricao).IsRequired().HasMaxLength(2000);
        builder.Property(e => e.Local).IsRequired().HasMaxLength(200);
        builder.Property(e => e.CargaHoraria).HasPrecision(4, 1);
        builder.Property(e => e.RowVersion).IsRowVersion();

        // Sem exclusão em cascata: preserva o histórico (RN05)
        builder.HasOne(e => e.Organizador)
               .WithMany(u => u.EventosOrganizados)
               .HasForeignKey(e => e.OrganizadorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}