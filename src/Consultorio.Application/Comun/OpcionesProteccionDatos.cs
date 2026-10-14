namespace Consultorio.Application.Comun;

/// <summary>
/// Configuración de protección de datos (sección "ProteccionDatos").
/// Cuando cambie el texto de la política de privacidad se sube la versión en la configuración:
/// cada consentimiento nuevo queda asociado a la versión que la persona aceptó.
/// </summary>
public sealed class OpcionesProteccionDatos
{
    public const string Seccion = "ProteccionDatos";

    public string VersionPoliticaPrivacidad { get; init; } = "";
}
