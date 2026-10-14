namespace Consultorio.Application.Pacientes;

/// <summary>Datos comunes de los formularios de paciente (crear y editar).</summary>
public abstract class DatosPacienteComando
{
    public string Cedula { get; set; } = "";

    public string Nombres { get; set; } = "";

    public string Apellidos { get; set; } = "";

    public string Telefono { get; set; } = "";

    /// <summary>Opcional.</summary>
    public string CorreoElectronico { get; set; } = "";
}

/// <summary>Registro de un paciente desde el panel (en persona o por teléfono).</summary>
public sealed class RegistrarPacienteComando : DatosPacienteComando
{
    /// <summary>El personal confirma que el paciente autorizó el tratamiento de sus datos.</summary>
    public bool ConsentimientoAceptado { get; set; }
}

public sealed class ActualizarPacienteComando : DatosPacienteComando
{
    public Guid Id { get; set; }

    /// <summary>Versión que se mostró en pantalla; detecta si alguien más lo modificó entretanto.</summary>
    public uint Version { get; set; }

    public static ActualizarPacienteComando DesdeDetalle(PacienteDetalle paciente) => new()
    {
        Id = paciente.Id,
        Version = paciente.Version,
        Cedula = paciente.Cedula,
        Nombres = paciente.Nombres,
        Apellidos = paciente.Apellidos,
        Telefono = paciente.Telefono,
        CorreoElectronico = paciente.CorreoElectronico ?? "",
    };
}
