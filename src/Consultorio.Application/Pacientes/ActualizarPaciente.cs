using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pacientes;
using FluentValidation;

namespace Consultorio.Application.Pacientes;

/// <summary>Caso de uso: corregir los datos de un paciente activo.</summary>
public sealed class ActualizarPaciente(
    IRepositorioPacientes repositorio,
    IUsuarioActual usuarioActual,
    IValidator<ActualizarPacienteComando> validador)
{
    public async Task<Resultado> EjecutarAsync(ActualizarPacienteComando comando, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var paciente = await repositorio.ObtenerPorIdAsync(comando.Id, cancellationToken);
        if (paciente is null)
        {
            return Resultado.Falla(MensajesPacientes.NoExiste);
        }

        if (paciente.EstaDeshabilitado)
        {
            return Resultado.Falla(MensajesPacientes.EstaDeshabilitado);
        }

        // La pantalla mostraba una versión anterior: alguien más guardó cambios entretanto.
        if (paciente.Version != comando.Version)
        {
            return Resultado.Falla(MensajesPacientes.ModificadoPorOtraPersona);
        }

        var cedula = CedulaEcuatoriana.Normalizar(comando.Cedula)!;
        if (cedula != paciente.Cedula)
        {
            var otro = await repositorio.ObtenerPorCedulaAsync(cedula, cancellationToken);
            if (otro is not null)
            {
                return Resultado.Falla(otro.EstaDeshabilitado
                    ? MensajesPacientes.CedulaDePacienteDeshabilitado
                    : MensajesPacientes.CedulaYaRegistrada);
            }
        }

        paciente.ActualizarDatos(cedula, comando.Nombres, comando.Apellidos, comando.Telefono, comando.CorreoElectronico);

        try
        {
            await repositorio.ActualizarAsync(paciente, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesPacientes.ModificadoPorOtraPersona);
        }
        catch (RegistroDuplicadoException)
        {
            return Resultado.Falla(MensajesPacientes.CedulaYaRegistrada);
        }

        return Resultado.Ok();
    }
}
