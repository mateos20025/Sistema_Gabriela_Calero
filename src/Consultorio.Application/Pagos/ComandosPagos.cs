using System.Globalization;
using Consultorio.Domain.Pagos;

namespace Consultorio.Application.Pagos;

/// <summary>Datos del formulario "Registrar pago". El monto llega como texto y se interpreta con <see cref="MontoDolares"/>.</summary>
public sealed class RegistrarPagoComando
{
    public Guid PacienteId { get; set; }

    /// <summary>Cita a la que corresponde el pago (opcional). Debe ser del mismo paciente.</summary>
    public Guid? CitaId { get; set; }

    public MetodoPago Metodo { get; set; } = MetodoPago.Transferencia;

    public string Monto { get; set; } = "";

    public string Concepto { get; set; } = "";

    /// <summary>Referencia bancaria de la transferencia (opcional).</summary>
    public string NumeroComprobante { get; set; } = "";
}

/// <summary>Archivo que subió el usuario, ya leído en memoria (máximo 5 MB).</summary>
public sealed record ArchivoSubido(string NombreOriginal, byte[] Contenido);

/// <summary>Formulario de configuración de pagos (solo la doctora).</summary>
public sealed class ConfiguracionPagosComando
{
    private static readonly CultureInfo CulturaEcuador = CultureInfo.GetCultureInfo("es-EC");

    public string PrecioConsulta { get; set; } = "";

    public string ConceptoPredeterminado { get; set; } = "";

    /// <summary>Si está activo, los datos bancarios son obligatorios.</summary>
    public bool RecibeTransferencias { get; set; }

    public string Banco { get; set; } = "";

    public TipoCuentaBancaria TipoCuenta { get; set; } = TipoCuentaBancaria.Ahorros;

    public string NumeroCuenta { get; set; } = "";

    public string Titular { get; set; } = "";

    public string IdentificacionTitular { get; set; } = "";

    /// <summary>Versión que se mostró en pantalla, para detectar cambios simultáneos.</summary>
    public uint Version { get; set; }

    public static ConfiguracionPagosComando Desde(ConfiguracionPagosVista vista) => new()
    {
        PrecioConsulta = vista.PrecioConsulta?.ToString("0.00", CulturaEcuador) ?? "",
        ConceptoPredeterminado = vista.ConceptoPredeterminado ?? "",
        RecibeTransferencias = vista.TieneDatosBancarios,
        Banco = vista.Banco ?? "",
        TipoCuenta = vista.TipoCuenta ?? TipoCuentaBancaria.Ahorros,
        NumeroCuenta = vista.NumeroCuenta ?? "",
        Titular = vista.Titular ?? "",
        IdentificacionTitular = vista.IdentificacionTitular ?? "",
        Version = vista.Version,
    };
}
