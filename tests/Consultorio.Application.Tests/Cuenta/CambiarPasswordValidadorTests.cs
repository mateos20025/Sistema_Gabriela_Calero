using Consultorio.Application.Cuenta;
using FluentValidation.TestHelper;

namespace Consultorio.Application.Tests.Cuenta;

public class CambiarPasswordValidadorTests
{
    private readonly CambiarPasswordValidador validador = new();

    private static CambiarPasswordComando Comando(string actual = "Actual12345", string nueva = "NuevaClave2026") =>
        new() { PasswordActual = actual, NuevaPassword = nueva, ConfirmarPassword = nueva };

    [Fact]
    public void Acepta_una_contrasena_que_cumple_la_politica()
    {
        validador.TestValidate(Comando()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Corta1A")]          // menos de 10 caracteres
    [InlineData("sinmayuscula1")]    // sin mayúscula
    [InlineData("SINMINUSCULA1")]    // sin minúscula
    [InlineData("SinNumerosAqui")]   // sin número
    public void Rechaza_contrasenas_debiles(string nueva)
    {
        validador.TestValidate(Comando(nueva: nueva))
            .ShouldHaveValidationErrorFor(c => c.NuevaPassword);
    }

    [Fact]
    public void Rechaza_que_la_nueva_sea_igual_a_la_actual()
    {
        validador.TestValidate(Comando(actual: "MismaClave2026", nueva: "MismaClave2026"))
            .ShouldHaveValidationErrorFor(c => c.NuevaPassword)
            .WithErrorMessage("La nueva contraseña debe ser distinta de la actual.");
    }

    [Fact]
    public void Rechaza_si_la_confirmacion_no_coincide()
    {
        var comando = Comando();
        comando.ConfirmarPassword = "OtraClave2026";

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.ConfirmarPassword);
    }

    [Fact]
    public void Exige_la_contrasena_actual()
    {
        validador.TestValidate(Comando(actual: "")).ShouldHaveValidationErrorFor(c => c.PasswordActual);
    }
}
