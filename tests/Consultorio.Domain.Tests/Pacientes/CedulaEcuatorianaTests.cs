using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Tests.Pacientes;

public class CedulaEcuatorianaTests
{
    [Theory]
    [InlineData("1710034065")] // Pichincha
    [InlineData("0102030400")] // Azuay
    [InlineData("2400000002")] // Santa Elena (provincia 24, la última)
    [InlineData("3000000004")] // Ecuatorianos registrados en el exterior
    public void Acepta_cedulas_validas(string cedula)
    {
        Assert.True(CedulaEcuatoriana.EsValida(cedula));
    }

    [Theory]
    [InlineData("1710034064", "dígito verificador incorrecto")]
    [InlineData("0010034065", "provincia 00")]
    [InlineData("2510034065", "provincia 25 no existe")]
    [InlineData("1760034065", "tercer dígito 6 (no es persona natural)")]
    [InlineData("171003406", "9 dígitos")]
    [InlineData("17100340651", "11 dígitos")]
    [InlineData("17100340AB", "letras")]
    [InlineData("", "vacía")]
    [InlineData(null, "nula")]
    public void Rechaza_cedulas_invalidas(string? cedula, string motivo)
    {
        Assert.False(CedulaEcuatoriana.EsValida(cedula), motivo);
    }

    [Theory]
    [InlineData("171003406-5")]
    [InlineData(" 1710034065 ")]
    [InlineData("17 1003 4065")]
    public void Normaliza_quitando_espacios_y_guiones(string cedula)
    {
        Assert.Equal("1710034065", CedulaEcuatoriana.Normalizar(cedula));
    }
}
