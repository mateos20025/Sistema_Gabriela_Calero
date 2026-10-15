using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Application.Pacientes;
using Consultorio.Domain.Citas;
using FluentValidation;

namespace Consultorio.Application.Citas;

/// <summary>
/// Caso de uso del sistema: libera las solicitudes web cuya reserva venció sin que el consultorio las atendiera.
/// Lo ejecuta un proceso en segundo plano cada minuto y también se llama antes de ocupar un horario.
/// </summary>
public sealed class ExpirarReservasVencidas(IRepositorioCitas repositorio, IRelojSistema reloj)
{
    public const string Motivo = "La reserva venció sin que el consultorio confirmara la cita.";

    public async Task<int> EjecutarAsync(CancellationToken cancellationToken)
    {
        var ahora = reloj.AhoraUtc;
        var liberadas = 0;

        foreach (var cita in await repositorio.ListarReservasVencidasAsync(ahora, cancellationToken))
        {
            cita.CambiarEstado(EstadoCita.Cancelada, usuarioId: null, ahora, Motivo);
            try
            {
                await repositorio.ActualizarAsync(cita, cancellationToken);
                liberadas++;
            }
            catch (ConflictoConcurrenciaException)
            {
                // La asistente la atendió justo en este momento: se respeta su cambio.
            }
        }

        return liberadas;
    }
}

/// <summary>Caso de uso: el personal agenda una cita (por teléfono o en persona); queda confirmada.</summary>
public sealed class AgendarCita(
    IRepositorioCitas repositorioCitas,
    IRepositorioPacientes repositorioPacientes,
    IRepositorioAgenda repositorioAgenda,
    MotorDisponibilidad motor,
    ExpirarReservasVencidas expirarReservas,
    IUsuarioActual usuarioActual,
    IRelojSistema reloj,
    IValidator<AgendarCitaComando> validador)
{
    public async Task<Resultado<Guid>> EjecutarAsync(AgendarCitaComando comando, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado<Guid>.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var paciente = await repositorioPacientes.ObtenerPorIdAsync(comando.PacienteId, cancellationToken);
        if (paciente is null)
        {
            return Resultado<Guid>.Falla(MensajesCitas.PacienteNoExiste);
        }

        if (paciente.EstaDeshabilitado)
        {
            return Resultado<Guid>.Falla(MensajesCitas.PacienteDeshabilitado);
        }

        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        if (agenda is null)
        {
            return Resultado<Guid>.Falla(MensajesCitas.AgendaNoConfigurada);
        }

        var inicio = DateTime.SpecifyKind(comando.InicioUtc!.Value, DateTimeKind.Utc);
        var horario = new IntervaloUtc(inicio, inicio.AddMinutes(agenda.Configuracion.DuracionCitaMinutos));

        await expirarReservas.EjecutarAsync(cancellationToken);
        if (!await motor.EstaLibreAsync(horario, null, cancellationToken))
        {
            return Resultado<Guid>.Falla(MensajesCitas.HorarioNoDisponible);
        }

        var observacion = string.IsNullOrWhiteSpace(comando.Observacion) ? null : comando.Observacion;
        var cita = Cita.AgendarDesdePanel(paciente.Id, horario, sesion.Id, reloj.AhoraUtc, observacion);

        try
        {
            await repositorioCitas.AgregarAsync(cita, cancellationToken);
        }
        catch (HorarioOcupadoException)
        {
            return Resultado<Guid>.Falla(MensajesCitas.HorarioNoDisponible);
        }

        return Resultado<Guid>.Ok(cita.Id);
    }
}

/// <summary>
/// Caso de uso: registrar el resultado del trabajo con la cita (pasar a la cola, confirmar, no contestó,
/// cancelar, atendida, no asistió). Las reglas de qué estado sigue a cuál están en el Domain.
/// </summary>
public sealed class CambiarEstadoCita(IRepositorioCitas repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid citaId, EstadoCita nuevo, string? observacion, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        var cita = await repositorio.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
        {
            return Resultado.Falla(MensajesCitas.CitaNoExiste);
        }

        var ahora = reloj.AhoraUtc;

        if (nuevo == EstadoCita.Reprogramada || !ReglasEstadoCita.PuedePasar(cita.Estado, nuevo))
        {
            return Resultado.Falla(MensajesCitas.TransicionNoPermitida(cita.Estado, nuevo));
        }

        if (nuevo == EstadoCita.Cancelada && string.IsNullOrWhiteSpace(observacion))
        {
            return Resultado.Falla(MensajesCitas.MotivoCancelacionObligatorio);
        }

        if (nuevo is EstadoCita.Atendida or EstadoCita.NoAsistio && ahora < cita.Inicio)
        {
            return Resultado.Falla(MensajesCitas.AunNoEsHora);
        }

        if (observacion?.Trim().Length > Cita.LongitudMaximaObservacion)
        {
            return Resultado.Falla($"La observación no puede tener más de {Cita.LongitudMaximaObservacion} caracteres.");
        }

        cita.CambiarEstado(nuevo, sesion.Id, ahora, observacion);

        try
        {
            await repositorio.ActualizarAsync(cita, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesCitas.ModificadoPorOtraPersona);
        }
        catch (HorarioOcupadoException)
        {
            return Resultado.Falla(MensajesCitas.HorarioNoDisponible);
        }

        return Resultado.Ok();
    }
}

