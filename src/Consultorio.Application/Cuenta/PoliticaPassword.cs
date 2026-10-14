using FluentValidation;

namespace Consultorio.Application.Cuenta;

/// <summary>
/// Reglas de contraseña. Las mismas constantes se usan para configurar Identity (Infrastructure)
/// y para validar los formularios, así nunca se contradicen.
/// </summary>
public static class PoliticaPassword
{
    public const int LongitudMinima = 10;
    public const int LongitudMaxima = 128;

    public static IRuleBuilderOptions<T, string> PasswordSegura<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("Escribe la nueva contraseña.")
            .MinimumLength(LongitudMinima).WithMessage($"Debe tener al menos {LongitudMinima} caracteres.")
            .MaximumLength(LongitudMaxima).WithMessage($"No puede tener más de {LongitudMaxima} caracteres.")
            .Must(p => p.Any(char.IsUpper)).WithMessage("Debe incluir al menos una letra mayúscula.")
            .Must(p => p.Any(char.IsLower)).WithMessage("Debe incluir al menos una letra minúscula.")
            .Must(p => p.Any(char.IsDigit)).WithMessage("Debe incluir al menos un número.");
}
