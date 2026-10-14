using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;

namespace Consultorio.Application.Pacientes;

/// <summary>Caso de uso: volver a habilitar un paciente deshabilitado (doctora o asistente).</summary>
public sealed class HabilitarPaciente(IRepositorioPacientes repositorio, IUsuarioActual usuarioActual)
{
    public async Task<Resultado> EjecutarAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var paciente = await repositorio.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return Resultado.Falla(MensajesPacientes.NoExiste);
        }

        if (!paciente.EstaDeshabilitado)
        {
            return Resultado.Falla(MensajesPacientes.YaEstabaHabilitado);
        }

        paciente.Habilitar();

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
