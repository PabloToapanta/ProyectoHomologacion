using Catalogo.Application.ImportacionProgramas;
using Xunit;

namespace Catalogo.Tests;

/// <summary>
/// Plan analítico sintético con la misma disposición de página del formato FISEI (coordenadas en puntos PDF,
/// Y = base de la palabra). No contiene texto de programas reales: el repositorio es público.
/// </summary>
internal sealed class PlanSintetico
{
    private readonly List<PalabraPdf> palabras = [];
    public IReadOnlyList<PalabraPdf> Palabras => palabras;

    public PlanSintetico Linea(int pagina, double x, double y, string texto)
    {
        var cx = x;
        foreach (var w in texto.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            palabras.Add(new(pagina, cx, y, w));
            cx += w.Length * 4.6 + 3;
        }
        return this;
    }

    public PlanSintetico Sin(string texto)
    {
        palabras.RemoveAll(p => p.Texto == texto);
        return this;
    }

    public static PlanSintetico Encabezado(string nivel = "PRIMERO", string creditos = "3", string total = "144") => new PlanSintetico()
        .Linea(1, 230, 777, "I. INFORMACIÓN GENERAL")
        .Linea(1, 30, 760, "Nombre de la asignatura").Linea(1, 30, 750, "ASIGNATURA SINTETICA")
        .Linea(1, 30, 740, "Carrera").Linea(1, 30, 730, "Software")
        .Linea(1, 30, 708, "Código SINT-01-02").Linea(1, 323, 708, "PRERREQUISITOS")
        .Linea(1, 30, 676, "Modalidad").Linea(1, 183, 676, "Asignatura").Linea(1, 362, 676, "Código")
        .Linea(1, 30, 620, "Créditos/Horas").Linea(1, 326, 620, "CORREQUISITOS")
        .Linea(1, 30, 610, creditos).Linea(1, 183, 607, "Asignatura").Linea(1, 362, 607, "Código")
        .Linea(1, 30, 600, "Nivel").Linea(1, 183, 594, "OTRA SINTETICA").Linea(1, 362, 594, "SINT-01-03")
        .Linea(1, 30, 590, nivel)
        .Linea(2, 30, 700, "TOTAL DE HORAS AL SEMESTRE").Linea(2, 400, 700, total)
        .Linea(4, 257, 777, "III CONTENIDO")
        .Linea(4, 38, 756, "Unidades curriculares").Linea(4, 157, 756, "Unidades temáticas").Linea(4, 282, 756, "Contenidos mínimos")
        .Linea(4, 393, 756, "No horas").Linea(4, 461, 756, "Resultados de").Linea(4, 464, 746, "aprendizaje");

    /// <summary>Dos unidades de 72 h en una página; la primera con dos temáticas.</summary>
    public static PlanSintetico Completo() => Encabezado()
        .Linea(4, 72, 732, "U.1").Linea(4, 132, 732, "Tema sintetico uno").Linea(4, 132, 722, "continuacion uno")
        .Linea(4, 260, 732, "Contenido sintetico A,").Linea(4, 260, 722, "parte final A.")
        .Linea(4, 406, 732, "72").Linea(4, 439, 732, "Resultado sintetico").Linea(4, 439, 722, "de la unidad uno.")
        .Linea(4, 132, 679, "Tema sintetico dos").Linea(4, 260, 679, "Contenido sintetico B.")
        .Linea(4, 72, 616, "U.2").Linea(4, 132, 616, "Tema sintetico tres").Linea(4, 260, 616, "Contenido sintetico C.")
        .Linea(4, 406, 616, "72").Linea(4, 447, 616, "Resultado sintetico dos.")
        .Cierre(4, 348);

    public PlanSintetico Cierre(int pagina, double y) =>
        Linea(pagina, 267, y + 10, "Resultado general sintetico de la asignatura")
        .Linea(pagina, 61, y, "Resultados del aprendizaje").Linea(pagina, 166, y, "de la asignatura")
        .Linea(pagina, 36, y - 22, "Total de horas").Linea(pagina, 393, y - 22, "144")
        .Linea(pagina, 465, 49, $"Página {pagina + 1}/9");
}

