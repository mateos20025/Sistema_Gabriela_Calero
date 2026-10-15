using Consultorio.Application.Citas;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Tests.Citas;

public class CitasTests
{
    private readonly RepositorioCitasEnMemoria citas = new();
    private readonly RepositorioPacientesEnMemoria pacientes = new();
    private readonly RepositorioAgendaEnMemoria agenda = DatosCitas.AgendaConfigurada();
    private readonly Paciente paciente = DatosCitas.Paciente();
    private RelojFijo reloj = new(DatosCitas.Ahora);

    public CitasTests() => pacientes.Pacientes.Add(paciente);

    private MotorDisponibilidad Motor() => new(agenda, citas, reloj);

    private ExpirarReservasVencidas Expirar() => new(citas, reloj);

    private AgendarCita Agendar() =>
        new(citas, pacientes, agenda, Motor(), Expirar(), UsuarioActualFalso.Asistente(), reloj, new AgendarCitaValidador());

    private CambiarEstadoCita CambiarEstado() => new(citas, UsuarioActualFalso.Asistente(), reloj);

    private ReprogramarCita Reprogramar() => new(citas, agenda, Motor(), Expirar(), UsuarioActualFalso.Asistente(), reloj);

    private AgendarCitaComando ComandoAgendar(int hora, int minuto = 0) =>
        new() { PacienteId = paciente.Id, InicioUtc = DatosCitas.LunesA(hora, minuto), Observacion = "" };

