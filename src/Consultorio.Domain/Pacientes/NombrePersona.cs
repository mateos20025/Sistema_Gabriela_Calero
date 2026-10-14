using System.Text;
using System.Text.RegularExpressions;

namespace Consultorio.Domain.Pacientes;

/// <summary>Reglas para nombres y apellidos de personas.</summary>
public static partial class NombrePersona
{
    public const int LongitudMaxima = 100;

    /// <summary>
    /// Quita espacios sobrantes ("  María   José " pasa a "María José") y valida que solo tenga letras
    /// (con tildes y ñ), espacios, apóstrofos, puntos o guiones. Devuelve <c>null</c> si no es válido.
    /// </summary>
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        // FormC une letra + tilde en un solo carácter: así "José" escrito de dos formas se guarda igual.
        var limpio = EspaciosRepetidos().Replace(valor.Trim(), " ").Normalize(NormalizationForm.FormC);

        return limpio.Length <= LongitudMaxima && CaracteresPermitidos().IsMatch(limpio) ? limpio : null;
    }

    public static bool EsValido(string? valor) => Normalizar(valor) is not null;

    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosRepetidos();

    [GeneratedRegex(@"^\p{L}[\p{L}\p{M}'’ .\-]*$")]
    private static partial Regex CaracteresPermitidos();
}
