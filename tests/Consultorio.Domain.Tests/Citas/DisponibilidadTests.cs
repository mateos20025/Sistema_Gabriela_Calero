using Consultorio.Domain.Citas;
using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Tests.Citas;

public class DisponibilidadTests
{
    // Jueves 15/10/2026 07:00 en Ecuador (12:00 UTC).
    private static readonly DateTime Ahora = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Lunes = new(2026, 10, 19);

    private static readonly IReadOnlyList<FranjaAtencion> Franjas =
    [
        FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
        FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(15, 0), new TimeOnly(16, 0)),
    ];

    private static ConfiguracionAgenda Configuracion(int duracion = 30, int minimaHoras = 0, int maximaDias = 30) =>
        ConfiguracionAgenda.Crear(duracion, minimaHoras, maximaDias, 24, 2, true);

    /// <summary>Hora de Ecuador del lunes de prueba, en UTC.</summary>
    private static DateTime LunesA(int hora, int minuto = 0) =>
        new(2026, 10, 19, hora + 5, minuto, 0, DateTimeKind.Utc);

    private static IReadOnlyList<IntervaloUtc> Libres(
        ConfiguracionAgenda? configuracion = null,
        IReadOnlyList<IntervaloUtc>? bloqueos = null,
        IReadOnlyList<IntervaloUtc>? ocupados = null,
        DateTime? ahora = null,
        DateOnly? fecha = null) =>
        CalculadoraDisponibilidad.HorariosLibres(
            fecha ?? Lunes, configuracion ?? Configuracion(), Franjas, bloqueos ?? [], ocupados ?? [], ahora ?? Ahora);

    [Fact]
    public void Genera_los_horarios_de_cada_franja_en_saltos_de_la_duracion()
    {
        var libres = Libres();

        Assert.Equal(8, libres.Count); // 6 en la mañana + 2 en la tarde
        Assert.Equal(LunesA(9), libres[0].Inicio);
        Assert.Equal(LunesA(9, 30), libres[0].Fin);
        Assert.Equal(LunesA(11, 30), libres[5].Inicio);
        Assert.Equal(LunesA(15), libres[6].Inicio);
    }

    [Fact]
    public void Quita_los_horarios_bloqueados_y_los_ocupados()
    {
        var bloqueo = new IntervaloUtc(LunesA(10), LunesA(11));
        var ocupado = new IntervaloUtc(LunesA(9, 30), LunesA(10));

        var libres = Libres(bloqueos: [bloqueo], ocupados: [ocupado]);

        Assert.Equal(5, libres.Count);
        Assert.DoesNotContain(libres, h => h.SeCruzaCon(bloqueo) || h.SeCruzaCon(ocupado));
    }

    [Fact]
    public void Respeta_la_anticipacion_minima()
    {
        // Lunes 08:50 en Ecuador, con 2 horas de anticipación: el primer horario posible es 11:00.
        var libres = Libres(Configuracion(minimaHoras: 2), ahora: LunesA(8, 50));

        Assert.Equal(LunesA(11), libres[0].Inicio);
    }

    [Fact]
    public void Sin_anticipacion_minima_no_ofrece_el_horario_que_empieza_justo_ahora()
    {
        var libres = Libres(Configuracion(minimaHoras: 0), ahora: LunesA(9));

        Assert.Equal(LunesA(9, 30), libres[0].Inicio);
    }

    [Fact]
    public void Respeta_la_anticipacion_maxima_y_no_ofrece_dias_pasados()
    {
        Assert.Empty(Libres(Configuracion(maximaDias: 3)));            // el lunes está a 4 días
        Assert.Empty(Libres(fecha: new DateOnly(2026, 10, 12)));         // lunes anterior
        Assert.Empty(Libres(fecha: new DateOnly(2026, 10, 20)));         // martes sin franjas
    }

    [Fact]
    public void No_ofrece_un_horario_que_no_cabe_completo_en_la_franja()
    {
        var franjaCorta = new[] { FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(9, 50)) };

        var libres = CalculadoraDisponibilidad.HorariosLibres(Lunes, Configuracion(), franjaCorta, [], [], Ahora);

        Assert.Single(libres);
    }

    [Fact]
    public void Solo_esta_libre_un_horario_exacto_de_la_lista()
    {
        var configuracion = Configuracion();

        Assert.True(CalculadoraDisponibilidad.EstaLibre(new IntervaloUtc(LunesA(9), LunesA(9, 30)), configuracion, Franjas, [], [], Ahora));
        Assert.False(CalculadoraDisponibilidad.EstaLibre(new IntervaloUtc(LunesA(9, 10), LunesA(9, 40)), configuracion, Franjas, [], [], Ahora));
        Assert.False(CalculadoraDisponibilidad.EstaLibre(new IntervaloUtc(LunesA(13), LunesA(13, 30)), configuracion, Franjas, [], [], Ahora));
    }

    [Fact]
    public void Convierte_fechas_entre_UTC_y_Ecuador()
    {
        // 03:00 UTC del 20 de octubre todavía es 19 de octubre en Ecuador.
        Assert.Equal(Lunes, CalculadoraDisponibilidad.FechaEcuador(new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateTime(2026, 10, 19, 5, 0, 0, DateTimeKind.Utc), CalculadoraDisponibilidad.InicioDelDiaUtc(Lunes));
    }

    [Fact]
    public void Valida_franjas_y_detecta_cruces()
    {
        Assert.Throws<ExcepcionDominio>(() => FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(9, 0)));
        Assert.Throws<ExcepcionDominio>(() => FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(9, 7), new TimeOnly(10, 0)));

        var cruzadas = new[]
        {
            FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            FranjaAtencion.Crear(DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(13, 0)),
        };
        Assert.NotNull(HorarioAtencion.Problema(cruzadas));
        Assert.Null(HorarioAtencion.Problema(Franjas));
    }

    [Theory]
    [InlineData(5, 0, 30, 24, 2)]     // duración muy corta
    [InlineData(32, 0, 30, 24, 2)]    // no es múltiplo de 5
    [InlineData(30, -1, 30, 24, 2)]   // anticipación negativa
    [InlineData(30, 0, 0, 24, 2)]     // sin días hacia adelante
    [InlineData(30, 0, 30, 0, 2)]     // reserva de 0 horas
    [InlineData(30, 0, 30, 24, 0)]    // 0 citas por paciente
    public void Rechaza_configuraciones_invalidas(int duracion, int minima, int maxima, int reserva, int maximoCitas)
    {
        Assert.Throws<ExcepcionDominio>(() => ConfiguracionAgenda.Crear(duracion, minima, maxima, reserva, maximoCitas, true));
    }

    [Fact]
    public void Un_bloqueo_exige_motivo()
    {
        var intervalo = new IntervaloUtc(LunesA(9), LunesA(12));

        Assert.Throws<ExcepcionDominio>(() => BloqueoAgenda.Crear(intervalo, " "));
        Assert.Equal("Congreso", BloqueoAgenda.Crear(intervalo, " Congreso ").Motivo);
    }
}
