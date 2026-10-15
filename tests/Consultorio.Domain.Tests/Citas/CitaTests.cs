using Consultorio.Domain.Citas;
using Consultorio.Domain.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Tests.Citas;

public class CitaTests
{
    // Jueves 15/10/2026 07:00 en Ecuador.
    private static readonly DateTime Ahora = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid PacienteId = Guid.NewGuid();
    private static readonly Guid Asistente = Guid.NewGuid();

    // Lunes 19/10/2026 de 09:00 a 09:30 en Ecuador.
    private static readonly IntervaloUtc Horario = new(
        new DateTime(2026, 10, 19, 14, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 19, 14, 30, 0, DateTimeKind.Utc));

    private static Cita DelPanel() => Cita.AgendarDesdePanel(PacienteId, Horario, Asistente, Ahora, null);

    private static Cita Confirmada()
    {
        var cita = DelPanel();
        cita.CambiarEstado(EstadoCita.Confirmada, Asistente, Ahora, null);
        return cita;
    }

    private static Cita DeLaWeb() => Cita.SolicitarDesdeWeb(
        PacienteId, Horario, Ahora, horasReserva: 24, new ContactoSolicitud(" Ana ", "Ruiz", "099 123 4567", "Ana@Correo.com"));

    [Theory]
    [InlineData(EstadoCita.Pendiente, EstadoCita.EnCola)]
    [InlineData(EstadoCita.Pendiente, EstadoCita.Cancelada)]
    [InlineData(EstadoCita.EnCola, EstadoCita.Confirmada)]
    [InlineData(EstadoCita.EnCola, EstadoCita.Reprogramada)]
    [InlineData(EstadoCita.EnCola, EstadoCita.Cancelada)]
    [InlineData(EstadoCita.EnCola, EstadoCita.NoContesta)]
    [InlineData(EstadoCita.NoContesta, EstadoCita.EnCola)]
    [InlineData(EstadoCita.NoContesta, EstadoCita.Cancelada)]
    [InlineData(EstadoCita.Confirmada, EstadoCita.Reprogramada)]
    [InlineData(EstadoCita.Confirmada, EstadoCita.Cancelada)]
    [InlineData(EstadoCita.Confirmada, EstadoCita.Atendida)]
    [InlineData(EstadoCita.Confirmada, EstadoCita.NoAsistio)]
    [InlineData(EstadoCita.Reprogramada, EstadoCita.Confirmada)]
    [InlineData(EstadoCita.Reprogramada, EstadoCita.Cancelada)]
    public void Permite_las_transiciones_de_la_guia(EstadoCita desde, EstadoCita hacia)
    {
        Assert.True(ReglasEstadoCita.PuedePasar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoCita.Pendiente, EstadoCita.Confirmada)]
    [InlineData(EstadoCita.EnCola, EstadoCita.Atendida)]
    [InlineData(EstadoCita.NoContesta, EstadoCita.Confirmada)]
    [InlineData(EstadoCita.Reprogramada, EstadoCita.Atendida)]
    [InlineData(EstadoCita.Cancelada, EstadoCita.Confirmada)]
    [InlineData(EstadoCita.Atendida, EstadoCita.Cancelada)]
    [InlineData(EstadoCita.NoAsistio, EstadoCita.Confirmada)]
    public void Rechaza_transiciones_no_permitidas(EstadoCita desde, EstadoCita hacia)
    {
        Assert.False(ReglasEstadoCita.PuedePasar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoCita.Cancelada)]
    [InlineData(EstadoCita.Atendida)]
    [InlineData(EstadoCita.NoAsistio)]
    public void Cancelada_atendida_y_no_asistio_son_finales(EstadoCita estado)
    {
        Assert.True(ReglasEstadoCita.EsFinal(estado));
        Assert.False(ReglasEstadoCita.EsActivo(estado));
    }

