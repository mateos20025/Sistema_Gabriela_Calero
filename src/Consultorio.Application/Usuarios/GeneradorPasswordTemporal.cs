using System.Security.Cryptography;
using Consultorio.Application.Cuenta;

namespace Consultorio.Application.Usuarios;

/// <summary>
/// Genera contraseñas temporales que cumplen la <see cref="PoliticaPassword"/>.
/// Usa un generador criptográfico (no <see cref="Random"/>) y evita caracteres que se confunden
/// al dictarlos o copiarlos a mano (0/O, 1/l/I).
/// </summary>
public static class GeneradorPasswordTemporal
{
    public const int Longitud = PoliticaPassword.LongitudMinima + 2;

    private const string Mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Minusculas = "abcdefghijkmnpqrstuvwxyz";
    private const string Digitos = "23456789";
    private const string Todos = Mayusculas + Minusculas + Digitos;

    public static string Generar()
    {
        Span<char> password = stackalloc char[Longitud];

        // Garantiza al menos una mayúscula, una minúscula y un número...
        password[0] = ElegirUno(Mayusculas);
        password[1] = ElegirUno(Minusculas);
        password[2] = ElegirUno(Digitos);

        // ...rellena el resto al azar y mezcla para que esas tres no queden siempre al inicio.
        RandomNumberGenerator.GetItems(Todos.AsSpan(), password[3..]);
        RandomNumberGenerator.Shuffle(password);

        return new string(password);
    }

    private static char ElegirUno(string opciones) =>
        opciones[RandomNumberGenerator.GetInt32(opciones.Length)];
}
