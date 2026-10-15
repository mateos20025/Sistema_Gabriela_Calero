using Consultorio.Application.Abstracciones;
using Consultorio.Application.Comun;
using Consultorio.Domain.Citas;
using FluentValidation;

namespace Consultorio.Application.Citas;

/// <summary>Caso de uso: ver la configuración de la agenda (todo el personal).</summary>
public sealed class ObtenerConfiguracionAgenda(IRepositorioAgenda repositorio, IUsuarioActual usuarioActual)
{
    public async Task<ConfiguracionAgendaVista> EjecutarAsync(CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        var agenda = await repositorio.ObtenerAsync(cancellationToken);
        return agenda is null ? ConfiguracionAgendaVista.SinConfigurar : ConfiguracionAgendaVista.Desde(agenda);
    }
}

/// <summary>Caso de uso: la doctora define duración de las citas, reglas de anticipación y horario semanal.</summary>
public sealed class ActualizarConfiguracionAgenda(
    IRepositorioAgenda repositorio,
    IUsuarioActual usuarioActual,
    IValidator<ConfiguracionAgendaComando> validador)
{
    public async Task<Resultado> EjecutarAsync(ConfiguracionAgendaComando comando, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirDoctoraAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var actual = await repositorio.ObtenerAsync(cancellationToken);
        if (actual is not null && actual.Configuracion.Version != comando.Version)
        {
            return Resultado.Falla(MensajesCitas.ModificadoPorOtraPersona);
        }

        ConfiguracionAgenda configuracion;
        if (actual is null)
        {
            configuracion = ConfiguracionAgenda.Crear(
                comando.DuracionCitaMinutos, comando.AnticipacionMinimaHoras, comando.AnticipacionMaximaDias,
                comando.HorasReservaSolicitud, comando.MaximoSolicitudesActivasPorCedula, comando.SolicitudesWebHabilitadas);
        }
        else
        {
            configuracion = actual.Configuracion;
            configuracion.Actualizar(
                comando.DuracionCitaMinutos, comando.AnticipacionMinimaHoras, comando.AnticipacionMaximaDias,
                comando.HorasReservaSolicitud, comando.MaximoSolicitudesActivasPorCedula, comando.SolicitudesWebHabilitadas);
        }

        var franjas = comando.Franjas
            .Select(f => FranjaAtencion.Crear(f.DiaSemana, TimeOnly.FromTimeSpan(f.Inicio!.Value), TimeOnly.FromTimeSpan(f.Fin!.Value)))
            .ToList();
        HorarioAtencion.Validar(franjas);

        try
        {
            await repositorio.GuardarAsync(configuracion, franjas, cancellationToken);
        }
        catch (ConflictoConcurrenciaException)
        {
            return Resultado.Falla(MensajesCitas.ModificadoPorOtraPersona);
        }

        return Resultado.Ok();
    }
}

/// <summary>Caso de uso: bloqueos vigentes y próximos.</summary>
public sealed class ListarBloqueos(IRepositorioAgenda repositorio, IUsuarioActual usuarioActual, IRelojSistema reloj)
{
    private static readonly TimeSpan Horizonte = TimeSpan.FromDays(400);

    public async Task<IReadOnlyList<BloqueoVista>> EjecutarAsync(CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        var ahora = reloj.AhoraUtc;
        var bloqueos = await repositorio.ListarBloqueosAsync(ahora, ahora + Horizonte, cancellationToken);
        return [.. bloqueos.OrderBy(b => b.Inicio).Select(b => new BloqueoVista(b.Id, b.Inicio, b.Fin, b.Motivo))];
    }
}

/// <summary>Caso de uso: la doctora bloquea un período (vacaciones, feriado...).</summary>
public sealed class CrearBloqueo(
    IRepositorioAgenda repositorioAgenda,
    IRepositorioCitas repositorioCitas,
    IUsuarioActual usuarioActual,
    IRelojSistema reloj,
    IValidator<CrearBloqueoComando> validador)
{
    public async Task<Resultado> EjecutarAsync(CrearBloqueoComando comando, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirDoctoraAsync();

        var validacion = await validador.ValidateAsync(comando, cancellationToken);
        if (!validacion.IsValid)
        {
            return Resultado.Falla(validacion.Errors.Select(e => e.ErrorMessage).Distinct());
        }

        var intervalo = ConversionBloqueo.AIntervalo(comando);
        var ahora = reloj.AhoraUtc;
        if (intervalo.Fin <= ahora)
        {
            return Resultado.Falla("El bloqueo debe terminar en el futuro.");
        }

        // No se bloquea encima de citas activas: primero hay que reprogramarlas o cancelarlas.
        var citas = await repositorioCitas.ListarHorariosOcupadosAsync(intervalo.Inicio, intervalo.Fin, ahora, null, cancellationToken);
        if (citas.Count > 0)
        {
            return Resultado.Falla(MensajesCitas.BloqueoConCitas(citas.Count));
        }

        await repositorioAgenda.AgregarBloqueoAsync(BloqueoAgenda.Crear(intervalo, comando.Motivo), cancellationToken);
        return Resultado.Ok();
    }
}

