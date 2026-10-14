namespace Consultorio.Domain.Comun;

/// <summary>
/// Se lanza cuando se intenta romper una regla del negocio (por ejemplo, registrar un paciente con una
/// cédula inválida). Los casos de uso validan antes, así que llegar aquí indica un error de programación.
/// </summary>
public sealed class ExcepcionDominio(string mensaje) : Exception(mensaje);
