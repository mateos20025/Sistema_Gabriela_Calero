namespace Consultorio.Domain.Pacientes;

/// <summary>Correo electrónico opcional del paciente.</summary>
public static class DireccionCorreo
{
    public const int LongitudMaxima = 256;

    /// <summary>
    /// Un valor vacío es válido (el correo es opcional) y se normaliza a <c>null</c>.
    /// Un correo con contenido se guarda sin espacios y en minúsculas.
    /// </summary>
    public static bool TryNormalizar(string? valor, out string? normalizado)
    {
        normalizado = null;
        if (string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }

        var correo = valor.Trim().ToLowerInvariant();
        var arroba = correo.IndexOf('@');

        var valido = correo.Length <= LongitudMaxima
            && arroba > 0
            && arroba == correo.LastIndexOf('@')
            && correo.IndexOf('.', arroba) > arroba + 1
            && !correo.EndsWith('.')
            && !correo.Any(char.IsWhiteSpace);

        if (valido)
        {
            normalizado = correo;
        }

        return valido;
    }

    public static bool EsValidoOVacio(string? valor) => TryNormalizar(valor, out _);
}
