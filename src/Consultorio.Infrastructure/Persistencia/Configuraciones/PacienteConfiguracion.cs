using Consultorio.Domain.Pacientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consultorio.Infrastructure.Persistencia.Configuraciones;

internal sealed class PacienteConfiguracion : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("pacientes", tabla =>
        {
            // Segunda línea de defensa: aunque un error en el código intentara guardar datos con
            // formato incorrecto, PostgreSQL los rechazaría.
            tabla.HasCheckConstraint("ck_pacientes_cedula", "cedula ~ '^[0-9]{10}$'");
            tabla.HasCheckConstraint("ck_pacientes_telefono", @"telefono ~ '^\+593[2-9][0-9]{7,8}$'");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Cedula).HasMaxLength(CedulaEcuatoriana.Longitud).IsRequired();
        // Única en TODAS las filas, también las deshabilitadas: un paciente nunca se duplica.
        builder.HasIndex(p => p.Cedula).IsUnique();

        builder.Property(p => p.Nombres).HasMaxLength(NombrePersona.LongitudMaxima).IsRequired();
        builder.Property(p => p.Apellidos).HasMaxLength(NombrePersona.LongitudMaxima).IsRequired();
        builder.HasIndex(p => new { p.Apellidos, p.Nombres });

        builder.Property(p => p.Telefono).HasMaxLength(TelefonoEcuatoriano.LongitudMaxima).IsRequired();
        builder.Property(p => p.CorreoElectronico).HasMaxLength(DireccionCorreo.LongitudMaxima);

        builder.Property(p => p.ConsentimientoVersion).HasMaxLength(ConsentimientoDatos.LongitudMaximaVersion).IsRequired();
        builder.Property(p => p.ConsentimientoOrigen).HasConversion<string>().HasMaxLength(20);

        // "xmin" es una columna interna de PostgreSQL que cambia en cada UPDATE: sirve de versión
        // para detectar ediciones simultáneas sin crear una columna nueva.
        builder.Property(p => p.Version).IsRowVersion();

        builder.Ignore(p => p.NombreCompleto);
        builder.Ignore(p => p.EstaDeshabilitado);

        // Las consultas normales nunca ven pacientes deshabilitados.
        // Para verlos hay que pedirlo explícitamente con IgnoreQueryFilters().
        builder.HasQueryFilter(p => p.DeshabilitadoEn == null);
    }
}
