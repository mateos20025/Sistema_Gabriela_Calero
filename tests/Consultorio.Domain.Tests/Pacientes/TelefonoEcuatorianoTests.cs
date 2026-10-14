using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Tests.Pacientes;

public class TelefonoEcuatorianoTests
{
    [Theory]
    [InlineData("0991234567")]
    [InlineData("099 123 4567")]
    [InlineData("099-123-4567")]
    [InlineData("+593 99 123 4567")]
    [InlineData("+593991234567")]
    [InlineData("593991234567")]
    [InlineData("+593 0991234567")]
    public void Normaliza_celulares_al_formato_internacional(string telefono)
    {
        Assert.Equal("+593991234567", TelefonoEcuatoriano.Normalizar(telefono));
    }

    [Theory]
    [InlineData("022345678")]
    [InlineData("(02) 234-5678")]
    [InlineData("+593 2 234 5678")]
    public void Normaliza_telefonos_fijos(string telefono)
    {
        Assert.Equal("+59322345678", TelefonoEcuatoriano.Normalizar(telefono));
    }

    [Theory]
    [InlineData("0891234567", "celular que no empieza con 9")]
    [InlineData("099123456", "celular incompleto")]
    [InlineData("0123456789", "código de área inexistente")]
    [InlineData("+1 305 555 1234", "número de otro país")]
    [InlineData("991234567", "sin el 0 inicial ni código de país")]
    [InlineData("0991234567 ext 2", "con texto")]
    [InlineData("09912+34567", "signo + en medio")]
    [InlineData("", "vacío")]
    [InlineData(null, "nulo")]
    public void Rechaza_telefonos_invalidos(string? telefono, string motivo)
    {
        Assert.False(TelefonoEcuatoriano.EsValido(telefono), motivo);
    }

    [Theory]
    [InlineData("+593991234567", "099 123 4567")]
    [InlineData("+59322345678", "(02) 234 5678")]
    public void Formatea_para_mostrar(string normalizado, string esperado)
    {
        Assert.Equal(esperado, TelefonoEcuatoriano.Formatear(normalizado));
    }
}
