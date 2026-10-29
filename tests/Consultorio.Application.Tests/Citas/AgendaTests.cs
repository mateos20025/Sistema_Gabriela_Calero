using Consultorio.Application.Citas;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Citas;

namespace Consultorio.Application.Tests.Citas;

public class AgendaTests
{
    private readonly RepositorioCitasEnMemoria citas = new();
    private readonly RelojFijo reloj = new(DatosCitas.Ahora);

    private static ConfiguracionAgendaComando ComandoConfiguracion(params FranjaComando[] franjas) => new()
    {
        DuracionCitaMinutos = 30,
        AnticipacionMinimaHoras = 2,
        AnticipacionMaximaDias = 60,
        HorasReservaSolicitud = 24,
        MaximoSolicitudesActivasPorCedula = 2,
        SolicitudesWebHabilitadas = true,
        Franjas = [.. franjas],
    };

    private static FranjaComando Franja(DayOfWeek dia, int desde, int hasta) =>
        new() { DiaSemana = dia, Inicio = TimeSpan.FromHours(desde), Fin = TimeSpan.FromHours(hasta) };

    private static ActualizarConfiguracionAgenda Actualizar(RepositorioAgendaEnMemoria agenda, UsuarioActualFalso usuario) =>
        new(agenda, usuario, new ConfiguracionAgendaValidador());

    [Fact]
    public async Task La_doctora_configura_la_agenda_por_primera_vez()
    {
        var agenda = new RepositorioAgendaEnMemoria();
        var comando = ComandoConfiguracion(Franja(DayOfWeek.Monday, 9, 12), Franja(DayOfWeek.Monday, 15, 18));

        var resultado = await Actualizar(agenda, UsuarioActualFalso.Doctora()).EjecutarAsync(comando, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(30, agenda.Configuracion!.DuracionCitaMinutos);
        Assert.Equal(2, agenda.Franjas.Count);
    }

    [Fact]
    public async Task La_asistente_no_puede_cambiar_la_configuracion()
    {
        var agenda = new RepositorioAgendaEnMemoria();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Actualizar(agenda, UsuarioActualFalso.Asistente()).EjecutarAsync(ComandoConfiguracion(), CancellationToken.None));
        Assert.Equal(0, agenda.Guardados);
    }

    [Fact]
    public async Task Rechaza_franjas_que_se_cruzan_el_mismo_dia()
    {
        var agenda = new RepositorioAgendaEnMemoria();
        var comando = ComandoConfiguracion(Franja(DayOfWeek.Monday, 9, 12), Franja(DayOfWeek.Monday, 11, 13));

        var resultado = await Actualizar(agenda, UsuarioActualFalso.Doctora()).EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal(0, agenda.Guardados);
    }

    [Fact]
    public async Task Rechaza_franjas_identicas()
    {
        var agenda = new RepositorioAgendaEnMemoria();
        var comando = ComandoConfiguracion(Franja(DayOfWeek.Friday, 9, 12), Franja(DayOfWeek.Friday, 9, 12));

        var resultado = await Actualizar(agenda, UsuarioActualFalso.Doctora()).EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task Detecta_que_otra_persona_cambio_la_configuracion()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var comando = ComandoConfiguracion(Franja(DayOfWeek.Monday, 9, 12));
        comando.Version = 99;

        var resultado = await Actualizar(agenda, UsuarioActualFalso.Doctora()).EjecutarAsync(comando, CancellationToken.None);

        Assert.Equal([MensajesCitas.ModificadoPorOtraPersona], resultado.Errores);
    }

    [Fact]
    public async Task Rechaza_una_duracion_que_no_es_multiplo_de_5()
    {
        var agenda = new RepositorioAgendaEnMemoria();
        var comando = ComandoConfiguracion(Franja(DayOfWeek.Monday, 9, 12));
        comando.DuracionCitaMinutos = 32;

        var resultado = await Actualizar(agenda, UsuarioActualFalso.Doctora()).EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
    }

    private CrearBloqueo CrearBloqueo(RepositorioAgendaEnMemoria agenda, UsuarioActualFalso? usuario = null) =>
        new(agenda, citas, usuario ?? UsuarioActualFalso.Doctora(), reloj, new CrearBloqueoValidador());

    private static CrearBloqueoComando BloqueoDelLunes() => new()
    {
        Desde = DatosCitas.Lunes.ToDateTime(TimeOnly.MinValue),
        Hasta = DatosCitas.Lunes.ToDateTime(TimeOnly.MinValue),
        DiaCompleto = true,
        Motivo = "Congreso médico",
    };

    [Fact]
    public async Task Un_bloqueo_de_dia_completo_quita_todos_los_horarios_de_ese_dia()
    {
        var agenda = DatosCitas.AgendaConfigurada();

        var resultado = await CrearBloqueo(agenda).EjecutarAsync(BloqueoDelLunes(), CancellationToken.None);

        Assert.True(resultado.Exito);
        var motor = new MotorDisponibilidad(agenda, citas, reloj);
        var dia = Assert.Single((await motor.CalcularAsync(DatosCitas.Lunes, 1, null, CancellationToken.None))!);
        Assert.Empty(dia.Horarios);
    }

