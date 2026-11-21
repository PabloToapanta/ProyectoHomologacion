using Catalogo.Application.ImportacionProgramas;
using UglyToad.PdfPig;

namespace Catalogo.Infrastructure.ImportacionProgramas;

/// <summary>HU-08: extracción determinista de palabras con su posición mediante PdfPig (Apache-2.0). Sin OCR.</summary>
public sealed class ExtractorTextoPdfPig : IExtractorTextoPdf
{
    // Límites técnicos frente a documentos manipulados: el plan analítico FISEI tiene menos de 10 páginas.
    public const int MaxPaginas = 30;
    public const int MaxPalabras = 50_000;

    public ExtraccionPdf Extraer(byte[] contenido, CancellationToken ct)
    {
        try
        {
            using var documento = PdfDocument.Open(contenido);
            if (documento.NumberOfPages is 0 or > MaxPaginas) return new(EstadoExtraccion.Invalido, []);
            var palabras = new List<PalabraPdf>();
            for (var numero = 1; numero <= documento.NumberOfPages; numero++)
            {
                ct.ThrowIfCancellationRequested();
                foreach (var palabra in documento.GetPage(numero).GetWords())
                {
                    if (string.IsNullOrWhiteSpace(palabra.Text)) continue;
                    palabras.Add(new(numero, palabra.BoundingBox.Left, palabra.BoundingBox.Bottom, palabra.Text));
                    if (palabras.Count > MaxPalabras) return new(EstadoExtraccion.Invalido, []);
                }
            }
            return palabras.Any(p => p.Texto.Any(char.IsLetter)) ? new(EstadoExtraccion.Texto, palabras) : new(EstadoExtraccion.SinTexto, []);
        }
        catch (OperationCanceledException) { throw; }
        // Entrada no confiable: cualquier fallo del analizador de PDF se informa como archivo inválido, sin detalles internos.
        catch (Exception) { return new(EstadoExtraccion.Invalido, []); }
    }
}