/// <summary>Caso de uso: mover la cita a otro horario libre; queda Reprogramada hasta que el paciente confirme.</summary>
public sealed class ReprogramarCita(
    IRepositorioCitas repositorioCitas,
    IRepositorioAgenda repositorioAgenda,
    MotorDisponibilidad motor,
    ExpirarReservasVencidas expirarReservas,
    IUsuarioActual usuarioActual,
    IRelojSistema reloj)
{
    public async Task<Resultado> EjecutarAsync(Guid citaId, DateTime? nuevoInicioUtc, string? observacion, CancellationToken cancellationToken)
    {
        var sesion = await usuarioActual.ExigirSesionAsync();

        if (nuevoInicioUtc is null)
        {
            return Resultado.Falla("Elige el nuevo horario.");
        }

        var cita = await repositorioCitas.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
        {
            return Resultado.Falla(MensajesCitas.CitaNoExiste);
        }

        if (!ReglasEstadoCita.PuedePasar(cita.Estado, EstadoCita.Reprogramada))
        {
            return Resultado.Falla(MensajesCitas.NoSePuedeReprogramar);
        }

        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        if (agenda is null)
        {
            return Resultado.Falla(MensajesCitas.AgendaNoConfigurada);
        }

        var inicio = DateTime.SpecifyKind(nuevoInicioUtc.Value, DateTimeKind.Utc);
        var horario = new IntervaloUtc(inicio, inicio.AddMinutes(agenda.Configuracion.DuracionCitaMinutos));
        if (horario == cita.Horario)
        {
            return Resultado.Falla(MensajesCitas.MismoHorario);
        }

        await expirarReservas.EjecutarAsync(cancellationToken);
        if (!await motor.EstaLibreAsync(horario, cita.Id, cancellationToken))
        {
            return Resultado.Falla(MensajesCitas.HorarioNoDisponible);
        }

        cita.Reprogramar(horario, sesion.Id, reloj.AhoraUtc, string.IsNullOrWhiteSpace(observacion) ? null : observacion);

        try
        {
            await repositorioCitas.ActualizarAsync(cita, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesCitas.ModificadoPorOtraPersona);
        }
        catch (HorarioOcupadoException)
        {
            return Resultado.Falla(MensajesCitas.HorarioNoDisponible);
        }

        return Resultado.Ok();
    }
}

public sealed class ObtenerCita(IRepositorioCitas repositorio, IUsuarioActual usuarioActual)
{
    public async Task<CitaDetalle?> EjecutarAsync(Guid citaId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return await repositorio.ObtenerDetalleAsync(citaId, cancellationToken);
    }
}

/// <summary>Caso de uso: la cola de trabajo de la asistente.</summary>
public sealed class ListarSolicitudes(IRepositorioCitas repositorio, IUsuarioActual usuarioActual)
{
    public async Task<ColaSolicitudes> EjecutarAsync(CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        var citas = await repositorio.ListarPorEstadosAsync(
            [EstadoCita.Pendiente, EstadoCita.EnCola, EstadoCita.NoContesta, EstadoCita.Reprogramada], cancellationToken);

        IReadOnlyList<CitaResumen> De(EstadoCita estado) => [.. citas.Where(c => c.Estado == estado).OrderBy(c => c.InicioUtc)];

        return new ColaSolicitudes(De(EstadoCita.Pendiente), De(EstadoCita.EnCola), De(EstadoCita.NoContesta), De(EstadoCita.Reprogramada));
    }
}

public sealed class ListarCitasDePaciente(IRepositorioCitas repositorio, IUsuarioActual usuarioActual)
{
    public async Task<IReadOnlyList<CitaResumen>> EjecutarAsync(Guid pacienteId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return await repositorio.ListarPorPacienteAsync(pacienteId, cancellationToken);
    }
}
