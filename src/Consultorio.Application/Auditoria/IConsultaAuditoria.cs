namespace Consultorio.Application.Auditoria;

/// <summary>
/// Lectura del historial de cambios. Los registros se escriben solos al guardar (Infrastructure),
/// así ningún caso de uso puede olvidarse de auditar.
/// </summary>
public interface IConsultaAuditoria
{
    Task<IReadOnlyList<EventoAuditoria>> ObtenerHistorialAsync(string entidad, Guid entidadId, CancellationToken cancellationToken);
}

public enum AccionAuditoria
{
    Creado = 1,
    Modificado = 2,
    Deshabilitado = 3,
    Habilitado = 4,
}

/// <summary>
/// Un cambio registrado. Solo guarda QUÉ campos cambiaron, no sus valores anteriores, para no
/// acumular copias de datos personales (minimización de la LOPDP).
/// </summary>
public sealed record EventoAuditoria(DateTime FechaUtc, string Usuario, AccionAuditoria Accion, IReadOnlyList<string> Campos);
