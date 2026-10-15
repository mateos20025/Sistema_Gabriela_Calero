using Consultorio.Application.Citas;
using Consultorio.Application.Comun;
using Consultorio.Application.Tests.Pacientes;
using Consultorio.Domain.Citas;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Application.Tests.Citas;

public class SolicitarCitaTests
{
    private const string Captcha = "respuesta-captcha";

    private readonly RepositorioCitasEnMemoria citas = new();
    private readonly RepositorioPacientesEnMemoria pacientes = new();
    private readonly RelojFijo reloj = new(DatosCitas.Ahora);
    private RepositorioAgendaEnMemoria agenda = DatosCitas.AgendaConfigurada();
    private VerificadorHumanoFalso verificador = new();

    private SolicitarCita CasoDeUso()
    {
        var motor = new MotorDisponibilidad(agenda, citas, reloj);
        return new SolicitarCita(
            citas, pacientes, agenda, motor, new ExpirarReservasVencidas(citas, reloj), verificador, reloj,
            new OpcionesProteccionDatos { VersionPoliticaPrivacidad = "1.0" }, new SolicitarCitaValidador());
    }

    private static SolicitarCitaComando Comando(int hora = 9, string cedula = DatosPrueba.CedulaValida) => new()
    {
        Cedula = cedula,
        Nombres = "Ana Lucía",
        Apellidos = "Torres Mora",
        Telefono = "098 765 4321",
        CorreoElectronico = "",
        ConsentimientoAceptado = true,
        Horario = SolicitarCitaComando.FormatearHorario(DatosCitas.LunesA(hora)),
    };

    private Task<Resultado<SolicitudRegistrada>> Solicitar(SolicitarCitaComando comando) =>
        CasoDeUso().EjecutarAsync(comando, Captcha, "203.0.113.10", CancellationToken.None);

    [Fact]
    public async Task Una_cedula_nueva_crea_el_paciente_con_su_consentimiento_y_la_solicitud()
    {
        var resultado = await Solicitar(Comando());

        Assert.True(resultado.Exito);
        var paciente = Assert.Single(pacientes.Pacientes);
        Assert.Equal(OrigenRegistro.Web, paciente.ConsentimientoOrigen);
        Assert.Equal("1.0", paciente.ConsentimientoVersion);

        var cita = Assert.Single(citas.Citas);
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
        Assert.Equal(OrigenRegistro.Web, cita.Origen);
        Assert.Equal(DatosCitas.Ahora.AddHours(24), cita.ReservaExpiraEn);
        Assert.Equal(DatosCitas.LunesA(9), resultado.Valor!.InicioUtc);
    }

    [Fact]
    public async Task Una_cedula_existente_no_modifica_la_ficha_y_guarda_el_contacto_aparte()
    {
        var existente = DatosPrueba.Paciente();
        pacientes.Pacientes.Add(existente);

        var resultado = await Solicitar(Comando());

        Assert.True(resultado.Exito);
        Assert.Equal("María", existente.Nombres);
        Assert.Equal(0, pacientes.Actualizaciones);
        var cita = Assert.Single(citas.Citas);
        Assert.Equal(existente.Id, cita.PacienteId);
        Assert.Equal("Ana Lucía Torres Mora", cita.ContactoNombre);
        Assert.Equal("+593987654321", cita.ContactoTelefono);
    }

    [Fact]
    public async Task Un_paciente_deshabilitado_recibe_un_mensaje_generico()
    {
        var existente = DatosPrueba.Paciente();
        existente.Deshabilitar(DatosCitas.Ahora);
        pacientes.Pacientes.Add(existente);

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.NoPudimosRegistrar], resultado.Errores);
        Assert.Empty(citas.Citas);
    }

    [Fact]
    public async Task Respeta_el_limite_de_solicitudes_activas_por_cedula()
    {
        agenda = DatosCitas.AgendaConfigurada(maximoActivas: 1);
        var existente = DatosPrueba.Paciente();
        pacientes.Pacientes.Add(existente);
        citas.Citas.Add(DatosCitas.Solicitada(existente.Id, DatosCitas.HorarioLunes(11)));

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.LimiteSolicitudes(1)], resultado.Errores);
        Assert.Single(citas.Citas);
    }

    [Fact]
    public async Task No_acepta_un_horario_ocupado()
    {
        citas.Citas.Add(DatosCitas.Confirmada(Guid.NewGuid(), DatosCitas.HorarioLunes(9)));

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
        Assert.Empty(pacientes.Pacientes);
    }

    [Fact]
    public async Task Una_reserva_vencida_deja_el_horario_libre()
    {
        var vencida = DatosCitas.Solicitada(Guid.NewGuid(), DatosCitas.HorarioLunes(9), creadaUtc: DatosCitas.Ahora.AddHours(-25));
        citas.Citas.Add(vencida);

        var resultado = await Solicitar(Comando());

        Assert.True(resultado.Exito);
        Assert.Equal(EstadoCita.Cancelada, vencida.Estado);
    }

    [Fact]
    public async Task Si_otra_persona_toma_el_horario_al_mismo_tiempo_avisa()
    {
        citas.LanzarHorarioOcupadoAlAgregar = true;

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.HorarioNoDisponible], resultado.Errores);
    }

    [Fact]
    public async Task Sin_captcha_valido_no_registra_nada()
    {
        verificador = new VerificadorHumanoFalso(aprueba: false);

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.VerificacionFallida], resultado.Errores);
        Assert.Empty(pacientes.Pacientes);
        Assert.Empty(citas.Citas);
    }

    [Fact]
    public async Task Con_el_captcha_deshabilitado_no_se_verifica()
    {
        verificador = new VerificadorHumanoFalso(habilitado: false);

        var resultado = await Solicitar(Comando());

        Assert.True(resultado.Exito);
        Assert.Equal(0, verificador.Verificaciones);
    }

    [Fact]
    public async Task Si_la_doctora_no_recibe_solicitudes_web_las_rechaza()
    {
        agenda = DatosCitas.AgendaConfigurada(web: false);

        var resultado = await Solicitar(Comando());

        Assert.Equal([MensajesCitas.SolicitudesWebDeshabilitadas], resultado.Errores);
        Assert.Equal(0, verificador.Verificaciones);
    }

    [Fact]
    public async Task Exige_el_consentimiento()
    {
        var comando = Comando();
        comando.ConsentimientoAceptado = false;

        var resultado = await Solicitar(comando);

        Assert.False(resultado.Exito);
        Assert.Empty(pacientes.Pacientes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-fecha")]
    [InlineData("2026-10-19T09:00:00")] // sin zona horaria
    [InlineData("2026-10-19T09:00:00-05:00")] // no es UTC
    public async Task Rechaza_horarios_mal_formados(string horario)
    {
        var comando = Comando();
        comando.Horario = horario;

        var resultado = await Solicitar(comando);

        Assert.False(resultado.Exito);
        Assert.Empty(citas.Citas);
    }

    [Fact]
    public async Task Rechaza_una_cedula_invalida()
    {
        var resultado = await Solicitar(Comando(cedula: "1234567890"));

        Assert.False(resultado.Exito);
        Assert.Empty(pacientes.Pacientes);
    }
}