    [Fact]
    public void Desde_el_panel_la_cita_queda_en_la_cola_y_con_historial()
    {
        var cita = DelPanel();

        Assert.Equal(EstadoCita.EnCola, cita.Estado);
        Assert.Equal(OrigenRegistro.Panel, cita.Origen);
        Assert.Null(cita.ReservaExpiraEn);
        var cambio = Assert.Single(cita.CambiosNuevos);
        Assert.Null(cambio.EstadoAnterior);
        Assert.Equal(EstadoCita.EnCola, cambio.EstadoNuevo);
        Assert.Equal(Asistente, cambio.UsuarioId);
    }

    [Fact]
    public void Desde_la_web_queda_pendiente_con_reserva_y_contacto()
    {
        var cita = DeLaWeb();

        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
        Assert.Equal(OrigenRegistro.Web, cita.Origen);
        Assert.Equal(Ahora.AddHours(24), cita.ReservaExpiraEn);
        Assert.Equal("Ana Ruiz", cita.ContactoNombre);
        Assert.Equal("+593991234567", cita.ContactoTelefono);
        Assert.Equal("ana@correo.com", cita.ContactoCorreo);
        Assert.Null(Assert.Single(cita.CambiosNuevos).UsuarioId);
    }

    [Fact]
    public void La_reserva_vence_solo_si_sigue_pendiente()
    {
        var cita = DeLaWeb();

        Assert.False(cita.ReservaVencida(Ahora.AddHours(23)));
        Assert.True(cita.ReservaVencida(Ahora.AddHours(24)));

        cita.CambiarEstado(EstadoCita.EnCola, Asistente, Ahora, null);
        Assert.Null(cita.ReservaExpiraEn);
        Assert.False(cita.ReservaVencida(Ahora.AddDays(5)));
    }

    [Fact]
    public void Cada_cambio_de_estado_queda_en_el_historial()
    {
        var cita = DeLaWeb();

        cita.CambiarEstado(EstadoCita.EnCola, Asistente, Ahora, null);
        cita.CambiarEstado(EstadoCita.NoContesta, Asistente, Ahora, "Llamé dos veces");
        cita.CambiarEstado(EstadoCita.EnCola, Asistente, Ahora, null);
        cita.CambiarEstado(EstadoCita.Confirmada, Asistente, Ahora, " Confirmó por teléfono ");

        Assert.Equal(5, cita.CambiosNuevos.Count);
        var ultimo = cita.CambiosNuevos[^1];
        Assert.Equal(EstadoCita.EnCola, ultimo.EstadoAnterior);
        Assert.Equal(EstadoCita.Confirmada, ultimo.EstadoNuevo);
        Assert.Equal("Confirmó por teléfono", ultimo.Observacion);
    }

    [Fact]
    public void La_reserva_no_dura_mas_alla_de_la_hora_de_la_cita()
    {
        // Pedida a las 07:00 para las 09:00 del mismo día, con 24 horas de reserva.
        var mismoDia = new IntervaloUtc(Ahora.AddHours(2), Ahora.AddHours(2.5));

        var cita = Cita.SolicitarDesdeWeb(
            PacienteId, mismoDia, Ahora, horasReserva: 24, new ContactoSolicitud("Ana", "Ruiz", "0991234567", null));

        Assert.Equal(mismoDia.Inicio, cita.ReservaExpiraEn);
    }

    [Fact]
    public void Despues_de_guardar_el_historial_no_se_repite()
    {
        var cita = DelPanel();

        cita.MarcarCambiosGuardados();
        cita.CambiarEstado(EstadoCita.Cancelada, Asistente, Ahora, "Viaje");

        Assert.Equal(EstadoCita.Cancelada, Assert.Single(cita.CambiosNuevos).EstadoNuevo);
    }

    [Fact]
    public void No_permite_saltarse_pasos()
    {
        Assert.Throws<ExcepcionDominio>(() => DeLaWeb().CambiarEstado(EstadoCita.Confirmada, Asistente, Ahora, null));
    }

