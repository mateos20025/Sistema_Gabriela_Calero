namespace Consultorio.Domain.Pacientes;

/// <summary>Por dónde llegó el paciente (y por dónde dio su consentimiento de datos).</summary>
public enum OrigenRegistro
{
    /// <summary>Lo registró el personal desde el panel (en persona o por teléfono).</summary>
    Panel = 1,

    /// <summary>Lo registró el propio paciente desde la web pública.</summary>
    Web = 2,

    /// <summary>Lo registró el propio paciente por WhatsApp.</summary>
    WhatsApp = 3,
}
