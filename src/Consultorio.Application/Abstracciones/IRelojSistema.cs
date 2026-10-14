using Consultorio.Application.Comun;

namespace Consultorio.Application.Abstracciones;

/// <summary>
/// Fuente única de la fecha y hora actual. En las pruebas se reemplaza por un reloj con fecha fija.
/// </summary>
public interface IRelojSistema
{
    /// <summary>Fecha y hora actual en UTC (así se guarda en la base de datos).</summary>
    DateTime AhoraUtc { get; }

    /// <summary>Fecha y hora actual en Ecuador continental (UTC-5), para mostrar o calcular agendas.</summary>
    DateTime AhoraEcuador => HoraEcuador.DesdeUtc(AhoraUtc);
}
