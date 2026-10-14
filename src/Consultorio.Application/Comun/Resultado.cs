namespace Consultorio.Application.Comun;

/// <summary>
/// Resultado de un caso de uso que puede fallar por una regla de negocio.
/// Los errores esperados (correo repetido, contraseña débil...) se devuelven aquí, no con excepciones.
/// </summary>
public class Resultado
{
    protected Resultado(bool exito, IReadOnlyList<string> errores)
    {
        Exito = exito;
        Errores = errores;
    }

    public bool Exito { get; }

    public IReadOnlyList<string> Errores { get; }

    public static Resultado Ok() => new(true, []);

    public static Resultado Falla(params IEnumerable<string> errores) => new(false, [.. errores]);
}

/// <summary>Resultado que, si tuvo éxito, trae además un valor.</summary>
public sealed class Resultado<T> : Resultado
{
    private Resultado(bool exito, T? valor, IReadOnlyList<string> errores)
        : base(exito, errores)
    {
        Valor = valor;
    }

    /// <summary>Solo tiene valor si <see cref="Resultado.Exito"/> es <c>true</c>.</summary>
    public T? Valor { get; }

    public static Resultado<T> Ok(T valor) => new(true, valor, []);

    public static new Resultado<T> Falla(params IEnumerable<string> errores) => new(false, default, [.. errores]);
}
