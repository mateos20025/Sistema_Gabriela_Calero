using Consultorio.Domain.Pacientes;
using Consultorio.Domain.Pagos;
using Consultorio.Infrastructure.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consultorio.Infrastructure.Persistencia.Configuraciones;

internal sealed class PagoConfiguracion : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("pagos", tabla =>
        {
            tabla.HasCheckConstraint("ck_pagos_monto", "monto > 0");
            // Una transferencia siempre tiene su captura.
            tabla.HasCheckConstraint("ck_pagos_transferencia_con_comprobante", "metodo <> 'Transferencia' OR comprobante_hash IS NOT NULL");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Monto).HasPrecision(10, 2);
        builder.Property(p => p.Metodo).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Concepto).HasMaxLength(Pago.LongitudMaximaConcepto).IsRequired();
        builder.Property(p => p.NumeroComprobante).HasMaxLength(Pago.LongitudMaximaNumeroComprobante);
        builder.Property(p => p.ComprobanteArchivoId).HasMaxLength(ComprobanteTransferencia.LongitudMaximaArchivoId);
        builder.Property(p => p.ComprobanteTipo).HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.ComprobanteHash).HasMaxLength(ComprobanteTransferencia.LongitudHash);
        builder.Property(p => p.MotivoResolucion).HasMaxLength(Pago.LongitudMaximaMotivo);

        builder.HasOne<Paciente>().WithMany()
            .HasForeignKey(p => p.PacienteId)
            .HasConstraintName("fk_pagos_pacientes_paciente_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>().WithMany()
            .HasForeignKey(p => p.RegistradoPorId)
            .HasConstraintName("fk_pagos_usuarios_registrado_por_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>().WithMany()
            .HasForeignKey(p => p.ResueltoPorId)
            .HasConstraintName("fk_pagos_usuarios_resuelto_por_id")
            .OnDelete(DeleteBehavior.Restrict);

        // La misma captura no puede estar en dos pagos vigentes, aunque dos personas la suban al mismo tiempo.
        builder.HasIndex(p => p.ComprobanteHash)
            .IsUnique()
            .HasFilter("comprobante_hash IS NOT NULL AND estado <> 'Anulado'")
            .HasDatabaseName("ux_pagos_comprobante_hash_vigente");

        builder.HasIndex(p => new { p.Estado, p.CreadoEn });

        builder.Property(p => p.Version).IsRowVersion();
        builder.Ignore(p => p.EstaPendiente);
    }
}

internal sealed class ConfiguracionPagosConfiguracion : IEntityTypeConfiguration<ConfiguracionPagos>
{
    public void Configure(EntityTypeBuilder<ConfiguracionPagos> builder)
    {
        builder.ToTable("configuracion_pagos");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.PrecioConsulta).HasPrecision(10, 2);
        builder.Property(c => c.ConceptoPredeterminado).HasMaxLength(Pago.LongitudMaximaConcepto);
        builder.Property(c => c.Banco).HasMaxLength(DatosBancarios.LongitudMaximaTexto);
        builder.Property(c => c.TipoCuenta).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.NumeroCuenta).HasMaxLength(DatosBancarios.LongitudMaximaNumeroCuenta);
        builder.Property(c => c.Titular).HasMaxLength(DatosBancarios.LongitudMaximaTexto);
        builder.Property(c => c.IdentificacionTitular).HasMaxLength(13);

        builder.Property(c => c.Version).IsRowVersion();
        builder.Ignore(c => c.TieneDatosBancarios);
    }
}