public class AnalizadorPlanAnaliticoTests
{
    [Fact]
    public void Extrae_asignatura_unidades_tematicas_contenidos_y_resultados()
    {
        var b = AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Completo().Palabras)!;
        Assert.Equal("SINT-01-02", b.Codigo);
        Assert.Equal("ASIGNATURA SINTETICA", b.Nombre);
        Assert.Equal(1, b.Nivel);
        Assert.Equal(3m, b.Creditos);
        Assert.Equal(144, b.Horas);
        Assert.Equal([1, 2], b.Unidades.Select(u => u.Numero));
        var u1 = b.Unidades[0];
        Assert.Equal(72, u1.Horas);
        Assert.Equal("Resultado sintetico de la unidad uno.", u1.ResultadoAprendizaje);
        Assert.Equal(["Tema sintetico uno continuacion uno", "Tema sintetico dos"], u1.Tematicas.Select(t => t.Nombre));
        Assert.Equal(["Contenido sintetico A, parte final A."], u1.Tematicas[0].Contenidos);
        Assert.Equal(["Contenido sintetico B."], u1.Tematicas[1].Contenidos);
        Assert.Equal("Tema sintetico uno continuacion uno / Tema sintetico dos", u1.Nombre);
        Assert.Equal("Resultado sintetico dos.", b.Unidades[1].ResultadoAprendizaje);
        Assert.Empty(b.Faltantes);
        // El nombre de unidad no existe en el formato: se propone y se advierte.
        Assert.Contains(b.Advertencias, a => a.Contains("nombre de cada unidad", StringComparison.Ordinal));
    }

    [Fact]
    public void Excluye_resultado_general_total_y_pie_de_pagina()
    {
        var b = AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Completo().Palabras)!;
        var texto = string.Join(' ', b.Unidades.SelectMany(u => u.Tematicas.SelectMany(t => t.Contenidos).Append(u.ResultadoAprendizaje)));
        Assert.DoesNotContain("general", texto);
        Assert.DoesNotContain("Página", texto);
        Assert.DoesNotContain("144", texto);
    }

    [Fact]
    public void Une_la_tabla_que_continua_en_la_pagina_siguiente()
    {
        var plan = PlanSintetico.Encabezado()
            .Linea(4, 72, 732, "U.1").Linea(4, 132, 732, "Tema sintetico uno").Linea(4, 260, 732, "Contenido sintetico A.")
            .Linea(4, 406, 732, "72").Linea(4, 439, 732, "Resultado sintetico uno.")
            .Linea(4, 465, 49, "Página 5/9")
            .Linea(5, 72, 744, "U.2").Linea(5, 132, 744, "Tema sintetico dos").Linea(5, 260, 744, "Contenido sintetico B.")
            .Linea(5, 406, 744, "72").Linea(5, 439, 744, "Resultado sintetico dos.")
            .Cierre(5, 600);
        var b = AnalizadorPlanAnaliticoFisei.Analizar(plan.Palabras)!;
        Assert.Equal([1, 2], b.Unidades.Select(u => u.Numero));
        Assert.Equal(["Contenido sintetico B."], b.Unidades[1].Tematicas.Single().Contenidos);
        Assert.Empty(b.Faltantes);
    }

    [Fact]
    public void Contenido_que_cruza_de_pagina_permanece_en_su_unidad()
    {
        var plan = PlanSintetico.Encabezado()
            .Linea(4, 72, 732, "U.1").Linea(4, 132, 732, "Tema sintetico uno").Linea(4, 260, 732, "Contenido sintetico A.")
            .Linea(4, 406, 732, "144").Linea(4, 439, 732, "Resultado sintetico")
            .Linea(5, 439, 744, "que continua.")
            .Cierre(5, 600);
        var u = Assert.Single(AnalizadorPlanAnaliticoFisei.Analizar(plan.Palabras)!.Unidades);
        Assert.Equal("Resultado sintetico que continua.", u.ResultadoAprendizaje);
    }

    [Theory]
    [InlineData("SEGUNDO", 2)]
    [InlineData("SÉPTIMO", 7)]
    [InlineData("Septimo", 7)]
    [InlineData("NOVENO", 9)]
    [InlineData("DÉCIMO", 10)]
    [InlineData("4", 4)]
    public void Reconoce_niveles_ordinales(string nivel, int esperado) =>
        Assert.Equal(esperado, AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Encabezado(nivel).Linea(4, 72, 732, "U.1").Cierre(4, 348).Palabras)!.Nivel);

    [Fact]
    public void Dato_ausente_o_ilegible_queda_vacio_y_marcado_sin_suponerlo()
    {
        var b = AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Encabezado("UNDECIMO", "tres").Palabras)!;
        Assert.Null(b.Nivel);
        Assert.Null(b.Creditos);
        Assert.Empty(b.Unidades);
        Assert.Contains(b.Faltantes, f => f.Contains("nivel", StringComparison.Ordinal));
        Assert.Contains(b.Faltantes, f => f.Contains("créditos", StringComparison.Ordinal));
        Assert.Contains(b.Faltantes, f => f.Contains("unidades", StringComparison.Ordinal));
    }

    [Fact]
    public void Unidad_sin_horas_resultado_o_contenidos_se_marca()
    {
        var plan = PlanSintetico.Encabezado().Linea(4, 72, 732, "U.1").Linea(4, 132, 732, "Tema sintetico uno").Cierre(4, 348);
        var b = AnalizadorPlanAnaliticoFisei.Analizar(plan.Palabras)!;
        var u = Assert.Single(b.Unidades);
        Assert.Null(u.Horas);
        Assert.Equal("", u.ResultadoAprendizaje);
        Assert.Empty(u.Tematicas.Single().Contenidos);
        Assert.Contains(b.Faltantes, f => f == "U.1: horas");
        Assert.Contains(b.Faltantes, f => f == "U.1: resultado de aprendizaje");
        Assert.Contains(b.Faltantes, f => f == "U.1: contenidos");
    }

    [Fact]
    public void Advierte_si_las_horas_de_las_unidades_no_suman_el_total()
    {
        var b = AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Encabezado(total: "160").Linea(4, 72, 732, "U.1").Linea(4, 406, 732, "144").Cierre(4, 348).Palabras)!;
        Assert.Contains(b.Advertencias, a => a.Contains("144", StringComparison.Ordinal) && a.Contains("160", StringComparison.Ordinal));
    }

    [Fact]
    public void No_toma_el_codigo_de_prerrequisitos_o_correquisitos()
    {
        var b = AnalizadorPlanAnaliticoFisei.Analizar(PlanSintetico.Completo().Sin("SINT-01-02").Palabras)!;
        Assert.Null(b.Codigo);
        Assert.Contains(b.Faltantes, f => f.Contains("código", StringComparison.Ordinal));
    }

    [Fact]
    public void Documento_de_otro_formato_no_genera_borrador()
    {
        var otro = new PlanSintetico().Linea(1, 30, 700, "Factura sintetica").Linea(1, 30, 680, "Total 144");
        Assert.Null(AnalizadorPlanAnaliticoFisei.Analizar(otro.Palabras));
        Assert.Null(AnalizadorPlanAnaliticoFisei.Analizar([]));
    }
}
