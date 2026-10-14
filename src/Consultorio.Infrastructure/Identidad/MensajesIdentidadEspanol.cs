using Microsoft.AspNetCore.Identity;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>Traduce al español los mensajes de error que genera Identity.</summary>
public sealed class MensajesIdentidadEspanol : IdentityErrorDescriber
{
    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Ocurrió un error inesperado. Inténtalo de nuevo.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "Otra persona modificó este registro al mismo tiempo. Recarga la página e inténtalo de nuevo.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "La contraseña actual no es correcta.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "El enlace o código ya no es válido.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "Ya existe un usuario asociado a este inicio de sesión.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), "El nombre de usuario no es válido.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), "El correo electrónico no es válido.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"Ya existe un usuario con el correo {userName}.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"Ya existe un usuario con el correo {email}.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), "El rol no es válido.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), $"El rol {role} ya existe.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "El usuario ya tiene una contraseña.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "El bloqueo no está habilitado para este usuario.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"El usuario ya tiene el rol {role}.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"El usuario no tiene el rol {role}.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"La contraseña debe tener al menos {uniqueChars} caracteres distintos.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir al menos un símbolo.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una letra minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una letra mayúscula.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "El código de recuperación no es válido.");

    private static IdentityError Error(string codigo, string descripcion) =>
        new() { Code = codigo, Description = descripcion };
}
