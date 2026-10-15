using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consultorio.Infrastructure.Persistencia.Configuraciones;

/// <summary>
/// Tabla "citas". Además de lo que se configura aquí, la migración "Citas" crea con SQL la restricción
/// de exclusión <see cref="CitasConfiguracion.RestriccionSinCruce"/>: PostgreSQL rechaza dos citas activas
/// que se crucen, aunque dos personas tomen el mismo horario en el mismo instante.
/// </summary>
internal sealed class CitasConfiguracion : IEntityTypeConfiguration<Cita>
{
    public const string RestriccionSinCruce = "ex_citas_sin_cruce";

    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("citas", tabla =>
        {
            tabla.HasCheckConstraint("ck_citas_fin_despues_de_inicio", "fin > inicio");
            // Solo una solicitud pendiente tiene reserva con vencimiento.
            tabla.HasCheckConstraint("ck_citas_reserva_solo_pendiente", "reserva_expira_en IS NULL OR estado = 'Pendiente'");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Origen).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.ContactoNombre).HasMaxLength(NombrePersona.LongitudMaxima * 2 + 1);
        builder.Property(c => c.ContactoTelefono).HasMaxLength(TelefonoEcuatoriano.LongitudMaxima);
        builder.Property(c => c.ContactoCorreo).HasMaxLength(DireccionCorreo.LongitudMaxima);

        builder.HasOne<Paciente>().WithMany()
            .HasForeignKey(c => c.PacienteId)
            .HasConstraintName("fk_citas_pacientes_paciente_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Inicio);
        builder.HasIndex(c => new { c.Estado, c.Inicio });
        builder.HasIndex(c => new { c.PacienteId, c.Inicio });

        builder.Property(c => c.Version).IsRowVersion();

        // El historial se guarda aparte (tabla historial_cita) desde el repositorio.
        builder.Ignore(c => c.CambiosNuevos);
        builder.Ignore(c => c.Horario);
        builder.Ignore(c => c.EstaActiva);
    }
}

internal sealed class HistorialCitaConfiguracion : IEntityTypeConfiguration<HistorialCita>
{
    public void Configure(EntityTypeBuilder<HistorialCita> builder)
    {
        builder.ToTable("historial_cita");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Observacion).HasMaxLength(Cita.LongitudMaximaObservacion);

        builder.HasOne<Cita>().WithMany()
            .HasForeignKey(h => h.CitaId)
            .HasConstraintName("fk_historial_cita_citas_cita_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>().WithMany()
            .HasForeignKey(h => h.UsuarioId)
            .HasConstraintName("fk_historial_cita_usuarios_usuario_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.CitaId, h.CreadoEn });
    }
}

internal sealed class ConfiguracionAgendaConfiguracion : IEntityTypeConfiguration<ConfiguracionAgenda>
{
    public void Configure(EntityTypeBuilder<ConfiguracionAgenda> builder)
    {
        builder.ToTable("configuracion_agenda", tabla =>
        {
            tabla.HasCheckConstraint(
                "ck_configuracion_agenda_duracion",
                $"duracion_cita_minutos BETWEEN {ConfiguracionAgenda.DuracionMinimaMinutos} AND {ConfiguracionAgenda.DuracionMaximaMinutos}");
            tabla.HasCheckConstraint(
                "ck_configuracion_agenda_anticipacion",
                $"anticipacion_minima_horas BETWEEN 0 AND {ConfiguracionAgenda.AnticipacionMinimaMaximaHoras} " +
                $"AND anticipacion_maxima_dias BETWEEN 1 AND {ConfiguracionAgenda.AnticipacionMaximaLimiteDias}");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Version).IsRowVersion();
    }
}

internal sealed class FranjaAtencionConfiguracion : IEntityTypeConfiguration<FranjaAtencion>
{
    public void Configure(EntityTypeBuilder<FranjaAtencion> builder)
    {
        builder.ToTable("franjas_atencion", tabla => tabla.HasCheckConstraint("ck_franjas_atencion_fin_despues_de_inicio", "fin > inicio"));

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.DiaSemana).HasConversion<string>().HasMaxLength(10);
        builder.HasIndex(f => f.DiaSemana);
    }
}

internal sealed class BloqueoAgendaConfiguracion : IEntityTypeConfiguration<BloqueoAgenda>
{
    public void Configure(EntityTypeBuilder<BloqueoAgenda> builder)
    {
        builder.ToTable("bloqueos_agenda", tabla => tabla.HasCheckConstraint("ck_bloqueos_agenda_fin_despues_de_inicio", "fin > inicio"));

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Motivo).HasMaxLength(BloqueoAgenda.LongitudMaximaMotivo).IsRequired();
        builder.HasIndex(b => new { b.Inicio, b.Fin });
        builder.Ignore(b => b.Intervalo);
    }
}
