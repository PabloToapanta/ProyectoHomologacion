using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Catalogo.Application.ImportacionProgramas;
using Catalogo.Infrastructure.ImportacionProgramas;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace Catalogo.Tests;

/// <summary>PDF sintéticos generados en la prueba con la disposición del plan analítico FISEI.</summary>
internal static class PdfSintetico
{
    /// <summary>
    /// PDF mínimo escrito a mano con Helvetica y WinAnsiEncoding: admite las tildes de los rótulos reales
    /// («Código», «mínimos»), que la fuente estándar del generador de PdfPig no codifica.
    /// </summary>
    public static byte[] Desde(PlanSintetico plan)
    {
        var latin = System.Text.Encoding.Latin1;
        var paginas = plan.Palabras.Max(p => p.Pagina);
        var objetos = new List<byte[]>
        {
            latin.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"),
            latin.GetBytes($"<< /Type /Pages /Kids [{string.Join(' ', Enumerable.Range(0, paginas).Select(i => $"{4 + i * 2} 0 R"))}] /Count {paginas} >>"),
            latin.GetBytes("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
        };
        for (var n = 1; n <= paginas; n++)
        {
            var flujo = new System.Text.StringBuilder();
            foreach (var p in plan.Palabras.Where(p => p.Pagina == n))
            {
                var texto = p.Texto.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
                flujo.Append(System.FormattableString.Invariant($"BT /F1 7 Tf {p.X:0.##} {p.Y:0.##} Td ({texto}) Tj ET\n"));
            }
            var contenido = latin.GetBytes(flujo.ToString());
            objetos.Add(latin.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {5 + (n - 1) * 2} 0 R >>"));
            objetos.Add([.. latin.GetBytes($"<< /Length {contenido.Length} >>\nstream\n"), .. contenido, .. latin.GetBytes("\nendstream")]);
        }
        using var salida = new MemoryStream();
        void Escribir(string s) => salida.Write(latin.GetBytes(s));
        Escribir("%PDF-1.4\n");
        var posiciones = new List<long>();
        for (var i = 0; i < objetos.Count; i++)
        {
            posiciones.Add(salida.Position);
            Escribir($"{i + 1} 0 obj\n"); salida.Write(objetos[i]); Escribir("\nendobj\n");
        }
        var xref = salida.Position;
        Escribir($"xref\n0 {objetos.Count + 1}\n0000000000 65535 f \n");
        foreach (var posicion in posiciones) Escribir($"{posicion:D10} 00000 n \n");
        Escribir($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return salida.ToArray();
    }

    /// <summary>PDF válido sin texto seleccionable, como un documento escaneado.</summary>
    public static byte[] SinTexto()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4).DrawRectangle(new UglyToad.PdfPig.Core.PdfPoint(50, 50), 100, 100);
        return builder.Build();
    }
}

public class ExtractorTextoPdfPigTests
{
    [Fact]
    public void Extrae_palabras_con_posicion_y_el_analizador_reconstruye_el_plan()
    {
        var e = new ExtractorTextoPdfPig().Extraer(PdfSintetico.Desde(PlanSintetico.Completo()), default);
        Assert.Equal(EstadoExtraccion.Texto, e.Estado);
        var b = AnalizadorPlanAnaliticoFisei.Analizar(e.Palabras)!;
        Assert.Equal("SINT-01-02", b.Codigo);
        Assert.Equal(2, b.Unidades.Count);
        Assert.Equal("Resultado sintetico de la unidad uno.", b.Unidades[0].ResultadoAprendizaje);
        Assert.Empty(b.Faltantes);
    }

