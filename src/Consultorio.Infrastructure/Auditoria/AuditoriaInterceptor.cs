using Consultorio.Application.Auditoria;
using Consultorio.Domain.Pacientes;
using Consultorio.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Consultorio.Infrastructure.Auditoria;

/// <summary>
/// Antes de cada SaveChanges revisa los cambios de las entidades auditadas y agrega filas a la tabla
/// "auditoria" en la MISMA transacción: o se guarda el cambio con su auditoría, o no se guarda nada.
/// </summary>
public sealed class AuditoriaInterceptor : SaveChangesInterceptor
{
    /// <summary>Entidades cuyos cambios se auditan. Los próximos módulos (citas, pagos) se agregan aquí.</summary>
    private static readonly HashSet<Type> TiposAuditados = [typeof(Paciente)];

    private static readonly HashSet<string> CamposTecnicos =
    [
        MarcasDeTiempoInterceptor.CreadoEn,
        MarcasDeTiempoInterceptor.ActualizadoEn,
        nameof(Paciente.Version),
    ];

    private const string CampoDeshabilitado = nameof(Paciente.DeshabilitadoEn);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Registrar(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Registrar(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void Registrar(DbContext? contexto)
    {
        if (contexto is not ConsultorioDbContext db)
        {
            return;
        }

        var registros = db.ChangeTracker.Entries()
            .Where(e => TiposAuditados.Contains(e.Metadata.ClrType))
            .Select(e => CrearRegistro(e, db.UsuarioAuditoriaId))
            .OfType<RegistroAuditoria>()
            .ToList();

        db.Auditoria.AddRange(registros);
    }

    private static RegistroAuditoria? CrearRegistro(EntityEntry entrada, Guid? usuarioId)
    {
        AccionAuditoria accion;
        string[] campos = [];

        switch (entrada.State)
        {
            case EntityState.Added:
                accion = AccionAuditoria.Creado;
                break;

            case EntityState.Modified:
                var modificados = entrada.Properties
                    .Where(p => p.IsModified && !CamposTecnicos.Contains(p.Metadata.Name))
                    .ToList();

                var deshabilitado = modificados.FirstOrDefault(p => p.Metadata.Name == CampoDeshabilitado);
                accion = deshabilitado switch
                {
                    { OriginalValue: null, CurrentValue: not null } => AccionAuditoria.Deshabilitado,
                    { OriginalValue: not null, CurrentValue: null } => AccionAuditoria.Habilitado,
                    _ => AccionAuditoria.Modificado,
                };

                campos = [.. modificados.Where(p => p.Metadata.Name != CampoDeshabilitado).Select(p => p.Metadata.Name)];
                if (accion == AccionAuditoria.Modificado && campos.Length == 0)
                {
                    return null; // Nada relevante cambió.
                }

                break;

            default:
                return null;
        }

        return new RegistroAuditoria
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = usuarioId,
            Entidad = entrada.Metadata.ClrType.Name,
            EntidadId = (Guid)entrada.Property("Id").CurrentValue!,
            Accion = accion.ToString(),
            Campos = campos,
        };
    }
}
