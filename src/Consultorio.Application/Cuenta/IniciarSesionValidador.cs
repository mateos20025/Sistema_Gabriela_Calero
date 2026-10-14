using FluentValidation;

namespace Consultorio.Application.Cuenta;

public sealed class IniciarSesionValidador : AbstractValidator<IniciarSesionComando>
{
    public IniciarSesionValidador()
    {
        // En cada campo se muestra solo el primer error (no "vacío" y "no válido" a la vez).
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Escribe tu correo electrónico.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.")
            .MaximumLength(256).WithMessage("El correo electrónico es demasiado largo.");

        RuleFor(c => c.Password)
            .NotEmpty().WithMessage("Escribe tu contraseña.")
            .MaximumLength(PoliticaPassword.LongitudMaxima).WithMessage("La contraseña es demasiado larga.");
    }
}
