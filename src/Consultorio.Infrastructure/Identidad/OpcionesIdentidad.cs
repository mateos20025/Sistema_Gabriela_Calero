using Consultorio.Application.Cuenta;
using Microsoft.AspNetCore.Identity;

namespace Consultorio.Infrastructure.Identidad;

/// <summary>Reglas de contraseña y de bloqueo de Identity, alineadas con <see cref="PoliticaPassword"/>.</summary>
internal static class OpcionesIdentidad
{
    public static void Configurar(IdentityOptions opciones)
    {
        opciones.User.RequireUniqueEmail = true;

        opciones.Password.RequiredLength = PoliticaPassword.LongitudMinima;
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = false;
        opciones.Password.RequiredUniqueChars = 4;

        // 5 intentos fallidos seguidos => cuenta bloqueada 15 minutos.
        opciones.Lockout.AllowedForNewUsers = true;
        opciones.Lockout.MaxFailedAccessAttempts = 5;
        opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        // No se envían correos de confirmación: las cuentas las crea la doctora.
        opciones.SignIn.RequireConfirmedAccount = false;
    }
}
