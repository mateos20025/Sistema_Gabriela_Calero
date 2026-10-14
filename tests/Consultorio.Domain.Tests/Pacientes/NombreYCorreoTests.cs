using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Tests.Pacientes;

public class NombreYCorreoTests
{
    [Theory]
    [InlineData("  María   José ", "María José")]
    [InlineData("O'Brien", "O'Brien")]
    [InlineData("Ana-María", "Ana-María")]
    [InlineData("Ñañez", "Ñañez")]
    [InlineData("De la Torre", "De la Torre")]
    public void Normaliza_nombres_validos(string nombre, string esperado)
    {
        Assert.Equal(esperado, NombrePersona.Normalizar(nombre));
    }

    [Theory]
    [InlineData("Juan2")]
    [InlineData(".Juan")]
    [InlineData("Juan <script>")]
    [InlineData("")]
    [InlineData(null)]
    public void Rechaza_nombres_invalidos(string? nombre)
    {
        Assert.False(NombrePersona.EsValido(nombre));
    }

    [Fact]
    public void Rechaza_nombres_demasiado_largos()
    {
        Assert.False(NombrePersona.EsValido(new string('a', NombrePersona.LongitudMaxima + 1)));
    }

    [Fact]
    public void Normaliza_el_correo_a_minusculas_y_sin_espacios()
    {
        Assert.True(DireccionCorreo.TryNormalizar(" Ana.Perez@Correo.COM ", out var correo));
        Assert.Equal("ana.perez@correo.com", correo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void El_correo_vacio_es_valido_y_queda_nulo(string? valor)
    {
        Assert.True(DireccionCorreo.TryNormalizar(valor, out var correo));
        Assert.Null(correo);
    }

    [Theory]
    [InlineData("ana@correo")]
    [InlineData("ana@@correo.com")]
    [InlineData("@correo.com")]
    [InlineData("ana perez@correo.com")]
    [InlineData("ana@correo.com.")]
    [InlineData("ana@.com")]
    public void Rechaza_correos_invalidos(string valor)
    {
        Assert.False(DireccionCorreo.EsValidoOVacio(valor));
    }
}
