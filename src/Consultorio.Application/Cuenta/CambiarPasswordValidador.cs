using FluentValidation;

namespace Consultorio.Application.Cuenta;

public sealed class CambiarPasswordValidador : AbstractValidator<CambiarPasswordComando>
{
    public CambiarPasswordValidador()
    {
        // En cada campo se muestra solo el primer error; los requisitos completos ya se ven bajo el campo.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.PasswordActual)
            .NotEmpty().WithMessage("Escribe tu contraseña actual.");

        RuleFor(c => c.NuevaPassword)
            .PasswordSegura()
            .NotEqual(c => c.PasswordActual).WithMessage("La nueva contraseña debe ser distinta de la actual.");

        RuleFor(c => c.ConfirmarPassword)
            .Equal(c => c.NuevaPassword).WithMessage("Las contraseñas no coinciden.");
    }
}
