using Consultorio.Application.Usuarios;
using Consultorio.Domain.Usuarios;
using FluentValidation.TestHelper;

namespace Consultorio.Application.Tests.Usuarios;

public class CrearUsuarioValidadorTests
{
    private readonly CrearUsuarioValidador validador = new();

    private static CrearUsuarioComando Comando() =>
        new() { NombreCompleto = "María Pérez", Email = "maria@consultorio.ec", Rol = Roles.Asistente };

    [Fact]
    public void Acepta_datos_validos()
    {
        validador.TestValidate(Comando()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Exige_nombre()
    {
        var comando = Comando();
        comando.NombreCompleto = " ";

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.NombreCompleto);
    }

    [Fact]
    public void Rechaza_nombre_de_mas_de_100_caracteres()
    {
        var comando = Comando();
        comando.NombreCompleto = new string('a', 101);

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.NombreCompleto);
    }

    [Fact]
    public void Rechaza_correo_invalido()
    {
        var comando = Comando();
        comando.Email = "maria.consultorio.ec";

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Administrador")]
    [InlineData("doctora")] // los roles distinguen mayúsculas
    public void Rechaza_roles_que_no_existen(string rol)
    {
        var comando = Comando();
        comando.Rol = rol;

        validador.TestValidate(comando).ShouldHaveValidationErrorFor(c => c.Rol);
    }
}