    [Fact]
    public void Cancelar_exige_motivo()
    {
        var cita = DelPanel();

        Assert.Throws<ExcepcionDominio>(() => cita.CambiarEstado(EstadoCita.Cancelada, Asistente, Ahora, " "));

        cita.CambiarEstado(EstadoCita.Cancelada, Asistente, Ahora, "El paciente viajó");
        Assert.Equal(EstadoCita.Cancelada, cita.Estado);
        Assert.False(cita.EstaActiva);
    }

    [Fact]
    public void Atendida_y_no_asistio_solo_despues_de_la_hora_de_la_cita()
    {
        var cita = Confirmada();

        Assert.Throws<ExcepcionDominio>(() => cita.CambiarEstado(EstadoCita.Atendida, Asistente, Horario.Inicio.AddMinutes(-1), null));

        cita.CambiarEstado(EstadoCita.Atendida, Asistente, Horario.Inicio.AddMinutes(5), null);
        Assert.Equal(EstadoCita.Atendida, cita.Estado);
    }

    [Fact]
    public void Una_cita_del_panel_sin_confirmar_no_se_marca_atendida()
    {
        Assert.Throws<ExcepcionDominio>(() => DelPanel().CambiarEstado(EstadoCita.Atendida, Asistente, Horario.Inicio.AddMinutes(5), null));
    }

    [Fact]
    public void Reprogramar_cambia_el_horario_y_lo_registra()
    {
        var cita = DelPanel();
        var nuevo = new IntervaloUtc(Horario.Inicio.AddDays(1), Horario.Fin.AddDays(1));

        cita.Reprogramar(nuevo, Asistente, Ahora, "Pidió otro día");

        Assert.Equal(EstadoCita.Reprogramada, cita.Estado);
        Assert.Equal(nuevo, cita.Horario);
        var cambio = cita.CambiosNuevos[^1];
        Assert.Equal(Horario.Inicio, cambio.InicioAnterior);
        Assert.Equal(nuevo.Inicio, cambio.InicioNuevo);

        cita.CambiarEstado(EstadoCita.Confirmada, Asistente, Ahora, null);
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
    }

    [Fact]
    public void Una_solicitud_pendiente_no_se_reprograma_ni_se_pasa_a_reprogramada_sin_horario()
    {
        var nuevo = new IntervaloUtc(Horario.Inicio.AddDays(1), Horario.Fin.AddDays(1));

        Assert.Throws<ExcepcionDominio>(() => DeLaWeb().Reprogramar(nuevo, Asistente, Ahora, null));
        Assert.Throws<ExcepcionDominio>(() => DelPanel().CambiarEstado(EstadoCita.Reprogramada, Asistente, Ahora, null));
    }

    [Fact]
    public void El_horario_debe_ser_futuro_y_en_UTC()
    {
        var pasado = new IntervaloUtc(Ahora.AddHours(-2), Ahora.AddHours(-1));

        Assert.Throws<ExcepcionDominio>(() => Cita.AgendarDesdePanel(PacienteId, pasado, Asistente, Ahora, null));
        Assert.Throws<ExcepcionDominio>(() => new IntervaloUtc(DateTime.Now, DateTime.Now.AddHours(1)));
        Assert.Throws<ExcepcionDominio>(() => new IntervaloUtc(Ahora, Ahora));
    }

    [Fact]
    public void Dos_intervalos_contiguos_no_se_cruzan()
    {
        var siguiente = new IntervaloUtc(Horario.Fin, Horario.Fin.AddMinutes(30));
        var cruzado = new IntervaloUtc(Horario.Inicio.AddMinutes(15), Horario.Fin.AddMinutes(15));

        Assert.False(Horario.SeCruzaCon(siguiente));
        Assert.True(Horario.SeCruzaCon(cruzado));
    }
}
