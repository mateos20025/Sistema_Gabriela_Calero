using System.Text;
using Consultorio.Domain.Pagos;

namespace Consultorio.Domain.Tests.Pagos;

public class MontoYArchivosTests
{
    [Theory]
    [InlineData("35", "35")]
    [InlineData("35,5", "35.5")]
    [InlineData("35,50", "35.50")]
    [InlineData("35.50", "35.50")]
    [InlineData("$ 35,50", "35.50")]
    [InlineData("1.234,50", "1234.50")]
    [InlineData("1,234.50", "1234.50")]
    [InlineData("1234.5", "1234.5")]
    [InlineData("0,01", "0.01")]
    [InlineData("10000", "10000")]
    public void Lee_montos_validos_sin_confundir_separadores(string texto, string esperado)
    {
        Assert.True(MontoDolares.TryParse(texto, out var monto));
        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), monto);
    }

    [Theory]
    [InlineData("1.234", "ambiguo: ¿mil doscientos o uno con 234?")]
    [InlineData("35,505", "tres decimales")]
    [InlineData("1.234.50", "mismo separador dos veces")]
    [InlineData("12.34,5.0", "separadores mezclados")]
    [InlineData("0", "menor que el mínimo")]
    [InlineData("10000,01", "mayor que el máximo")]
    [InlineData("-5", "negativo")]
    [InlineData(",50", "sin parte entera")]
    [InlineData("abc", "letras")]
    [InlineData("", "vacío")]
    [InlineData(null, "nulo")]
    public void Rechaza_montos_invalidos_o_ambiguos(string? texto, string motivo)
    {
        Assert.False(MontoDolares.TryParse(texto, out _), motivo);
    }

    [Fact]
    public void Detecta_el_tipo_por_el_contenido()
    {
        Assert.Equal(TipoArchivoComprobante.Jpg, ArchivosComprobante.DetectarTipo([0xFF, 0xD8, 0xFF, 0xE0, 0x00]));
        Assert.Equal(TipoArchivoComprobante.Png, ArchivosComprobante.DetectarTipo([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00]));
        Assert.Equal(TipoArchivoComprobante.Pdf, ArchivosComprobante.DetectarTipo(Encoding.ASCII.GetBytes("%PDF-1.7\n")));
    }

    [Fact]
    public void Rechaza_archivos_que_no_son_jpg_png_ni_pdf()
    {
        Assert.Null(ArchivosComprobante.DetectarTipo(Encoding.ASCII.GetBytes("MZ ejecutable")));
        Assert.Null(ArchivosComprobante.DetectarTipo(Encoding.ASCII.GetBytes("GIF89a")));
        Assert.Null(ArchivosComprobante.DetectarTipo(Encoding.ASCII.GetBytes("<html><script>")));
        Assert.Null(ArchivosComprobante.DetectarTipo([]));
    }
}