    [Fact]
    public void Pdf_sin_texto_o_danado_no_genera_palabras()
    {
        var extractor = new ExtractorTextoPdfPig();
        Assert.Equal(EstadoExtraccion.SinTexto, extractor.Extraer(PdfSintetico.SinTexto(), default).Estado);
        Assert.Equal(EstadoExtraccion.Invalido, extractor.Extraer("%PDF-1.7 truncado"u8.ToArray(), default).Estado);
    }
}

public class ImportacionProgramasApiTests
{
    private static MultipartFormDataContent Lote(Guid carreraId, params (string Nombre, byte[] Bytes, string Tipo)[] archivos)
    {
        var form = new MultipartFormDataContent { { new StringContent(carreraId.ToString()), "carreraId" } };
        foreach (var (nombre, bytes, tipo) in archivos)
        {
            var parte = new ByteArrayContent(bytes);
            parte.Headers.ContentType = new MediaTypeHeaderValue(tipo);
            form.Add(parte, "archivos", nombre);
        }
        return form;
    }

    private static async Task<Guid> Carrera(HttpClient admin)
    {
        using var r = await admin.PostAsJsonAsync("/api/carreras", new { nombre = "Software sintetica", facultad = "FISEI", vigencia = true });
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static object Programa(Guid carreraId) => new
    {
        carreraId, codigo = "SINT-01-02", nombre = "Asignatura sintetica", nivel = 1, creditos = 3, horas = 144,
        unidades = new[]
        {
            new { numero = 1, nombre = "Unidad sintetica uno", horas = 72, resultadoAprendizaje = "Resultado sintetico uno.",
                tematicas = new[] { new { nombre = "Tema uno", contenidos = new[] { "Contenido A" } } } },
            new { numero = 2, nombre = "Unidad sintetica dos", horas = 72, resultadoAprendizaje = "Resultado sintetico dos.",
                tematicas = new[] { new { nombre = "Tema dos", contenidos = new[] { "Contenido B", "Contenido C" } } } }
        }
    };

    private static MultipartFormDataContent Confirmacion(object programa, byte[] pdf, string nombre = "plan.pdf")
    {
        var parte = new ByteArrayContent(pdf); parte.Headers.ContentType = new("application/pdf");
        return new MultipartFormDataContent { { new StringContent(JsonSerializer.Serialize(programa)), "programa" }, { parte, "archivo", nombre } };
    }

    [Fact]
    public async Task Analiza_varios_archivos_con_un_resultado_por_archivo_y_sin_guardar()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin(); var carrera = await Carrera(admin);
        using var r = await admin.PostAsync("/api/importaciones-programas/analisis", Lote(carrera,
            ("plan.pdf", PdfSintetico.Desde(PlanSintetico.Completo()), "application/pdf"),
            ("escaneado.pdf", PdfSintetico.SinTexto(), "application/pdf"),
            ("disfrazado.pdf", "no es un pdf"u8.ToArray(), "application/pdf")));
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var lote = await r.Content.ReadFromJsonAsync<JsonElement>();
        var archivos = lote.GetProperty("archivos").EnumerateArray().ToArray();
        Assert.Equal(["Borrador", "SinTexto", "Invalido"], archivos.Select(a => a.GetProperty("estado").GetString()));
        Assert.Equal("SINT-01-02", archivos[0].GetProperty("borrador").GetProperty("codigo").GetString());
        Assert.Empty(await admin.GetFromJsonAsync<JsonElement[]>($"/api/carreras/{carrera}/asignaturas") ?? []);
    }

