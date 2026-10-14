using Consultorio.Application.Cuenta;
using Consultorio.Application.Usuarios;
using FluentValidation.TestHelper;

namespace Consultorio.Application.Tests.Usuarios;

public class GeneradorPasswordTemporalTests
{
    [Fact]
    public void Las_contrasenas_generadas_cumplen_la_politica()
    {
        var validador = new CambiarPasswordValidador();

        // Se repite muchas veces porque el resultado es aleatorio.
        for (var i = 0; i < 500; i++)
        {
            var password = GeneradorPasswordTemporal.Generar();
            var comando = new CambiarPasswordComando
            {
                PasswordActual = "OtraDistinta1",
                NuevaPassword = password,
                ConfirmarPassword = password,
            };

            validador.TestValidate(comando).ShouldNotHaveAnyValidationErrors();
        }
    }

    [Fact]
    public void Tiene_la_longitud_esperada_y_no_usa_caracteres_confusos()
    {
        for (var i = 0; i < 500; i++)
        {
            var password = GeneradorPasswordTemporal.Generar();

            Assert.Equal(GeneradorPasswordTemporal.Longitud, password.Length);
            Assert.True(password.Length >= PoliticaPassword.LongitudMinima);
            Assert.DoesNotContain(password, c => "0O1lI".Contains(c));
        }
    }

    [Fact]
    public void No_repite_contrasenas()
    {
        var generadas = Enumerable.Range(0, 1000).Select(_ => GeneradorPasswordTemporal.Generar()).ToHashSet();

        Assert.Equal(1000, generadas.Count);
    }
}
