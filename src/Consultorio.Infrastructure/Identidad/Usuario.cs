using Microsoft.AspNetCore.Identity;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>
/// Cuenta del personal (doctora o asistente). Hereda de Identity el correo, el hash de la contraseña,
/// el bloqueo por intentos fallidos, etc. El nombre de usuario siempre es el correo.
/// </summary>
public sealed class Usuario : IdentityUser<Guid>
{
    public string NombreCompleto { get; set; } = "";

    /// <summary>Un usuario desactivado no puede iniciar sesión. Nunca se borran cuentas (auditoría).</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Verdadero mientras use una contraseña temporal creada por la doctora o por el sistema.</summary>
    public bool DebeCambiarPassword { get; set; }

    public DateTime? UltimoAccesoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime ActualizadoEn { get; set; }
}
