using Consultorio.Infrastructure.Identidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consultorio.Infrastructure.Auditoria;

/// <summary>
/// Una fila de la tabla "auditoria": quién hizo qué sobre qué registro y cuándo (creado_en).
/// Solo se agregan filas; nunca se modifican ni se borran.
/// </summary>
public sealed class RegistroAuditoria
{
    public Guid Id { get; set; }

    /// <summary>Usuario del panel que hizo el cambio; <c>null</c> si lo hizo el sistema.</summary>
    public Guid? UsuarioId { get; set; }

    /// <summary>Tipo de registro, por ejemplo "Paciente".</summary>
    public string Entidad { get; set; } = "";

    public Guid EntidadId { get; set; }

    public string Accion { get; set; } = "";

    /// <summary>Nombres de los campos que cambiaron (sin sus valores).</summary>
    public string[] Campos { get; set; } = [];

    /// <summary>Nuevo estado del registro (pagos, citas). No es un dato personal.</summary>
    public string? EstadoNuevo { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }
}

internal sealed class RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("auditoria");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Entidad).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Accion).HasMaxLength(20).IsRequired();
        builder.Property(r => r.EstadoNuevo).HasMaxLength(40);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(r => r.UsuarioId)
            .HasConstraintName("fk_auditoria_usuarios_usuario_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.Entidad, r.EntidadId, r.CreadoEn });
    }
}
