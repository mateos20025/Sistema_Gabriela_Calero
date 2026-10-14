using Consultorio.Application.Abstracciones;

namespace Consultorio.Infrastructure.Comun;

internal sealed class RelojSistema : IRelojSistema
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}
