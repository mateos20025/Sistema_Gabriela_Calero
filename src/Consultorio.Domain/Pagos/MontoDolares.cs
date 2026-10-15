using System.Globalization;

namespace Consultorio.Domain.Pagos;

/// <summary>
/// Montos en dólares (moneda de Ecuador), con 2 decimales como máximo.
/// <para>
/// En Ecuador se escribe tanto "35,50" como "35.50". Con la configuración regional de Ecuador, .NET leería
/// "35.50" como 3550, así que aquí se interpreta de forma segura: el último separador es el decimal si
/// le siguen 1 o 2 dígitos. Lo ambiguo (por ejemplo "1.234") se rechaza en vez de adivinar.
/// </para>
/// </summary>
public static class MontoDolares
{
    public const decimal Minimo = 0.01m;

    /// <summary>Tope de seguridad contra errores de digitación (un cero de más).</summary>
    public const decimal Maximo = 10_000m;

    private const int LongitudMaximaTexto = 15;

    public static bool EsValido(decimal monto) =>
        monto >= Minimo && monto <= Maximo && decimal.Round(monto, 2) == monto;

    /// <summary>Acepta "35", "35,5", "35.50", "$ 35,50", "1.234,50" o "1,234.50".</summary>
    public static bool TryParse(string? texto, out decimal monto)
    {
        monto = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Replace("$", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).Trim();
        if (limpio.Length is 0 or > LongitudMaximaTexto || !limpio.All(c => char.IsAsciiDigit(c) || c is '.' or ','))
        {
            return false;
        }

        var entero = limpio;
        var decimales = "";
        var posicionDecimal = limpio.LastIndexOfAny(['.', ',']);

        if (posicionDecimal >= 0)
        {
            var separadorDecimal = limpio[posicionDecimal];
            decimales = limpio[(posicionDecimal + 1)..];
            entero = limpio[..posicionDecimal];

            if (decimales.Length is < 1 or > 2 || entero.Contains(separadorDecimal))
            {
                return false;
            }

            // Si la parte entera tiene separador de miles, debe ser el otro símbolo y en grupos de 3.
            var separadorMiles = separadorDecimal == ',' ? '.' : ',';
            if (entero.Contains(separadorMiles))
            {
                var grupos = entero.Split(separadorMiles);
                if (grupos[0].Length is < 1 or > 3 || grupos.Skip(1).Any(g => g.Length != 3))
                {
                    return false;
                }

                entero = string.Concat(grupos);
            }
        }

        if (entero.Length == 0 || !entero.All(char.IsAsciiDigit))
        {
            return false;
        }

        var normalizado = decimales.Length == 0 ? entero : $"{entero}.{decimales}";
        monto = decimal.Parse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        return EsValido(monto);
    }
}
