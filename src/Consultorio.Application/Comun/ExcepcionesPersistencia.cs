namespace Consultorio.Application.Comun;

/// <summary>
/// Otra persona guardó cambios sobre el mismo registro después de que se leyó.
/// La lanza el repositorio; el caso de uso la convierte en un mensaje para el usuario.
/// </summary>
public sealed class ConflictoConcurrenciaException(Exception? interna = null)
    : Exception("El registro fue modificado por otra persona.", interna);

/// <summary>
/// La base de datos rechazó el cambio porque repite un valor único (por ejemplo, una cédula).
/// Ocurre si dos personas registran lo mismo casi al mismo tiempo.
/// </summary>
public sealed class RegistroDuplicadoException(Exception? interna = null)
    : Exception("Ya existe un registro con ese valor.", interna);
