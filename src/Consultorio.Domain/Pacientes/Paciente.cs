using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Pacientes;

/// <summary>
/// Paciente del consultorio. Se identifica por su cédula (única).
/// <para>
/// Solo se guardan los datos necesarios para gestionar sus citas (principio de minimización de la LOPDP).
/// Los datos se cambian únicamente con los métodos de esta clase, que validan las reglas; por eso
/// las propiedades tienen <c>private set</c>.
/// </para>
/// <para>
/// Nunca se borra: un paciente se "deshabilita" (deja de aparecer en las búsquedas y en la agenda)
/// y se puede volver a habilitar cuando haga falta.
/// </para>
/// </summary>
public sealed class Paciente
{
    // Constructor vacío que usa EF Core al leer de la base de datos.
    private Paciente()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>10 dígitos, sin espacios ni guiones.</summary>
    public string Cedula { get; private set; } = "";

    public string Nombres { get; private set; } = "";

    public string Apellidos { get; private set; } = "";

    /// <summary>Formato E.164 (+593...).</summary>
    public string Telefono { get; private set; } = "";

    /// <summary>Opcional. En minúsculas.</summary>
    public string? CorreoElectronico { get; private set; }

    public DateTime ConsentimientoAceptadoEn { get; private set; }

    public string ConsentimientoVersion { get; private set; } = "";

    public OrigenRegistro ConsentimientoOrigen { get; private set; }

    /// <summary>Fecha (UTC) en que se deshabilitó. <c>null</c> si está habilitado.</summary>
    public DateTime? DeshabilitadoEn { get; private set; }

    /// <summary>
    /// Versión del registro para detectar ediciones simultáneas: si dos personas editan al mismo
    /// paciente, la segunda en guardar recibe un aviso en vez de borrar sin saberlo los cambios de la primera.
    /// </summary>
    public uint Version { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public bool EstaDeshabilitado => DeshabilitadoEn is not null;

    public static Paciente Registrar(
        string cedula,
        string nombres,
        string apellidos,
        string telefono,
        string? correoElectronico,
        ConsentimientoDatos consentimiento)
    {
        ArgumentNullException.ThrowIfNull(consentimiento);

        var paciente = new Paciente
        {
            Id = Guid.CreateVersion7(),
            ConsentimientoAceptadoEn = consentimiento.AceptadoEnUtc,
            ConsentimientoVersion = consentimiento.VersionPolitica,
            ConsentimientoOrigen = consentimiento.Origen,
        };

        paciente.AsignarDatos(cedula, nombres, apellidos, telefono, correoElectronico);
        return paciente;
    }

    public void ActualizarDatos(string cedula, string nombres, string apellidos, string telefono, string? correoElectronico)
    {
        if (EstaDeshabilitado)
        {
            throw new ExcepcionDominio("No se puede modificar un paciente deshabilitado.");
        }

        AsignarDatos(cedula, nombres, apellidos, telefono, correoElectronico);
    }

    public void Deshabilitar(DateTime ahoraUtc)
    {
        if (ahoraUtc.Kind != DateTimeKind.Utc)
        {
            throw new ExcepcionDominio("La fecha debe estar en UTC.");
        }

        if (EstaDeshabilitado)
        {
            throw new ExcepcionDominio("El paciente ya está deshabilitado.");
        }

        DeshabilitadoEn = ahoraUtc;
    }

    public void Habilitar()
    {
        if (!EstaDeshabilitado)
        {
            throw new ExcepcionDominio("El paciente ya está habilitado.");
        }

        DeshabilitadoEn = null;
    }

    private void AsignarDatos(string cedula, string nombres, string apellidos, string telefono, string? correoElectronico)
    {
        Cedula = CedulaEcuatoriana.Normalizar(cedula)
            ?? throw new ExcepcionDominio("La cédula no es válida.");

        Nombres = NombrePersona.Normalizar(nombres)
            ?? throw new ExcepcionDominio("Los nombres no son válidos.");

        Apellidos = NombrePersona.Normalizar(apellidos)
            ?? throw new ExcepcionDominio("Los apellidos no son válidos.");

        Telefono = TelefonoEcuatoriano.Normalizar(telefono)
            ?? throw new ExcepcionDominio("El teléfono no es válido.");

        if (!DireccionCorreo.TryNormalizar(correoElectronico, out var correo))
        {
            throw new ExcepcionDominio("El correo electrónico no es válido.");
        }

        CorreoElectronico = correo;
    }
}
