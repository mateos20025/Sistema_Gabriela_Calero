using Consultorio.Domain.Comun;
using Consultorio.Domain.Pagos;

namespace Consultorio.Domain.Tests.Pagos;

public class ConfiguracionPagosTests
{
    private static DatosBancarios Datos(string identificacion = "1710034065", string numero = "2201 234-567") =>
        new("Banco del Pichincha", TipoCuentaBancaria.Ahorros, numero, "Gabriela  Calero", identificacion);

    [Fact]
    public void Normaliza_los_datos_bancarios()
    {
        var datos = Datos();

        Assert.Equal("2201234567", datos.NumeroCuenta);
        Assert.Equal("Gabriela Calero", datos.Titular);
        Assert.Equal("1710034065", datos.IdentificacionTitular);
    }

    [Theory]
    [InlineData("1710034065")]    // cédula
    [InlineData("1710034065001")] // RUC de persona natural
    [InlineData("1790012345001")] // RUC de sociedad
    public void Acepta_cedula_o_ruc(string identificacion)
    {
        Assert.Equal(identificacion, Datos(identificacion).IdentificacionTitular);
    }

    [Theory]
    [InlineData("1710034064")]    // cédula con verificador incorrecto
    [InlineData("1710034065002")] // RUC que no termina en 001
    [InlineData("12345")]
    public void Rechaza_identificaciones_invalidas(string identificacion)
    {
        Assert.Throws<ExcepcionDominio>(() => Datos(identificacion));
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("12345678901234567890123")]
    [InlineData("22A01")]
    public void Rechaza_numeros_de_cuenta_invalidos(string numero)
    {
        Assert.Throws<ExcepcionDominio>(() => Datos(numero: numero));
    }

    [Fact]
    public void Actualizar_guarda_precio_concepto_y_cuenta()
    {
        var configuracion = ConfiguracionPagos.Nueva();
        Assert.False(configuracion.TieneDatosBancarios);

        configuracion.Actualizar(40m, " Consulta médica ", Datos());

        Assert.Equal(ConfiguracionPagos.IdUnico, configuracion.Id);
        Assert.Equal(40m, configuracion.PrecioConsulta);
        Assert.Equal("Consulta médica", configuracion.ConceptoPredeterminado);
        Assert.True(configuracion.TieneDatosBancarios);
        Assert.Equal(TipoCuentaBancaria.Ahorros, configuracion.TipoCuenta);
    }

    [Fact]
    public void Se_pueden_quitar_el_precio_y_la_cuenta()
    {
        var configuracion = ConfiguracionPagos.Nueva();
        configuracion.Actualizar(40m, "Consulta", Datos());

        configuracion.Actualizar(null, null, null);

        Assert.Null(configuracion.PrecioConsulta);
        Assert.Null(configuracion.ConceptoPredeterminado);
        Assert.False(configuracion.TieneDatosBancarios);
    }

    [Fact]
    public void Rechaza_un_precio_invalido()
    {
        Assert.Throws<ExcepcionDominio>(() => ConfiguracionPagos.Nueva().Actualizar(0m, null, null));
    }
}
