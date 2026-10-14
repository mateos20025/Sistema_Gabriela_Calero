using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Auditoria;
using Consultorio.Infrastructure.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infrastructure.Persistencia;

/// <summary>
/// Única base de datos del sistema: tablas de Identity (usuarios, roles...), pacientes, auditoría y,
/// en los próximos módulos, citas y pagos.
/// </summary>
public sealed class ConsultorioDbContext(DbContextOptions<ConsultorioDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Paciente> Pacientes => Set<Paciente>();

    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    /// <summary>
    /// Usuario responsable de los cambios que se guarden con este contexto (para la auditoría).
    /// Lo asigna <see cref="FabricaContextos"/>; si queda vacío, el cambio se atribuye al sistema.
    /// </summary>
    public Guid? UsuarioAuditoriaId { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Aplica todas las clases IEntityTypeConfiguration de la carpeta Configuraciones.
        builder.ApplyConfigurationsFromAssembly(typeof(ConsultorioDbContext).Assembly);

        AgregarMarcasDeTiempo(builder);
    }

    /// <summary>
    /// Regla del proyecto: todas las tablas tienen creado_en y actualizado_en.
    /// Si la entidad ya tiene esas propiedades (como Usuario) se usan; si no, se crean como
    /// "propiedades sombra" que solo existen en la base de datos.
    /// El <see cref="MarcasDeTiempoInterceptor"/> les pone valor al guardar.
    /// <para>
    /// Solo se procesan entidades con clave primaria (tablas reales). En este punto EF también tiene
    /// "candidatas" que descartará al final (por ejemplo IdentityPasskeyData de Identity); si se
    /// configuraran aquí dejarían de descartarse y EF exigiría que tengan clave.
    /// </para>
    /// </summary>
    private static void AgregarMarcasDeTiempo(ModelBuilder builder)
    {
        var tablas = builder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && t.FindPrimaryKey() is not null)
            .ToList();

        foreach (var tipo in tablas)
        {
            builder.Entity(tipo.ClrType).Property<DateTime>(MarcasDeTiempoInterceptor.CreadoEn);
            builder.Entity(tipo.ClrType).Property<DateTime>(MarcasDeTiempoInterceptor.ActualizadoEn);
        }
    }
}
