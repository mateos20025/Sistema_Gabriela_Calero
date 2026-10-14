using Consultorio.Application.Comun;

namespace Consultorio.Application.Tests.Comun;

public class HoraEcuadorTests
{
    [Fact]
    public void Resta_cinco_horas_a_la_hora_UTC()
    {
        var utc = new DateTime(2026, 10, 13, 3, 30, 0, DateTimeKind.Utc);

        var ecuador = HoraEcuador.DesdeUtc(utc);

        Assert.Equal(new DateTime(2026, 10, 12, 22, 30, 0), ecuador);
    }

    [Fact]
    public void Ida_y_vuelta_devuelve_la_misma_hora_UTC()
    {
        var utc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var resultado = HoraEcuador.AUtc(HoraEcuador.DesdeUtc(utc));

        Assert.Equal(utc, resultado);
        Assert.Equal(DateTimeKind.Utc, resultado.Kind);
    }

    [Fact]
    public void Rechaza_fechas_en_hora_local_del_servidor()
    {
        var local = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => HoraEcuador.DesdeUtc(local));
    }
}
