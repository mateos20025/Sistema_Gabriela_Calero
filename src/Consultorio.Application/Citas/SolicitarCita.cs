using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;
using FluentValidation;

namespace Consultorio.Application.Citas;

/// <summary>
/// Caso de uso público: un paciente pide una cita desde la web (sin iniciar sesión).
/// <list type="bullet">
/// <item>Si la cédula no existe, se crea el paciente con su consentimiento.</item>
/// <item>Si existe, NO se muestran ni se modifican sus datos: lo escrito queda como contacto de la solicitud.</item>
/// <item>La solicitud queda Pendiente y reserva el horario por las horas que configuró la doctora.</item>
/// </list>
/// </summary>
public sealed class SolicitarCita(
    IRepositorioCitas repositorioCitas,
    IRepositorioPacientes repositorioPacientes,
    IRepositorioAgenda repositorioAgenda,
    MotorDisponibilidad motor,
    ExpirarReservasVencidas expirarReservas,
    IVerificadorHumano verificadorHumano,
    IRelojSistema reloj,
    OpcionesProteccionDatos proteccionDatos,
    IValidator<SolicitarCitaComando> validador)
{
    public async Task<Resultado<SolicitudRegistrada>> EjecutarAsync(
        SolicitarCitaComando comando, string? respuestaCaptcha, string? ipCliente, CancellationToken cancellationToken)
    {
        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        if (agenda is null || !agenda.Configuracion.SolicitudesWebHabilitadas)
        {
            return Falla(MensajesCitas.SolicitudesWebDeshabilitadas);
        }

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado<SolicitudRegistrada>.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        if (verificadorHumano.Habilitado && !await verificadorHumano.VerificarAsync(respuestaCaptcha, ipCliente, cancellationToken))
        {
            return Falla(MensajesCitas.VerificacionFallida);
        }

        SolicitarCitaComando.TryLeerHorario(comando.Horario, out var inicio);
        var horario = new IntervaloUtc(inicio, inicio.AddMinutes(agenda.Configuracion.DuracionCitaMinutos));

        await expirarReservas.EjecutarAsync(cancellationToken);
        if (!await motor.EstaLibreAsync(horario, null, cancellationToken))
        {
            return Falla(MensajesCitas.HorarioNoDisponible);
        }

        var ahora = reloj.AhoraUtc;
        var cedula = CedulaEcuatoriana.Normalizar(comando.Cedula)!;
        var paciente = await repositorioPacientes.ObtenerPorCedulaAsync(cedula, cancellationToken);

        if (paciente is { EstaDeshabilitado: true })
        {
            // Mensaje genérico: no se revela nada sobre la cédula a quien está en la web.
            return Falla(MensajesCitas.NoPudimosRegistrar);
        }

        if (paciente is null)
        {
            paciente = await RegistrarPacienteAsync(comando, cedula, ahora, cancellationToken);
        }
        else if (await repositorioCitas.ContarActivasDePacienteAsync(paciente.Id, ahora, cancellationToken)
                 >= agenda.Configuracion.MaximoSolicitudesActivasPorCedula)
        {
            return Falla(MensajesCitas.LimiteSolicitudes(agenda.Configuracion.MaximoSolicitudesActivasPorCedula));
        }

        var contacto = new ContactoSolicitud(comando.Nombres, comando.Apellidos, comando.Telefono, comando.CorreoElectronico);
        var cita = Cita.SolicitarDesdeWeb(paciente.Id, horario, ahora, agenda.Configuracion.HorasReservaSolicitud, contacto);

        try
        {
            await repositorioCitas.AgregarAsync(cita, cancellationToken);
        }
        catch (HorarioOcupadoException)
        {
            return Falla(MensajesCitas.HorarioNoDisponible);
        }

        return Resultado<SolicitudRegistrada>.Ok(new SolicitudRegistrada(cita.Inicio, cita.ReservaExpiraEn!.Value));
    }

    private async Task<Paciente> RegistrarPacienteAsync(SolicitarCitaComando comando, string cedula, DateTime ahora, CancellationToken cancellationToken)
    {
        var consentimiento = new ConsentimientoDatos(ahora, proteccionDatos.VersionPoliticaPrivacidad, OrigenRegistro.Web);
        var nuevo = Paciente.Registrar(cedula, comando.Nombres, comando.Apellidos, comando.Telefono, comando.CorreoElectronico, consentimiento);

        try
        {
            await repositorioPacientes.AgregarAsync(nuevo, cancellationToken);
            return nuevo;
        }
        catch (RegistroDuplicadoException)
        {
            // Dos envíos con la misma cédula al mismo tiempo: se usa el que quedó guardado.
            return await repositorioPacientes.ObtenerPorCedulaAsync(cedula, cancellationToken)
                ?? throw new InvalidOperationException("No se encontró el paciente recién registrado.");
        }
    }

    private static Resultado<SolicitudRegistrada> Falla(string mensaje) => Resultado<SolicitudRegistrada>.Falla(mensaje);
}