public sealed class EliminarBloqueo(IRepositorioAgenda repositorio, IUsuarioActual usuarioActual)
{
    public async Task<Resultado> EjecutarAsync(Guid bloqueoId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirDoctoraAsync();
        return await repositorio.EliminarBloqueoAsync(bloqueoId, cancellationToken)
            ? Resultado.Ok()
            : Resultado.Falla(MensajesCitas.BloqueoNoExiste);
    }
}

/// <summary>Caso de uso: horarios libres para agendar o reprogramar desde el panel.</summary>
public sealed class ConsultarDisponibilidad(MotorDisponibilidad motor, IUsuarioActual usuarioActual)
{
    public const int DiasMaximos = 14;

    /// <returns><c>null</c> si la agenda no está configurada.</returns>
    public async Task<IReadOnlyList<DiaDisponible>?> EjecutarAsync(
        DateOnly desde, int dias, Guid? excluirCitaId, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();
        return await motor.CalcularAsync(desde, Math.Clamp(dias, 1, DiasMaximos), excluirCitaId, cancellationToken);
    }
}

/// <summary>Caso de uso: días con horarios libres para la página pública (no requiere sesión).</summary>
public sealed class ConsultarDisponibilidadPublica(MotorDisponibilidad motor, IRepositorioAgenda repositorioAgenda)
{
    /// <summary>Tope de días que se calculan en una sola consulta pública.</summary>
    public const int DiasMaximos = 60;

    /// <returns><c>null</c> si no se reciben solicitudes en línea.</returns>
    public async Task<IReadOnlyList<DiaDisponible>?> EjecutarAsync(CancellationToken cancellationToken)
    {
        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        if (agenda is null || !agenda.Configuracion.SolicitudesWebHabilitadas)
        {
            return null;
        }

        var dias = Math.Min(agenda.Configuracion.AnticipacionMaximaDias + 1, DiasMaximos);
        var calendario = await motor.CalcularAsync(motor.HoyEnEcuador(), dias, null, cancellationToken);
        return calendario?.Where(d => d.Horarios.Count > 0).ToList();
    }
}

/// <summary>Caso de uso: agenda de uno a siete días (citas no canceladas, bloqueos y horario de atención).</summary>
public sealed class ObtenerAgenda(IRepositorioAgenda repositorioAgenda, IRepositorioCitas repositorioCitas, IUsuarioActual usuarioActual)
{
    public async Task<AgendaVista> EjecutarAsync(DateOnly desde, int dias, CancellationToken cancellationToken)
    {
        await usuarioActual.ExigirSesionAsync();

        dias = Math.Clamp(dias, 1, 7);
        var inicioUtc = CalculadoraDisponibilidad.InicioDelDiaUtc(desde);
        var finUtc = CalculadoraDisponibilidad.InicioDelDiaUtc(desde.AddDays(dias));

        var agenda = await repositorioAgenda.ObtenerAsync(cancellationToken);
        var citas = await repositorioCitas.ListarEnRangoAsync(inicioUtc, finUtc, cancellationToken);
        var bloqueos = await repositorioAgenda.ListarBloqueosAsync(inicioUtc, finUtc, cancellationToken);

        return new AgendaVista(
            desde,
            dias,
            agenda is not null,
            agenda?.Configuracion.DuracionCitaMinutos ?? 0,
            agenda?.Franjas.Select(f => new FranjaVista(f.DiaSemana, f.Inicio, f.Fin)).ToList() ?? [],
            [.. citas.Where(c => c.Estado != EstadoCita.Cancelada).OrderBy(c => c.InicioUtc)],
            [.. bloqueos.Select(b => new BloqueoVista(b.Id, b.Inicio, b.Fin, b.Motivo))]);
    }
}