    [Fact]
    public async Task Un_bloqueo_por_horas_usa_la_hora_de_Ecuador()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var comando = BloqueoDelLunes();
        comando.DiaCompleto = false;
        comando.HoraDesde = TimeSpan.FromHours(10);
        comando.HoraHasta = TimeSpan.FromHours(11);

        await CrearBloqueo(agenda).EjecutarAsync(comando, CancellationToken.None);

        var bloqueo = Assert.Single(agenda.Bloqueos);
        Assert.Equal(DatosCitas.LunesA(10), bloqueo.Inicio);
        Assert.Equal(DatosCitas.LunesA(11), bloqueo.Fin);
    }

    [Fact]
    public async Task No_bloquea_un_periodo_con_citas_activas()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        citas.Citas.Add(DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(10)));

        var resultado = await CrearBloqueo(agenda).EjecutarAsync(BloqueoDelLunes(), CancellationToken.None);

        Assert.Equal([MensajesCitas.BloqueoConCitas(1)], resultado.Errores);
        Assert.Empty(agenda.Bloqueos);
    }

    [Fact]
    public async Task No_bloquea_un_periodo_que_ya_paso()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var ayer = DateOnly.FromDateTime(DatosCitas.Ahora).AddDays(-1).ToDateTime(TimeOnly.MinValue);
        var comando = new CrearBloqueoComando { Desde = ayer, Hasta = ayer, Motivo = "Feriado" };

        var resultado = await CrearBloqueo(agenda).EjecutarAsync(comando, CancellationToken.None);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public async Task La_asistente_no_puede_bloquear_la_agenda()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            CrearBloqueo(DatosCitas.AgendaConfigurada(), UsuarioActualFalso.Asistente()).EjecutarAsync(BloqueoDelLunes(), CancellationToken.None));
    }

    [Fact]
    public async Task La_doctora_elimina_un_bloqueo()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var bloqueo = BloqueoAgenda.Crear(DatosCitas.HorarioLunes(9), "Trámite");
        agenda.Bloqueos.Add(bloqueo);
        var caso = new EliminarBloqueo(agenda, UsuarioActualFalso.Doctora());

        Assert.True((await caso.EjecutarAsync(bloqueo.Id, CancellationToken.None)).Exito);
        Assert.Equal([MensajesCitas.BloqueoNoExiste], (await caso.EjecutarAsync(bloqueo.Id, CancellationToken.None)).Errores);
    }

    [Fact]
    public async Task La_disponibilidad_publica_es_nula_si_no_se_reciben_solicitudes_web()
    {
        var agenda = DatosCitas.AgendaConfigurada(web: false);
        var caso = new ConsultarDisponibilidadPublica(new MotorDisponibilidad(agenda, citas, reloj), agenda);

        Assert.Null(await caso.EjecutarAsync(CancellationToken.None));
    }

    [Fact]
    public async Task La_disponibilidad_publica_solo_devuelve_dias_con_horarios()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var caso = new ConsultarDisponibilidadPublica(new MotorDisponibilidad(agenda, citas, reloj), agenda);

        var dias = await caso.EjecutarAsync(CancellationToken.None);

        Assert.NotNull(dias);
        Assert.NotEmpty(dias);
        Assert.All(dias, d => Assert.Equal(DayOfWeek.Monday, d.Fecha.DayOfWeek));
        Assert.Equal(DatosCitas.Lunes, dias[0].Fecha);
    }

    [Fact]
    public async Task La_agenda_muestra_las_citas_no_canceladas_del_rango()
    {
        var agenda = DatosCitas.AgendaConfigurada();
        var activa = DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(9));
        var cancelada = DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(10));
        cancelada.CambiarEstado(EstadoCita.Cancelada, Guid.NewGuid(), DatosCitas.Ahora, "El paciente viaja");
        citas.Citas.AddRange([activa, cancelada]);
        var caso = new ObtenerAgenda(agenda, citas, UsuarioActualFalso.Asistente());

        var vista = await caso.EjecutarAsync(DatosCitas.Lunes, 1, CancellationToken.None);

        Assert.True(vista.Configurada);
        Assert.Equal(activa.Id, Assert.Single(vista.Citas).Id);
    }

    [Fact]
    public async Task El_horario_publico_va_de_lunes_a_domingo_y_esta_vacio_sin_configurar()
    {
        var sinConfigurar = await new ObtenerHorarioPublico(new RepositorioAgendaEnMemoria()).EjecutarAsync(CancellationToken.None);
        Assert.Empty(sinConfigurar);

        var agenda = DatosCitas.AgendaConfigurada();
        agenda.Franjas.Add(FranjaAtencion.Crear(DayOfWeek.Sunday, new TimeOnly(8, 0), new TimeOnly(10, 0)));

        var horario = await new ObtenerHorarioPublico(agenda).EjecutarAsync(CancellationToken.None);

        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Sunday], horario.Select(f => f.DiaSemana));
    }

    [Fact]
    public async Task Sin_configurar_la_disponibilidad_del_panel_es_nula()
    {
        var agenda = new RepositorioAgendaEnMemoria();
        var caso = new ConsultarDisponibilidad(new MotorDisponibilidad(agenda, citas, reloj), UsuarioActualFalso.Asistente());

        Assert.Null(await caso.EjecutarAsync(DatosCitas.Lunes, 7, null, CancellationToken.None));
    }
}
