namespace Consultorio.Application.Comun;

/// <summary>Una página de resultados y el total de registros que cumplen el filtro.</summary>
public sealed record PaginaResultado<T>(IReadOnlyList<T> Elementos, int Total);
