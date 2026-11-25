using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Catalogo.Domain;
using Xunit;

namespace Catalogo.Tests;

/// <summary>HU-38: catálogo de normativas. Datos y PDF sintéticos generados en la prueba.</summary>
public class NormativaTests
{
    private static readonly byte[] Pdf = "%PDF-1.4\n% normativa sintetica\n%%EOF\n"u8.ToArray();
    private static readonly DateTimeOffset Ahora = new(2026, 11, 25, 15, 0, 0, TimeSpan.Zero);

    private static Normativa Nueva(string codigo = "RES-SINT-001-2023", Guid? reformaA = null) =>
        new(codigo, "Instructivo sintético", "Consejo sintético", new DateOnly(2023, 3, 1), new DateOnly(2023, 3, 2),
            reformaA, "instructivo.pdf", Pdf, Ahora, "admin.sintetico");

    [Fact]
    public void Registra_vigente_con_huella_y_codigo_normalizado()
    {
        var n = Nueva("  res-sint-001-2023 ");
        Assert.Equal(EstadoNormativa.Vigente, n.Estado);
        Assert.Equal("res-sint-001-2023", n.Codigo);
        Assert.Equal("RES-SINT-001-2023", n.CodigoNormalizado);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Pdf)), n.Sha256);
        Assert.Equal(Pdf.Length, n.Tamano);
    }

    [Theory]
    [InlineData("", "Instructivo", "Consejo")]
    [InlineData("RES-1", " ", "Consejo")]
    [InlineData("RES-1", "Instructivo", "")]
    public void Datos_obligatorios_vacios_se_rechazan(string codigo, string titulo, string emisor)
    {
        var e = Assert.Throws<CatalogoException>(() => new Normativa(codigo, titulo, emisor, new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 1), null, "a.pdf", Pdf, Ahora, "admin"));
        Assert.Equal("validacion", e.Codigo);
    }

    [Fact]
    public void Archivo_que_no_es_pdf_o_vigencia_anterior_a_la_emision_se_rechazan()
    {
        Assert.Throws<CatalogoException>(() => new Normativa("RES-1", "T", "E", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 1),
            null, "a.pdf", "texto plano"u8.ToArray(), Ahora, "admin"));
        Assert.Throws<CatalogoException>(() => new Normativa("RES-1", "T", "E", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 1),
            null, "a.pdf", new byte[Normativa.MaxBytes + 1], Ahora, "admin"));
        Assert.Throws<CatalogoException>(() => new Normativa("RES-1", "T", "E", new DateOnly(2023, 5, 1), new DateOnly(2023, 4, 30),
            null, "a.pdf", Pdf, Ahora, "admin"));
    }

    [Fact]
    public void Una_reforma_deja_la_anterior_como_reformada_y_solo_se_reforma_o_deroga_una_vigente()
    {
        var anterior = Nueva();
        anterior.MarcarReformadaPor(Guid.NewGuid());
        Assert.Equal(EstadoNormativa.Reformada, anterior.Estado);
        Assert.Throws<CatalogoException>(() => anterior.MarcarReformadaPor(Guid.NewGuid()));
        Assert.Throws<CatalogoException>(() => anterior.Derogar("Motivo", Ahora, "admin"));
        // Si se elimina la reforma equivocada, la anterior vuelve a regir.
        anterior.Restituir();
        Assert.Equal(EstadoNormativa.Vigente, anterior.Estado);
        Assert.Null(anterior.ReformadaPorId);
        Assert.Throws<CatalogoException>(() => anterior.Restituir());
        var otra = Nueva("RES-SINT-002-2023");
        Assert.Throws<CatalogoException>(() => otra.Derogar(" ", Ahora, "admin"));
        otra.Derogar("Sustituida por otra norma", Ahora, "admin");
        Assert.Equal(EstadoNormativa.Derogada, otra.Estado);
        Assert.Equal("Sustituida por otra norma", otra.MotivoDerogacion);
    }
}

public class NormativasApiTests
{
    private static readonly byte[] Pdf = "%PDF-1.4\n% normativa sintetica\n%%EOF\n"u8.ToArray();

    internal static MultipartFormDataContent Formulario(string codigo, Guid? reformaA = null, byte[]? archivo = null,
        string vigenteDesde = "2023-03-02", string? titulo = "Instructivo sintético")
    {
        var parte = new ByteArrayContent(archivo ?? Pdf);
        parte.Headers.ContentType = new("application/pdf");
        var form = new MultipartFormDataContent
        {
            { new StringContent(codigo), "codigo" },
            { new StringContent("Consejo sintético"), "emisor" },
            { new StringContent("2023-03-01"), "fechaEmision" },
            { new StringContent(vigenteDesde), "vigenteDesde" },
            { parte, "archivo", "C:\\cliente\\instructivo.pdf" }
        };
        if (titulo is not null) form.Add(new StringContent(titulo), "titulo");
        if (reformaA is not null) form.Add(new StringContent(reformaA.Value.ToString()), "reformaAId");
        return form;
    }

