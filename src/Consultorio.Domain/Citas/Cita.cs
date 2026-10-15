using Consultorio.Domain.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Citas;

/// <summary>
/// Cita del consultorio (un solo calendario). Las transiciones de estado siguen <see cref="ReglasEstadoCita"/>
/// y cada cambio queda en el historial con quién lo hizo, cuándo y una observación.
/// Nunca se borra: se cancela.
/// </summary>
public sealed class Cita
{
    public const int LongitudMaximaObservacion = 300;
    public static readonly TimeSpan DuracionMaxima = TimeSpan.FromHours(8);

    private readonly List<HistorialCita> cambiosNuevos = [];

    private Cita()
    {
    }

    public Guid Id { get; private set; }

    public Guid PacienteId { get; private set; }

    /// <summary>Inicio en UTC.</summary>
    public DateTime Inicio { get; private set; }

    /// <summary>Fin en UTC (no incluido).</summary>
    public DateTime Fin { get; private set; }

    public EstadoCita Estado { get; private set; }

    public OrigenRegistro Origen { get; private set; }

    /// <summary>Hasta cuándo una solicitud Pendiente reserva el horario. Después se libera.</summary>
    public DateTime? ReservaExpiraEn { get; private set; }

    /// <summary>
    /// Datos que escribió quien pidió la cita por la web. No modifican la ficha del paciente:
    /// la asistente los verifica al llamar.
    /// </summary>
    public string? ContactoNombre { get; private set; }

    public string? ContactoTelefono { get; private set; }

    public string? ContactoCorreo { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    /// <summary>Cambios de estado hechos desde que se cargó la cita (el repositorio los guarda).</summary>
    public IReadOnlyList<HistorialCita> CambiosNuevos => cambiosNuevos;

    public IntervaloUtc Horario => new(Inicio, Fin);

    public bool EstaActiva => ReglasEstadoCita.EsActivo(Estado);

    public bool ReservaVencida(DateTime ahoraUtc) =>
        Estado == EstadoCita.Pendiente && ReservaExpiraEn is { } expira && expira <= ahoraUtc;

    /// <summary>El personal agenda por teléfono o en persona: la cita nace confirmada.</summary>
    public static Cita AgendarDesdePanel(Guid pacienteId, IntervaloUtc horario, Guid usuarioId, DateTime ahoraUtc, string? observacion)
    {
        ExigirUtc(ahoraUtc);
        if (usuarioId == Guid.Empty)
        {
            throw new ExcepcionDominio("Se debe saber quién agenda la cita.");
        }

        var cita = Nueva(pacienteId, horario, ahoraUtc, EstadoCita.Confirmada, OrigenRegistro.Panel);
        cita.RegistrarCambio(null, EstadoCita.Confirmada, usuarioId, observacion ?? "Agendada desde el panel.", null, null);
        return cita;
    }

    /// <summary>El paciente pide la cita por la web: queda pendiente y reserva el horario por un tiempo.</summary>
    public static Cita SolicitarDesdeWeb(Guid pacienteId, IntervaloUtc horario, DateTime ahoraUtc, int horasReserva, ContactoSolicitud contacto)
    {
        ExigirUtc(ahoraUtc);
        ArgumentNullException.ThrowIfNull(contacto);
        if (horasReserva < 1)
        {
            throw new ExcepcionDominio("La reserva debe durar al menos una hora.");
        }

        var cita = Nueva(pacienteId, horario, ahoraUtc, EstadoCita.Pendiente, OrigenRegistro.Web);

        // La reserva nunca dura más allá de la hora de la cita.
        var expira = ahoraUtc.AddHours(horasReserva);
        cita.ReservaExpiraEn = expira < horario.Inicio ? expira : horario.Inicio;
        cita.ContactoNombre = contacto.Nombre;
        cita.ContactoTelefono = contacto.Telefono;
        cita.ContactoCorreo = contacto.Correo;
        cita.RegistrarCambio(null, EstadoCita.Pendiente, null, "Solicitud desde la página web.", null, null);
        return cita;
    }

    /// <param name="usuarioId"><c>null</c> si lo hace el sistema (por ejemplo, al vencer una reserva).</param>
    public void CambiarEstado(EstadoCita nuevo, Guid? usuarioId, DateTime ahoraUtc, string? observacion)
    {
        ExigirUtc(ahoraUtc);

        if (nuevo == EstadoCita.Reprogramada)
        {
            throw new ExcepcionDominio("Para reprogramar se debe indicar el nuevo horario.");
        }

        if (!ReglasEstadoCita.PuedePasar(Estado, nuevo))
        {
            throw new ExcepcionDominio($"Una cita en estado {Estado} no puede pasar a {nuevo}.");
        }

        if (nuevo == EstadoCita.Cancelada && string.IsNullOrWhiteSpace(observacion))
        {
            throw new ExcepcionDominio("Indica el motivo de la cancelación.");
        }

        if (nuevo is EstadoCita.Atendida or EstadoCita.NoAsistio && ahoraUtc < Inicio)
        {
            throw new ExcepcionDominio("Solo se puede marcar después de la hora de la cita.");
        }

        var anterior = Estado;
        Estado = nuevo;
        if (nuevo != EstadoCita.Pendiente)
        {
            ReservaExpiraEn = null;
        }

        RegistrarCambio(anterior, nuevo, usuarioId, observacion, null, null);
    }

    /// <summary>El repositorio la llama después de guardar el historial, para no guardarlo dos veces.</summary>
    public void MarcarCambiosGuardados() => cambiosNuevos.Clear();

    public void Reprogramar(IntervaloUtc nuevoHorario, Guid usuarioId, DateTime ahoraUtc, string? observacion)
    {
        ExigirUtc(ahoraUtc);

        if (!ReglasEstadoCita.PuedePasar(Estado, EstadoCita.Reprogramada))
        {
            throw new ExcepcionDominio($"Una cita en estado {Estado} no se puede reprogramar.");
        }

        ValidarHorario(nuevoHorario, ahoraUtc);

        var anterior = Estado;
        var inicioAnterior = Inicio;
        Inicio = nuevoHorario.Inicio;
        Fin = nuevoHorario.Fin;
        Estado = EstadoCita.Reprogramada;
        ReservaExpiraEn = null;

        RegistrarCambio(anterior, EstadoCita.Reprogramada, usuarioId, observacion, inicioAnterior, nuevoHorario.Inicio);
    }

    private static Cita Nueva(Guid pacienteId, IntervaloUtc horario, DateTime ahoraUtc, EstadoCita estado, OrigenRegistro origen)
    {
        if (pacienteId == Guid.Empty)
        {
            throw new ExcepcionDominio("La cita debe tener un paciente.");
        }

        ValidarHorario(horario, ahoraUtc);

        return new Cita
        {
            Id = Guid.CreateVersion7(),
            PacienteId = pacienteId,
            Inicio = horario.Inicio,
            Fin = horario.Fin,
            Estado = estado,
            Origen = origen,
        };
    }

    private static void ValidarHorario(IntervaloUtc horario, DateTime ahoraUtc)
    {
        if (horario.Inicio <= ahoraUtc)
        {
            throw new ExcepcionDominio("El horario de la cita debe ser en el futuro.");
        }

        if (horario.Duracion > DuracionMaxima)
        {
            throw new ExcepcionDominio("Una cita no puede durar más de 8 horas.");
        }
    }

    private void RegistrarCambio(EstadoCita? anterior, EstadoCita nuevo, Guid? usuarioId, string? observacion, DateTime? inicioAnterior, DateTime? inicioNuevo)
    {
        if (observacion is not null && observacion.Trim().Length > LongitudMaximaObservacion)
        {
            throw new ExcepcionDominio($"La observación no puede tener más de {LongitudMaximaObservacion} caracteres.");
        }

        cambiosNuevos.Add(HistorialCita.Crear(Id, anterior, nuevo, usuarioId, observacion, inicioAnterior, inicioNuevo));
    }

    private static void ExigirUtc(DateTime fecha)
    {
        if (fecha.Kind != DateTimeKind.Utc)
        {
            throw new ExcepcionDominio("La fecha debe estar en UTC.");
        }
    }
}

/// <summary>Una fila del historial de la cita (tabla historial_cita). Solo se agregan; nunca se modifican.</summary>
public sealed class HistorialCita
{
    private HistorialCita()
    {
    }

