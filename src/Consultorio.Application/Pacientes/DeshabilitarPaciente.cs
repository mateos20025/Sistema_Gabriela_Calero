using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;

namespace Consultorio.Application.Pacientes;

/// <summary>
/// Caso de uso: deshabilitar un paciente (doctora o asistente). Deja de aparecer en las búsquedas;
/// sus datos se conservan, la acción queda en la auditoría y se deshace con <see cref="HabilitarPaciente"/>.
/// </summary>
public sealed class DeshabilitarPaciente(
    IRepositorioPacientes repositorio,
    IUsuarioActual usuarioActual,
    IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return Resultado.Falla(MensajesPacientes.NoExiste);
        }

        if (paciente.EstaDeshabilitado)
        {
            return Resultado.Falla(MensajesPacientes.YaEstabaDeshabilitado);
        }

        paciente.Deshabilitar(reloj.AhoraUtc);

        try
        {
            await repositorio.ActualizarAsync(paciente, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesPacientes.ModificadoPorOtraPersona);
        }

        return Resultado.Ok();
    }
}
