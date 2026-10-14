namespace Consultorio.Domain.Pacientes;

/// <summary>
/// Teléfonos de Ecuador. Se guardan siempre en formato internacional E.164 (+593...), que es el que
/// exige WhatsApp, y se muestran en formato nacional.
/// <list type="bullet">
/// <item>Celular: 09 + 8 dígitos (0991234567 pasa a +593991234567).</item>
/// <item>Fijo: 0 + código de provincia (2 a 7) + 7 dígitos (022345678 pasa a +59322345678).</item>
/// </list>
/// </summary>
public static class TelefonoEcuatoriano
{
    public const string CodigoPais = "+593";

    /// <summary>Longitud máxima del valor guardado (+593 y 9 dígitos).</summary>
    public const int LongitudMaxima = 13;

    private const string CodigoPaisSinSigno = "593";
    private const int LongitudCelular = 9;
    private const int LongitudFijo = 8;

    /// <summary>
    /// Acepta "0991234567", "099 123 4567", "(02) 234-5678", "+593 99 123 4567" o "593991234567".
    /// Devuelve el número en formato E.164, o <c>null</c> si no es un teléfono ecuatoriano válido.
    /// </summary>
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();
        var conSignoMas = texto.StartsWith('+');
        var cuerpo = conSignoMas ? texto[1..] : texto;

        if (!cuerpo.All(c => char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' or '.'))
        {
            return null;
        }

        var digitos = string.Concat(cuerpo.Where(char.IsAsciiDigit));

        string nacional;
        if (conSignoMas || digitos.StartsWith(CodigoPaisSinSigno, StringComparison.Ordinal))
        {
            if (!digitos.StartsWith(CodigoPaisSinSigno, StringComparison.Ordinal))
            {
                return null; // Número de otro país.
            }

            nacional = digitos[CodigoPaisSinSigno.Length..];
        }
        else if (digitos.StartsWith('0'))
        {
            nacional = digitos[1..];
        }
        else
        {
            return null;
        }

        // Es común escribir "+593 099..." (con el 0 nacional); se tolera.
        if (nacional.StartsWith('0'))
        {
            nacional = nacional[1..];
        }

        var esCelular = nacional.Length == LongitudCelular && nacional[0] == '9';
        var esFijo = nacional.Length == LongitudFijo && nacional[0] is >= '2' and <= '7';

        return esCelular || esFijo ? CodigoPais + nacional : null;
    }

    public static bool EsValido(string? valor) => Normalizar(valor) is not null;

    /// <summary>"+593991234567" se muestra "099 123 4567"; "+59322345678" se muestra "(02) 234 5678".</summary>
    public static string Formatear(string normalizado)
    {
        if (!normalizado.StartsWith(CodigoPais, StringComparison.Ordinal))
        {
            return normalizado;
        }

        var nacional = normalizado[CodigoPais.Length..];
        return nacional.Length switch
        {
            LongitudCelular => $"0{nacional[..2]} {nacional[2..5]} {nacional[5..]}",
            LongitudFijo => $"(0{nacional[0]}) {nacional[1..4]} {nacional[4..]}",
            _ => normalizado,
        };
    }
}
