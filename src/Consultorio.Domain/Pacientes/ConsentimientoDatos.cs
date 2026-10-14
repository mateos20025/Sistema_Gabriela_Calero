using Consultorio.Domain.Comun;

namespace Consultorio.Domain.Pacientes;

/// <summary>
/// Autorización del paciente para tratar sus datos personales (LOPDP). Se guarda cuándo la dio,
/// sobre qué versión de la política de privacidad y por qué canal, para poder demostrarlo.
/// </summary>
public sealed record ConsentimientoDatos
{
    public const int LongitudMaximaVersion = 20;

    public ConsentimientoDatos(DateTime aceptadoEnUtc, string versionPolitica, OrigenRegistro origen)
    {
        if (aceptadoEnUtc.Kind != DateTimeKind.Utc)
        {
            throw new ExcepcionDominio("La fecha del consentimiento debe estar en UTC.");
        }

        if (string.IsNullOrWhiteSpace(versionPolitica) || versionPolitica.Trim().Length > LongitudMaximaVersion)
        {
            throw new ExcepcionDominio("La versión de la política de privacidad no es válida.");
        }

        if (!Enum.IsDefined(origen))
        {
            throw new ExcepcionDominio("El origen del consentimiento no es válido.");
        }

        AceptadoEnUtc = aceptadoEnUtc;
        VersionPolitica = versionPolitica.Trim();
        Origen = origen;
    }

    public DateTime AceptadoEnUtc { get; }

    public string VersionPolitica { get; }

    public OrigenRegistro Origen { get; }
}