    public Guid Id { get; private set; }

    public Guid CitaId { get; private set; }

    /// <summary><c>null</c> al crear la cita.</summary>
    public EstadoCita? EstadoAnterior { get; private set; }

    public EstadoCita EstadoNuevo { get; private set; }

    /// <summary><c>null</c> si lo hizo el sistema o el propio paciente desde la web.</summary>
    public Guid? UsuarioId { get; private set; }

    public string? Observacion { get; private set; }

    /// <summary>Solo en reprogramaciones.</summary>
    public DateTime? InicioAnterior { get; private set; }

    public DateTime? InicioNuevo { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    internal static HistorialCita Crear(
        Guid citaId, EstadoCita? anterior, EstadoCita nuevo, Guid? usuarioId, string? observacion,
        DateTime? inicioAnterior, DateTime? inicioNuevo) => new()
    {
        Id = Guid.CreateVersion7(),
        CitaId = citaId,
        EstadoAnterior = anterior,
        EstadoNuevo = nuevo,
        UsuarioId = usuarioId,
        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim(),
        InicioAnterior = inicioAnterior,
        InicioNuevo = inicioNuevo,
    };
}

/// <summary>Datos de contacto que dejó quien pidió la cita por la web, ya validados y normalizados.</summary>
public sealed record ContactoSolicitud
{
    public ContactoSolicitud(string nombres, string apellidos, string telefono, string? correo)
    {
        var nombreValido = NombrePersona.Normalizar(nombres) ?? throw new ExcepcionDominio("Los nombres no son válidos.");
        var apellidoValido = NombrePersona.Normalizar(apellidos) ?? throw new ExcepcionDominio("Los apellidos no son válidos.");

        Nombre = $"{nombreValido} {apellidoValido}";
        Telefono = TelefonoEcuatoriano.Normalizar(telefono) ?? throw new ExcepcionDominio("El teléfono no es válido.");

        if (!DireccionCorreo.TryNormalizar(correo, out var correoValido))
        {
            throw new ExcepcionDominio("El correo electrónico no es válido.");
        }

        Correo = correoValido;
    }

    public string Nombre { get; }

    public string Telefono { get; }

    public string? Correo { get; }
}
