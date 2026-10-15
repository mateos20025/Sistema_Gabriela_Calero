using Consultorio.Domain.Comun;
using Consultorio.Domain.Pacientes;

namespace Consultorio.Domain.Pagos;

/// <summary>
/// Configuración de cobros que define la doctora: precio de la consulta y cuenta para transferencias.
/// Hay un solo registro (<see cref="IdUnico"/>). Sus cambios se auditan: un número de cuenta alterado
/// es un riesgo de fraude.
/// </summary>
public sealed class ConfiguracionPagos
{
    public static readonly Guid IdUnico = new("0b5c3a2e-6f1d-4c8e-9a7b-c0f1e2d3a4b5");

    private ConfiguracionPagos()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Precio sugerido al registrar un pago. Opcional.</summary>
    public decimal? PrecioConsulta { get; private set; }

    /// <summary>Concepto sugerido al registrar un pago, por ejemplo "Consulta médica". Opcional.</summary>
    public string? ConceptoPredeterminado { get; private set; }

    public string? Banco { get; private set; }

    public TipoCuentaBancaria? TipoCuenta { get; private set; }

    public string? NumeroCuenta { get; private set; }

    public string? Titular { get; private set; }

    /// <summary>Cédula (10 dígitos) o RUC (13 dígitos) del titular.</summary>
    public string? IdentificacionTitular { get; private set; }

    public uint Version { get; private set; }

    public DateTime CreadoEn { get; private set; }

    public DateTime ActualizadoEn { get; private set; }

    public bool TieneDatosBancarios => Banco is not null;

    public static ConfiguracionPagos Nueva() => new() { Id = IdUnico };

    public void Actualizar(decimal? precioConsulta, string? conceptoPredeterminado, DatosBancarios? datosBancarios)
    {
        if (precioConsulta is not null && !MontoDolares.EsValido(precioConsulta.Value))
        {
            throw new ExcepcionDominio("El precio de la consulta no es válido.");
        }

        if (!string.IsNullOrWhiteSpace(conceptoPredeterminado) && !Pago.ConceptoEsValido(conceptoPredeterminado))
        {
            throw new ExcepcionDominio("El concepto no es válido.");
        }

        PrecioConsulta = precioConsulta;
        ConceptoPredeterminado = string.IsNullOrWhiteSpace(conceptoPredeterminado) ? null : conceptoPredeterminado.Trim();
        Banco = datosBancarios?.Banco;
        TipoCuenta = datosBancarios?.TipoCuenta;
        NumeroCuenta = datosBancarios?.NumeroCuenta;
        Titular = datosBancarios?.Titular;
        IdentificacionTitular = datosBancarios?.IdentificacionTitular;
    }
}

/// <summary>Cuenta bancaria para recibir transferencias. Todos los datos son obligatorios.</summary>
public sealed record DatosBancarios
{
    public const int LongitudMaximaTexto = 100;
    public const int LongitudMinimaNumeroCuenta = 5;
    public const int LongitudMaximaNumeroCuenta = 20;

    public DatosBancarios(string banco, TipoCuentaBancaria tipoCuenta, string numeroCuenta, string titular, string identificacionTitular)
    {
        Banco = TextoValido(banco) ?? throw new ExcepcionDominio("El nombre del banco no es válido.");

        TipoCuenta = Enum.IsDefined(tipoCuenta) ? tipoCuenta : throw new ExcepcionDominio("El tipo de cuenta no es válido.");

        NumeroCuenta = NormalizarNumeroCuenta(numeroCuenta) ?? throw new ExcepcionDominio("El número de cuenta no es válido.");

        Titular = TextoValido(titular) ?? throw new ExcepcionDominio("El titular no es válido.");

        IdentificacionTitular = NormalizarIdentificacion(identificacionTitular)
            ?? throw new ExcepcionDominio("La cédula o RUC del titular no es válido.");
    }

    public string Banco { get; }

    public TipoCuentaBancaria TipoCuenta { get; }

    public string NumeroCuenta { get; }

    public string Titular { get; }

    public string IdentificacionTitular { get; }

    /// <summary>Solo dígitos (se ignoran espacios y guiones).</summary>
    public static string? NormalizarNumeroCuenta(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = string.Concat(valor.Where(c => !char.IsWhiteSpace(c) && c != '-'));
        return limpio.Length is >= LongitudMinimaNumeroCuenta and <= LongitudMaximaNumeroCuenta && limpio.All(char.IsAsciiDigit)
            ? limpio
            : null;
    }

    /// <summary>Cédula ecuatoriana válida (10 dígitos) o RUC de 13 dígitos terminado en 001.</summary>
    public static string? NormalizarIdentificacion(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = string.Concat(valor.Where(c => !char.IsWhiteSpace(c) && c != '-'));
        if (limpio.Length == CedulaEcuatoriana.Longitud)
        {
            return CedulaEcuatoriana.Normalizar(limpio);
        }

        return limpio.Length == 13 && limpio.All(char.IsAsciiDigit) && limpio.EndsWith("001", StringComparison.Ordinal)
            ? limpio
            : null;
    }

    public static string? TextoValido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = string.Join(' ', valor.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length is >= 2 and <= LongitudMaximaTexto
               && limpio.All(c => char.IsLetterOrDigit(c) || c is ' ' or '.' or ',' or '-' or '&' or '\'')
            ? limpio
            : null;
    }
}
