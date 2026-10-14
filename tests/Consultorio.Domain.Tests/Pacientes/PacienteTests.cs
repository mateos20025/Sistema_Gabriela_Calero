using Consultorio.Domain.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Tests.Pacientes;

public class PacienteTests
{
    private static readonly DateTime Ahora = new(2026, 10, 13, 15, 0, 0, DateTimeKind.Utc);

    private static ConsentimientoDatos Consentimiento() => new(Ahora, "1.0", OrigenRegistro.Panel);

    private static Paciente Nuevo() =>
        Paciente.Registrar("171003406-5", "  María  José ", "Pérez", "099 123 4567", " Maria@Correo.com ", Consentimiento());

    [Fact]
    public void Registrar_normaliza_los_datos_y_guarda_el_consentimiento()
    {
        var paciente = Nuevo();

        Assert.NotEqual(Guid.Empty, paciente.Id);
        Assert.Equal("1710034065", paciente.Cedula);
        Assert.Equal("María José", paciente.Nombres);
        Assert.Equal("Pérez", paciente.Apellidos);
        Assert.Equal("María José Pérez", paciente.NombreCompleto);
        Assert.Equal("+593991234567", paciente.Telefono);
        Assert.Equal("maria@correo.com", paciente.CorreoElectronico);
        Assert.Equal(Ahora, paciente.ConsentimientoAceptadoEn);
        Assert.Equal("1.0", paciente.ConsentimientoVersion);
        Assert.Equal(OrigenRegistro.Panel, paciente.ConsentimientoOrigen);
        Assert.False(paciente.EstaDeshabilitado);
    }

    [Fact]
    public void Registrar_sin_correo_lo_deja_nulo()
    {
        var paciente = Paciente.Registrar("1710034065", "Ana", "Ruiz", "0991234567", "  ", Consentimiento());

        Assert.Null(paciente.CorreoElectronico);
    }

    [Theory]
    [InlineData("1710034064", "Ana", "Ruiz", "0991234567", null)]
    [InlineData("1710034065", "", "Ruiz", "0991234567", null)]
    [InlineData("1710034065", "Ana", "Ruiz2", "0991234567", null)]
    [InlineData("1710034065", "Ana", "Ruiz", "12345", null)]
    [InlineData("1710034065", "Ana", "Ruiz", "0991234567", "no-es-correo")]
    public void Registrar_con_datos_invalidos_lanza_excepcion_de_dominio(
        string cedula, string nombres, string apellidos, string telefono, string? correo)
    {
        Assert.Throws<ExcepcionDominio>(() =>
            Paciente.Registrar(cedula, nombres, apellidos, telefono, correo, Consentimiento()));
    }

    [Fact]
    public void ActualizarDatos_cambia_los_datos()
    {
        var paciente = Nuevo();

        paciente.ActualizarDatos("0102030400", "Ana", "Ruiz", "022345678", null);

        Assert.Equal("0102030400", paciente.Cedula);
        Assert.Equal("Ana Ruiz", paciente.NombreCompleto);
        Assert.Equal("+59322345678", paciente.Telefono);
        Assert.Null(paciente.CorreoElectronico);
    }

    [Fact]
    public void Deshabilitar_y_habilitar_cambian_el_estado()
    {
        var paciente = Nuevo();

        paciente.Deshabilitar(Ahora);
        Assert.True(paciente.EstaDeshabilitado);
        Assert.Equal(Ahora, paciente.DeshabilitadoEn);

        paciente.Habilitar();
        Assert.False(paciente.EstaDeshabilitado);
        Assert.Null(paciente.DeshabilitadoEn);
    }

    [Fact]
    public void No_se_puede_deshabilitar_dos_veces()
    {
        var paciente = Nuevo();
        paciente.Deshabilitar(Ahora);

        Assert.Throws<ExcepcionDominio>(() => paciente.Deshabilitar(Ahora));
    }

    [Fact]
    public void No_se_puede_habilitar_un_paciente_habilitado()
    {
        Assert.Throws<ExcepcionDominio>(() => Nuevo().Habilitar());
    }

    [Fact]
    public void No_se_puede_modificar_un_paciente_deshabilitado()
    {
        var paciente = Nuevo();
        paciente.Deshabilitar(Ahora);

        Assert.Throws<ExcepcionDominio>(() =>
            paciente.ActualizarDatos("1710034065", "Ana", "Ruiz", "0991234567", null));
    }

    [Fact]
    public void Deshabilitar_exige_fecha_en_UTC()
    {
        var local = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ExcepcionDominio>(() => Nuevo().Deshabilitar(local));
    }

    [Fact]
    public void El_consentimiento_exige_fecha_UTC_y_version()
    {
        var local = new DateTime(2026, 10, 13, 10, 0, 0, DateTimeKind.Local);

        Assert.Throws<ExcepcionDominio>(() => new ConsentimientoDatos(local, "1.0", OrigenRegistro.Web));
        Assert.Throws<ExcepcionDominio>(() => new ConsentimientoDatos(Ahora, " ", OrigenRegistro.Web));
        Assert.Throws<ExcepcionDominio>(() => new ConsentimientoDatos(Ahora, new string('1', 21), OrigenRegistro.Web));
        Assert.Throws<ExcepcionDominio>(() => new ConsentimientoDatos(Ahora, "1.0", (OrigenRegistro)99));
    }
}
