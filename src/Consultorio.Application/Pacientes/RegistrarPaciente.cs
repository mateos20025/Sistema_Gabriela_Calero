using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Domain.Pacientes;
using FluentValidation;

namespace Consultorio.Application.Pacientes;

/// <summary>
/// Caso de uso: el personal registra un paciente desde el panel, con su consentimiento de datos.
/// (El registro que hace el propio paciente desde la web llegará con el módulo de Citas).
/// </summary>
public sealed class RegistrarPaciente(
    IRepositorioPacientes repositorio,
    IUsuarioActual usuarioActual,
    IRelojSistema reloj,
    OpcionesProteccionDatos proteccionDatos,
    IValidator<RegistrarPacienteComando> validador)
{
    public async Task<Resultado<Guid>> EjecutarAsync(RegistrarPacienteComando comando, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado<Guid>.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var cedula = CedulaEcuatoriana.Normalizar(comando.Cedula)!;
        var existente = await repositorio.ObtenerPorCedulaAsync(cedula, cancellationToken);
        if (existente is not null)
        {
            return Resultado<Guid>.Falla(existente.EstaDeshabilitado
                ? MensajesPacientes.CedulaDePacienteDeshabilitado
                : MensajesPacientes.CedulaYaRegistrada);
        }

        var consentimiento = new ConsentimientoDatos(reloj.AhoraUtc, proteccionDatos.VersionPoliticaPrivacidad, OrigenRegistro.Panel);
        var paciente = Paciente.Registrar(
            cedula, comando.Nombres, comando.Apellidos, comando.Telefono, comando.CorreoElectronico, consentimiento);

        try
        {
            await repositorio.AgregarAsync(paciente, cancellationToken);
        }
        catch (RegistroDuplicadoException)
        {
            // Otra persona registró la misma cédula en el mismo instante.
            return Resultado<Guid>.Falla(MensajesPacientes.CedulaYaRegistrada);
        }

        return Resultado<Guid>.Ok(paciente.Id);
    }
}
