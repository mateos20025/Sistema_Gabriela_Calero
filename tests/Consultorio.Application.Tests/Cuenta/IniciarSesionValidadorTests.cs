using Consultorio.Application.Cuenta;
using FluentValidation.TestHelper;

namespace Consultorio.Application.Tests.Cuenta;

public class IniciarSesionValidadorTests
{
    private readonly IniciarSesionValidador validador = new();

    [Fact]
    public void Acepta_correo_y_contrasena()
    {
        var comando = new IniciarSesionComando { Email = "asistente@consultorio.ec", Password = "cualquiera" };

        validador.TestValidate(comando).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-es-un-correo")]
    public void Rechaza_correo_vacio_o_invalido(string email)
    {
        var comando = new IniciarSesionComando { Email = email, Password = "cualquiera" };

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Con_correo_vacio_muestra_un_solo_mensaje()
    {
        var comando = new IniciarSesionComando { Email = "", Password = "cualquiera" };

        var resultado = validador.TestValidate(comando);

        Assert.Single(resultado.Errors, e => e.PropertyName == nameof(IniciarSesionComando.Email));
    }

    [Fact]
    public void Exige_la_contrasena()
    {
        var comando = new IniciarSesionComando { Email = "asistente@consultorio.ec", Password = "" };

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.Password);
    }
}