    [Fact]
    public async Task Pdf_con_nombre_o_tipo_engañoso_se_valida_por_contenido()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin(); var carrera = await Carrera(admin);
        using var r = await admin.PostAsync("/api/importaciones-programas/analisis",
            Lote(carrera, ("plan.txt", PdfSintetico.Desde(PlanSintetico.Completo()), "text/plain")));
        var lote = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Borrador", lote.GetProperty("archivos")[0].GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Confirma_guarda_en_el_catalogo_y_conserva_el_pdf_con_su_huella()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin(); var carrera = await Carrera(admin);
        admin.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Administrador", usuario: "admin.sintetico"));
        var pdf = PdfSintetico.Desde(PlanSintetico.Completo());
        using var r = await admin.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(Programa(carrera), pdf, "C:\\ruta\\cliente\\plan.pdf"));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var id = (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var detalle = await admin.GetFromJsonAsync<JsonElement>($"/api/asignaturas/{id}");
        Assert.Equal(2, detalle.GetProperty("unidades").GetArrayLength());
        Assert.Equal("Tema dos", detalle.GetProperty("unidades")[1].GetProperty("tematicas")[0].GetProperty("nombre").GetString());
        using var descarga = await admin.GetAsync($"/api/asignaturas/{id}/programa-analitico");
        Assert.Equal(HttpStatusCode.OK, descarga.StatusCode);
        Assert.Equal("application/pdf", descarga.Content.Headers.ContentType!.MediaType);
        Assert.Equal(pdf, await descarga.Content.ReadAsByteArrayAsync());
        Assert.Equal("plan.pdf", descarga.Content.Headers.ContentDisposition!.FileNameStar ?? descarga.Content.Headers.ContentDisposition.FileName?.Trim('"'));
        Assert.Equal(Convert.ToHexString(SHA256.HashData(pdf)), descarga.Headers.GetValues("X-Content-SHA256").Single());
        Assert.Equal("admin.sintetico", descarga.Headers.GetValues("X-Cargado-Por").Single());
    }

    [Fact]
    public async Task Codigo_existente_no_se_sobrescribe_y_datos_invalidos_no_guardan_nada()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin(); var carrera = await Carrera(admin);
        admin.DefaultRequestHeaders.Authorization = new("Bearer", f.Token("Administrador", usuario: "admin.sintetico"));
        var pdf = PdfSintetico.Desde(PlanSintetico.Completo());
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(Programa(carrera), pdf))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(Programa(carrera), pdf))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(new { carreraId = carrera, codigo = "OTRO" }, pdf))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(Programa(carrera), "texto"u8.ToArray()))).StatusCode);
        using var mal = new MultipartFormDataContent { { new StringContent("{no json"), "programa" }, { new ByteArrayContent(pdf), "archivo", "plan.pdf" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/importaciones-programas/confirmacion", mal)).StatusCode);
        Assert.Single(await admin.GetFromJsonAsync<JsonElement[]>($"/api/carreras/{carrera}/asignaturas") ?? []);
    }

    [Theory]
    [InlineData("Docente", HttpStatusCode.Forbidden)]
    [InlineData("Coordinador", HttpStatusCode.Forbidden)]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    public async Task Solo_el_Administrador_carga_confirma_o_descarga(string? rol, HttpStatusCode esperado)
    {
        using var f = new CatalogoFactory(); using var c = f.CreateClient();
        if (rol is not null) c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol, usuario: "usuario.sintetico"));
        var pdf = PdfSintetico.Desde(PlanSintetico.Completo());
        Assert.Equal(esperado, (await c.PostAsync("/api/importaciones-programas/analisis", Lote(Guid.NewGuid(), ("plan.pdf", pdf, "application/pdf")))).StatusCode);
        Assert.Equal(esperado, (await c.PostAsync("/api/importaciones-programas/confirmacion", Confirmacion(Programa(Guid.NewGuid()), pdf))).StatusCode);
        Assert.Equal(esperado, (await c.GetAsync($"/api/asignaturas/{Guid.NewGuid()}/programa-analitico")).StatusCode);
    }

    [Fact]
    public async Task Carrera_inexistente_lote_vacio_y_programa_sin_pdf()
    {
        using var f = new CatalogoFactory(); using var admin = f.Admin();
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync("/api/importaciones-programas/analisis", Lote(Guid.NewGuid(), ("plan.pdf", PdfSintetico.Desde(PlanSintetico.Completo()), "application/pdf")))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/api/importaciones-programas/analisis", Lote(await Carrera(admin)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/asignaturas/{Guid.NewGuid()}/programa-analitico")).StatusCode);
    }
}