    private static HttpClient Cliente(CatalogoFactory f, string rol, string usuario)
    {
        var c = f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new("Bearer", f.Token(rol, usuario: usuario));
        return c;
    }

    private static async Task<JsonElement> Registrar(HttpClient c, MultipartFormDataContent form)
    {
        using var r = await c.PostAsync("/api/normativas", form);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return await r.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Coordinador")]
    public async Task CA1_Administrador_y_Coordinador_registran_y_se_conserva_el_pdf(string rol)
    {
        using var f = new CatalogoFactory(); using var c = Cliente(f, rol, "usuario.sintetico");
        var n = await Registrar(c, Formulario("RES-SINT-001-2023"));
        Assert.Equal("Vigente", n.GetProperty("estado").GetString());
        Assert.Equal("instructivo.pdf", n.GetProperty("nombreArchivo").GetString());
        Assert.Equal("usuario.sintetico", n.GetProperty("registradaPor").GetString());
        using var pdf = await c.GetAsync($"/api/normativas/{n.GetProperty("id").GetGuid()}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Pdf, await pdf.Content.ReadAsByteArrayAsync());
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Pdf)), pdf.Headers.GetValues("X-Content-SHA256").Single());
    }

    [Fact]
    public async Task CA1_datos_incompletos_o_archivo_no_pdf_se_rechazan_sin_guardar()
    {
        using var f = new CatalogoFactory(); using var c = Cliente(f, "Administrador", "admin.sintetico");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/normativas", Formulario("RES-1", titulo: null))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/normativas", Formulario("RES-1", archivo: "no es pdf"u8.ToArray()))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/normativas", Formulario("RES-1", vigenteDesde: "2023-02-01"))).StatusCode);
        Assert.Empty(await c.GetFromJsonAsync<JsonElement[]>("/api/normativas") ?? []);
    }

    [Fact]
    public async Task CA2_codigo_unico_y_la_reforma_deja_vigente_la_mas_reciente()
    {
        using var f = new CatalogoFactory(); using var c = Cliente(f, "Administrador", "admin.sintetico");
        var original = await Registrar(c, Formulario("RES-SINT-005-2023"));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync("/api/normativas", Formulario(" res-sint-005-2023 "))).StatusCode);
        var idOriginal = original.GetProperty("id").GetGuid();
        var reforma = await Registrar(c, Formulario("RES-SINT-011-2026", idOriginal, vigenteDesde: "2026-03-02"));
        Assert.Equal("Vigente", reforma.GetProperty("estado").GetString());
        Assert.Equal(idOriginal, reforma.GetProperty("reformaA").GetProperty("id").GetGuid());
        var anterior = await c.GetFromJsonAsync<JsonElement>($"/api/normativas/{idOriginal}");
        Assert.Equal("Reformada", anterior.GetProperty("estado").GetString());
        Assert.Equal("RES-SINT-011-2026", anterior.GetProperty("reformadaPor").GetProperty("codigo").GetString());
        // Solo se reforma la vigente: la reformada ya no admite otra reforma.
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsync("/api/normativas", Formulario("RES-SINT-012-2026", idOriginal, vigenteDesde: "2026-03-02"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsync("/api/normativas", Formulario("RES-SINT-013-2026", Guid.NewGuid()))).StatusCode);
        var vigentes = await c.GetFromJsonAsync<JsonElement[]>("/api/normativas?estado=Vigente");
        Assert.Equal(["RES-SINT-011-2026"], vigentes!.Select(v => v.GetProperty("codigo").GetString()));
    }

    [Fact]
    public async Task CA3_Docente_consulta_filtra_y_descarga_pero_no_registra_ni_deroga()
    {
        using var f = new CatalogoFactory(); using var admin = Cliente(f, "Administrador", "admin.sintetico");
        var id = (await Registrar(admin, Formulario("RES-SINT-001-2023"))).GetProperty("id").GetGuid();
        using var docente = Cliente(f, "Docente", "docente.sintetico");
        Assert.Single(await docente.GetFromJsonAsync<JsonElement[]>("/api/normativas") ?? []);
        Assert.Equal(HttpStatusCode.OK, (await docente.GetAsync($"/api/normativas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await docente.GetAsync($"/api/normativas/{id}/pdf")).StatusCode);
        Assert.Empty(await docente.GetFromJsonAsync<JsonElement[]>("/api/normativas?estado=Derogada") ?? []);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PostAsync("/api/normativas", Formulario("RES-2"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.PostAsJsonAsync($"/api/normativas/{id}/derogacion", new { motivo = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await docente.GetAsync("/api/normativas?estado=Otra")).StatusCode);
    }

    [Fact]
    public async Task CA4_derogar_registra_historial_y_no_existe_borrado()
    {
        using var f = new CatalogoFactory(); using var c = Cliente(f, "Coordinador", "coord.sintetico");
        var id = (await Registrar(c, Formulario("RES-SINT-001-2023"))).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/normativas/{id}/derogacion", new { motivo = " " })).StatusCode);
        using var derogar = await c.PostAsJsonAsync($"/api/normativas/{id}/derogacion", new { motivo = "Derogada por resolución sintética" });
        Assert.Equal(HttpStatusCode.OK, derogar.StatusCode);
        Assert.Equal("Derogada", (await derogar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/normativas/{id}/derogacion", new { motivo = "Otra vez" })).StatusCode);
        var historial = await c.GetFromJsonAsync<JsonElement[]>($"/api/normativas/{id}/historial");
        Assert.Equal(["Registrada", "Derogada"], historial!.Select(h => h.GetProperty("tipo").GetString()));
        Assert.All(historial!, h => Assert.Equal("coord.sintetico", h.GetProperty("usuario").GetString()));
        Assert.Equal("Derogada por resolución sintética", historial![1].GetProperty("detalle").GetString());
        // El Coordinador no elimina: solo el Administrador corrige un registro equivocado.
        Assert.Equal(HttpStatusCode.Forbidden, (await c.SendAsync(Eliminar(id, "Error"))).StatusCode);
    }

    internal static HttpRequestMessage Eliminar(Guid id, string? motivo) =>
        new(HttpMethod.Delete, $"/api/normativas/{id}") { Content = JsonContent.Create(new { motivo }) };

    [Fact]
    public async Task Administrador_elimina_un_registro_equivocado_y_queda_en_el_historial()
    {
        using var f = new CatalogoFactory(); using var coord = Cliente(f, "Coordinador", "coord.sintetico");
        var original = (await Registrar(coord, Formulario("RES-SINT-005-2023"))).GetProperty("id").GetGuid();
        var equivocada = (await Registrar(coord, Formulario("RES-SINT-999-2026", original, vigenteDesde: "2026-03-02"))).GetProperty("id").GetGuid();
        using var admin = Cliente(f, "Administrador", "admin.sintetico");
        // La norma ya reformada no se elimina mientras exista su reforma.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(Eliminar(original, "Error"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.SendAsync(Eliminar(equivocada, " "))).StatusCode);
        using var docente = Cliente(f, "Docente", "docente.sintetico");
        Assert.Equal(HttpStatusCode.Forbidden, (await docente.SendAsync(Eliminar(equivocada, "Error"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.SendAsync(Eliminar(equivocada, "Subida por error: código equivocado"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/normativas/{equivocada}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/normativas/{equivocada}/pdf")).StatusCode);
        // La norma que se había marcado como reformada vuelve a estar vigente.
        var restaurada = await admin.GetFromJsonAsync<JsonElement>($"/api/normativas/{original}");
        Assert.Equal("Vigente", restaurada.GetProperty("estado").GetString());
        Assert.Equal(JsonValueKind.Null, restaurada.GetProperty("reformadaPor").ValueKind);
        var eliminada = await admin.GetFromJsonAsync<JsonElement[]>($"/api/normativas/{equivocada}/historial");
        var ultimo = eliminada![^1];
        Assert.Equal("Eliminada", ultimo.GetProperty("tipo").GetString());
        Assert.Equal("admin.sintetico", ultimo.GetProperty("usuario").GetString());
        Assert.Contains("Subida por error", ultimo.GetProperty("detalle").GetString());
        var historialOriginal = await admin.GetFromJsonAsync<JsonElement[]>($"/api/normativas/{original}/historial");
        Assert.Equal(["Registrada", "Reformada", "Restituida"], historialOriginal!.Select(h => h.GetProperty("tipo").GetString()));
        // El código vuelve a quedar libre para registrar la normativa correcta.
        await Registrar(admin, Formulario("RES-SINT-999-2026", original, vigenteDesde: "2026-03-02"));
        Assert.Equal(HttpStatusCode.NotFound, (await admin.SendAsync(Eliminar(Guid.NewGuid(), "Error"))).StatusCode);
    }

    [Fact]
    public async Task CA4_la_reforma_queda_en_el_historial_de_la_norma_reformada()
    {
        using var f = new CatalogoFactory(); using var c = Cliente(f, "Administrador", "admin.sintetico");
        var id = (await Registrar(c, Formulario("RES-SINT-005-2023"))).GetProperty("id").GetGuid();
        await Registrar(c, Formulario("RES-SINT-011-2026", id, vigenteDesde: "2026-03-02"));
        var historial = await c.GetFromJsonAsync<JsonElement[]>($"/api/normativas/{id}/historial");
        Assert.Equal(["Registrada", "Reformada"], historial!.Select(h => h.GetProperty("tipo").GetString()));
        Assert.Contains("RES-SINT-011-2026", historial![1].GetProperty("detalle").GetString());
    }

    [Fact]
    public async Task CA6_sin_sesion_401_y_recurso_inexistente_404()
    {
        using var f = new CatalogoFactory(); using var anonimo = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/api/normativas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.PostAsync("/api/normativas", Formulario("RES-1"))).StatusCode);
        using var c = Cliente(f, "Administrador", "admin.sintetico");
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/normativas/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/normativas/{Guid.NewGuid()}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/normativas/{Guid.NewGuid()}/derogacion", new { motivo = "x" })).StatusCode);
    }
}
