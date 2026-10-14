using Consultorio.Domain.Usuarios;
using FluentValidation;

namespace Consultorio.Application.Usuarios;

public sealed class CrearUsuarioValidador : AbstractValidator<CrearUsuarioComando>
{
    public CrearUsuarioValidador()
    {
        // En cada campo se muestra solo el primer error.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(c => c.NombreCompleto)
            .NotEmpty().WithMessage("Escribe el nombre completo.")
            .MaximumLength(100).WithMessage("El nombre no puede tener más de 100 caracteres.");

        RuleFor(c => c.Email)
            .NotEmpty().WithMessage("Escribe el correo electrónico.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.")
            .MaximumLength(256).WithMessage("El correo electrónico es demasiado largo.");

        RuleFor(c => c.Rol)
            .Must(Roles.EsValido).WithMessage("Elige un rol válido.");
    }
}