    [Fact]
    public async Task Agenda_en_un_horario_libre_y_la_cita_queda_por_llamar()
    {
        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9, 30), CancellationToken.None);

        Assert.True(resultado.Exito);
        var cita = Assert.Single(citas.Citas);
        Assert.Equal(EstadoCita.EnCola, cita.Estado);
        Assert.Equal(DatosCitas.LunesA(10), cita.Fin);
        Assert.Equal(OrigenRegistro.Panel, cita.Origen);
    }

    [Fact]
    public async Task No_agenda_en_un_horario_ocupado()
    {
        citas.Citas.Add(DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(9)));

        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
    }

    [Theory]
    [InlineData(9, 10)] // no coincide con el inicio de un horario
    [InlineData(13, 0)] // fuera del horario de atención
    public async Task No_agenda_horarios_que_no_estan_en_la_lista_de_libres(int hora, int minuto)
    {
        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(hora, minuto), CancellationToken.None);

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
        Assert.Empty(citas.Citas);
    }

    [Fact]
    public async Task Si_otra_persona_toma_el_horario_al_mismo_tiempo_avisa()
    {
        citas.LanzarHorarioOcupadoAlAgregar = true;

        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
    }

    [Fact]
    public async Task No_agenda_a_un_paciente_deshabilitado()
    {
        paciente.Deshabilitar(DatosCitas.Ahora);

        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        Assert.Equal([MensajesCitas.PacienteDeshabilitado], resultado.Errores);
    }

    [Fact]
    public async Task Sin_agenda_configurada_no_se_puede_agendar()
    {
        agenda.Configuracion = null;

        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        Assert.Equal([MensajesCitas.AgendaNoConfigurada], resultado.Errores);
    }

    [Fact]
    public async Task Sin_sesion_no_se_puede_agendar()
    {
        var caso = new AgendarCita(
            citas, pacientes, agenda, Motor(), Expirar(), UsuarioActualFalso.SinSesion(), reloj, new AgendarCitaValidador());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => caso.EjecutarAsync(ComandoAgendar(9), CancellationToken.None));
    }

    [Fact]
    public async Task Una_reserva_web_vencida_libera_el_horario_al_agendar()
    {
        var vencida = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(9), creadaUtc: DatosCitas.Ahora.AddHours(-30));
        citas.Citas.Add(vencida);

        var resultado = await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoCita.Cancelada, vencida.Estado);
    }

    [Fact]
    public async Task Sigue_el_flujo_de_la_cola_hasta_confirmar()
    {
        var cita = DatosCitas.Solicitada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        Assert.True((await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.EnCola, null, CancellationToken.None)).Exito);
        Assert.True((await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.NoContesta, "Buzón de voz", CancellationToken.None)).Exito);
        Assert.True((await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.EnCola, null, CancellationToken.None)).Exito);
        Assert.True((await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Confirmada, null, CancellationToken.None)).Exito);

        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
        Assert.Null(cita.ReservaExpiraEn);
        Assert.Equal(5, cita.CambiosNuevos.Count);
    }

    [Fact]
    public async Task No_permite_saltarse_la_cola()
    {
        var cita = DatosCitas.Solicitada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Confirmada, null, CancellationToken.None);

        Assert.Equal([MensajesCitas.TransicionNoPermitida(EstadoCita.Pendiente, EstadoCita.Confirmada)], resultado.Errores);
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
    }

    [Fact]
    public async Task Cancelar_exige_el_motivo()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Cancelada, "  ", CancellationToken.None);

        Assert.Equal([MensajesCitas.MotivoCancelacionObligatorio], resultado.Errores);
    }

    [Fact]
    public async Task No_marca_atendida_antes_de_la_hora_de_la_cita()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Atendida, null, CancellationToken.None);

        Assert.Equal([MensajesCitas.AunNoEsHora], resultado.Errores);
    }

    [Fact]
    public async Task Marca_atendida_despues_de_la_hora_de_la_cita()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);
        reloj = new RelojFijo(DatosCitas.LunesA(9, 10));

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Atendida, null, CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoCita.Atendida, cita.Estado);
    }

    [Fact]
    public async Task Avisa_si_otra_persona_cambio_la_cita()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);
        citas.LanzarConflictoAlActualizar = true;

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Cancelada, "Viaje", CancellationToken.None);

        Assert.Equal([MensajesCitas.ModificadoPorOtraPersona], resultado.Errores);
    }

    [Fact]
    public async Task Reprogramar_por_cambio_de_estado_no_esta_permitido()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await CambiarEstado().EjecutarAsync(cita.Id, EstadoCita.Reprogramada, null, CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
    }

    [Fact]
    public async Task Reprograma_a_otro_horario_libre_y_guarda_el_historial()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await Reprogramar().EjecutarAsync(cita.Id, DatosCitas.LunesA(11), "Pidió más tarde", CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoCita.Reprogramada, cita.Estado);
        Assert.Equal(DatosCitas.LunesA(11), cita.Inicio);
        var cambio = cita.CambiosNuevos[^1];
        Assert.Equal(DatosCitas.LunesA(9), cambio.InicioAnterior);
        Assert.Equal(DatosCitas.LunesA(11), cambio.InicioNuevo);
    }

    [Fact]
    public async Task No_reprograma_al_mismo_horario()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await Reprogramar().EjecutarAsync(cita.Id, DatosCitas.LunesA(9), null, CancellationToken.None);

        Assert.Equal([MensajesCitas.MismoHorario], resultado.Errores);
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
    }

    [Fact]
    public async Task No_reprograma_a_un_horario_ocupado()
    {
        var cita = DatosCitas.Confirmada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.AddRange([cita, DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(10))]);

        var resultado = await Reprogramar().EjecutarAsync(cita.Id, DatosCitas.LunesA(10), null, CancellationToken.None);

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
        Assert.Equal(DatosCitas.LunesA(9), cita.Inicio);
    }

    [Fact]
    public async Task No_reprograma_una_solicitud_que_no_paso_por_la_cola()
    {
        var cita = DatosCitas.Solicitada(paciente.Id, DatosCitas.HorarioLunes(9));
        citas.Citas.Add(cita);

        var resultado = await Reprogramar().EjecutarAsync(cita.Id, DatosCitas.LunesA(11), null, CancellationToken.None);

        Assert.Equal([MensajesCitas.NoSePuedeReprogramar], resultado.Errores);
    }

    [Fact]
    public async Task Expira_solo_las_reservas_web_vencidas()
    {
        var vencida = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(9), creadaUtc: DatosCitas.Ahora.AddHours(-25));
        var vigente = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(10), creadaUtc: DatosCitas.Ahora.AddHours(-2));
        var confirmada = DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(11));
        citas.Citas.AddRange([vencida, vigente, confirmada]);

        var liberadas = await Expirar().EjecutarAsync(CancellationToken.None);

        Assert.Equal(1, liberadas);
        Assert.Equal(EstadoCita.Cancelada, vencida.Estado);
        Assert.Equal(ExpirarReservasVencidas.Motivo, vencida.CambiosNuevos[^1].Observacion);
        Assert.Null(vencida.CambiosNuevos[^1].UsuarioId);
        Assert.Equal(EstadoCita.Pendiente, vigente.Estado);
        Assert.Equal(EstadoCita.Confirmada, confirmada.Estado);
    }

    [Fact]
    public async Task La_cita_agendada_desde_el_panel_aparece_en_por_llamar()
    {
        await Agendar().EjecutarAsync(ComandoAgendar(9), CancellationToken.None);

        var cola = await new ListarSolicitudes(citas, UsuarioActualFalso.Asistente()).EjecutarAsync(CancellationToken.None);

        Assert.Single(cola.PorLlamar);
        Assert.Empty(cola.Nuevas);
    }

    [Fact]
    public async Task La_cola_agrupa_las_solicitudes_por_estado()
    {
        var nueva = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(9));
        var enCola = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(10));
        enCola.CambiarEstado(EstadoCita.EnCola, Guid.NewGuid(), DatosCitas.Ahora, null);
        var confirmada = DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(11));
        citas.Citas.AddRange([nueva, enCola, confirmada]);

        var cola = await new ListarSolicitudes(citas, UsuarioActualFalso.Asistente()).EjecutarAsync(CancellationToken.None);

        Assert.Equal(nueva.Id, Assert.Single(cola.Nuevas).Id);
        Assert.Equal(enCola.Id, Assert.Single(cola.PorLlamar).Id);
        Assert.Empty(cola.NoContestaron);
        Assert.Empty(cola.Reprogramadas);
    }
}
