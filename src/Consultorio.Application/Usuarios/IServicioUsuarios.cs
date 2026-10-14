using Consultorio.Application.Comun;

namespace Consultorio.Application.Usuarios;

/// <summary>
/// Gestión de las cuentas del personal. Solo la doctora puede usarla (se comprueba aquí también,
/// no solo en la pantalla). No hay registro público: todas las cuentas se crean desde aquí.
/// </summary>
public interface IServicioUsuarios
{
    Task<IReadOnlyList<UsuarioResumen>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>Crea la cuenta y devuelve la contraseña temporal generada (se muestra una sola vez).</summary>
    Task<Resultado<string>> CrearAsync(CrearUsuarioComando comando, CancellationToken cancellationToken);

    Task<Resultado> CambiarRolAsync(Guid usuarioId, string nuevoRol, CancellationToken cancellationToken);

    /// <summary>Genera una nueva contraseña temporal, cierra las sesiones abiertas y la devuelve.</summary>
    Task<Resultado<string>> RestablecerPasswordAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Impide que el usuario inicie sesión y cierra sus sesiones abiertas. No borra datos.</summary>
    Task<Resultado> DesactivarAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Resultado> ActivarAsync(Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>Quita el bloqueo temporal por intentos fallidos.</summary>
    Task<Resultado> DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken);
}
