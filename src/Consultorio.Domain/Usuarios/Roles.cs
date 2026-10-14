namespace Consultorio.Domain.Usuarios;

/// <summary>
/// Roles del personal del consultorio. Los pacientes no tienen cuenta ni rol.
/// </summary>
public static class Roles
{
    /// <summary>Acceso total: métricas, horarios, usuarios, datos bancarios e historia clínica.</summary>
    public const string Doctora = "Doctora";

    /// <summary>Gestiona la cola de citas y los pagos. No ve la historia clínica.</summary>
    public const string Asistente = "Asistente";

    public static IReadOnlyList<string> Todos { get; } = [Doctora, Asistente];

    public static bool EsValido(string? rol) => rol is not null && Todos.Contains(rol);
}
