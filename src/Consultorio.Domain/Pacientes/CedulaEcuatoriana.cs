namespace Consultorio.Domain.Pacientes;

/// <summary>
/// Validación de la cédula de identidad ecuatoriana (personas naturales):
/// <list type="bullet">
/// <item>10 dígitos.</item>
/// <item>Los dos primeros son la provincia: 01 a 24, o 30 (ecuatorianos registrados en el exterior).</item>
/// <item>El tercero es menor que 6 (los valores 6 y 9 corresponden a entidades, no a personas).</item>
/// <item>El décimo es el dígito verificador del algoritmo "módulo 10" del Registro Civil.</item>
/// </list>
/// </summary>
public static class CedulaEcuatoriana
{
    public const int Longitud = 10;

    private const int ProvinciaMinima = 1;
    private const int ProvinciaMaxima = 24;
    private const int ProvinciaExterior = 30;
    private const int TercerDigitoMaximo = 5;

    private static readonly int[] Coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];

    /// <summary>
    /// Quita espacios y guiones ("171003406-5" pasa a "1710034065") y valida.
    /// Devuelve la cédula limpia, o <c>null</c> si no es válida.
    /// </summary>
    public static string? Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var cedula = string.Concat(valor.Where(c => !char.IsWhiteSpace(c) && c != '-'));
        return EsValidaSinFormato(cedula) ? cedula : null;
    }

    public static bool EsValida(string? valor) => Normalizar(valor) is not null;

    private static bool EsValidaSinFormato(string cedula)
    {
        if (cedula.Length != Longitud || !cedula.All(char.IsAsciiDigit))
        {
            return false;
        }

        var provincia = Digito(cedula, 0) * 10 + Digito(cedula, 1);
        var provinciaValida = provincia is >= ProvinciaMinima and <= ProvinciaMaxima || provincia == ProvinciaExterior;
        if (!provinciaValida || Digito(cedula, 2) > TercerDigitoMaximo)
        {
            return false;
        }

        // Módulo 10: se multiplica cada uno de los 9 primeros dígitos por 2 o por 1 (alternando);
        // si el producto pasa de 9 se le resta 9; el verificador es lo que falta para la siguiente decena.
        var suma = 0;
        for (var i = 0; i < Coeficientes.Length; i++)
        {
            var producto = Digito(cedula, i) * Coeficientes[i];
            suma += producto > 9 ? producto - 9 : producto;
        }

        var verificador = (10 - suma % 10) % 10;
        return verificador == Digito(cedula, 9);
    }

    private static int Digito(string texto, int posicion) => texto[posicion] - '0';
}
