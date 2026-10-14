using Consultorio.Application.Abstracciones;
using Consultorio.Application.Auditoria;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Pacientes;

/// <summary>
/// Caso de uso: ficha de un paciente (también de los deshabilitados, para poder habilitarlos).
/// El historial de cambios solo lo ve la doctora.
/// </summary>
public sealed class ObtenerPaciente(
    IRepositorioPacientes repositorio,
    IConsultaAuditoria auditoria,
    IUsuarioActual usuarioActual)
{
    public async Task<PacienteDetalle?> EjecutarAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return null;
        }

        var historial = sesion.EsDoctora
            ? await auditoria.ObtenerHistorialAsync(nameof(Paciente), paciente.Id, cancellationToken)
            : [];

        return new PacienteDetalle(
            paciente.Id,
            paciente.Cedula,
            paciente.Nombres,
            paciente.Apellidos,
            paciente.Telefono,
            paciente.CorreoElectronico,
            paciente.ConsentimientoAceptadoEn,
            paciente.ConsentimientoVersion,
            paciente.ConsentimientoOrigen,
            paciente.DeshabilitadoEn,
            paciente.Version,
            paciente.CreadoEn,
            paciente.ActualizadoEn,
            historial);
    }
}
