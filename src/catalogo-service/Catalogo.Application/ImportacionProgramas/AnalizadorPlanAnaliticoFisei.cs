using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Catalogo.Application.ImportacionProgramas;

/// <summary>Palabra extraída de un PDF: página, borde izquierdo y línea base en puntos (Y crece hacia arriba).</summary>
public sealed record PalabraPdf(int Pagina, double X, double Y, string Texto);
public sealed record TematicaBorrador(string Nombre, IReadOnlyList<string> Contenidos);
public sealed record UnidadBorrador(int Numero, string Nombre, int? Horas, string ResultadoAprendizaje, IReadOnlyList<TematicaBorrador> Tematicas);
/// <summary>Borrador para revisión humana. Un dato no encontrado queda null o vacío y figura en <see cref="Faltantes"/>.</summary>
public sealed record ProgramaBorrador(string? Codigo, string? Nombre, int? Nivel, decimal? Creditos, int? Horas,
    IReadOnlyList<UnidadBorrador> Unidades, IReadOnlyList<string> Faltantes, IReadOnlyList<string> Advertencias);

/// <summary>
/// HU-08: analizador determinista del plan analítico FISEI (I. Información general y III. Contenido).
/// Las columnas de la tabla se ubican a partir de su encabezado; no se completa ningún dato por suposición.
/// </summary>
public static partial class AnalizadorPlanAnaliticoFisei
{
    private sealed record Linea(int Pagina, double Y, double X, string Texto);
    private const double ToleranciaLinea = 2.5;
    private const double SaltoBloque = 15;

    public static ProgramaBorrador? Analizar(IReadOnlyList<PalabraPdf> palabras)
    {
        if (palabras is null || palabras.Count == 0) return null;
        var izquierda = palabras.Where(p => p.X < 180).ToArray();
        var lineasIzquierda = Lineas(izquierda);
        var inicioNombre = lineasIzquierda.FindIndex(l => Igual(l.Texto, "Nombre de la asignatura"));
        var tabla = UbicarTabla(palabras);
        if (inicioNombre < 0 && tabla is null) return null;

        var faltantes = new List<string>();
        var advertencias = new List<string>();
        string? nombre = null, codigo = null; int? nivel = null; decimal? creditos = null;
        if (inicioNombre >= 0)
        {
            var paginaEncabezado = lineasIzquierda[inicioNombre].Pagina;
            var encabezado = lineasIzquierda.Where(l => l.Pagina == paginaEncabezado).ToList();
            var desde = encabezado.FindIndex(l => Igual(l.Texto, "Nombre de la asignatura"));
            var partes = encabezado.Skip(desde + 1).TakeWhile(l => !Igual(l.Texto, "Carrera")).Select(l => l.Texto).ToArray();
            nombre = partes.Length is > 0 and <= 3 ? string.Join(' ', partes) : null;
            // El primer «Código» del encabezado es el de la asignatura; los de prerrequisitos están a la derecha.
            var lineaCodigo = encabezado.FirstOrDefault(l => l.Texto.StartsWith("Código ", StringComparison.Ordinal) || l.Texto == "Código");
            var valorCodigo = lineaCodigo?.Texto.Length > 7 ? lineaCodigo.Texto[7..].Trim() : null;
            codigo = string.IsNullOrWhiteSpace(valorCodigo) || valorCodigo.Contains(' ') ? null : valorCodigo;
            creditos = decimal.TryParse(Siguiente(encabezado, "Créditos/Horas")?.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var c) && c > 0 ? c : null;
            nivel = Nivel(Siguiente(encabezado, "Nivel"));
        }
        if (codigo is null) faltantes.Add("código de la asignatura");
        if (nombre is null) faltantes.Add("nombre de la asignatura");
        if (nivel is null) faltantes.Add("nivel");
        if (creditos is null) faltantes.Add("créditos");

        var total = Lineas(palabras).FirstOrDefault(l => l.Texto.StartsWith("TOTAL DE HORAS AL SEMESTRE", StringComparison.OrdinalIgnoreCase));
        var numeroTotal = total is null ? null : NumeroAlFinal().Match(total.Texto);
        int? horas = numeroTotal is { Success: true } && int.TryParse(numeroTotal.Groups[1].Value, out var h) && h > 0 ? h : null;
        if (horas is null) faltantes.Add("total de horas al semestre");

        var unidades = tabla is null ? [] : Unidades(palabras, tabla, faltantes);
        if (unidades.Count == 0) faltantes.Add("unidades de la tabla III. Contenido");
        else
        {
            advertencias.Add("El plan no indica el nombre de cada unidad: se propone a partir de sus unidades temáticas; revíselo.");
            var suma = unidades.Sum(u => u.Horas ?? 0);
            if (horas is not null && unidades.All(u => u.Horas is not null) && suma != horas)
                advertencias.Add($"Las horas de las unidades suman {suma} y el total del plan es {horas}; revise ambos datos.");
        }
        return new(codigo, nombre, nivel, creditos, horas, unidades, faltantes, advertencias);
    }

    private sealed record Tabla(int Pagina, double Y, double Tematicas, double Contenidos, double Horas, double Resultados);

    private static Tabla? UbicarTabla(IReadOnlyList<PalabraPdf> palabras)
    {
        foreach (var pagina in palabras.Select(p => p.Pagina).Distinct().Order())
        {
            var ws = palabras.Where(p => p.Pagina == pagina).ToArray();
            var contenidos = ws.FirstOrDefault(p => p.Texto == "Contenidos" && ws.Any(q => q.Texto == "mínimos" && Math.Abs(q.Y - p.Y) <= ToleranciaLinea));
            if (contenidos is null || !ws.Any(p => p.Texto == "CONTENIDO" && p.Y > contenidos.Y)) continue;
            var fila = ws.Where(p => Math.Abs(p.Y - contenidos.Y) <= ToleranciaLinea).ToArray();
            var tematicas = fila.Where(p => p.Texto == "Unidades").OrderBy(p => p.X).Skip(1).FirstOrDefault();
            var no = fila.FirstOrDefault(p => p.Texto == "No");
            var resultados = fila.FirstOrDefault(p => p.Texto == "Resultados");
            if (tematicas is null || no is null || resultados is null) return null;
            return new(pagina, contenidos.Y, tematicas.X - 30, contenidos.X - 27, no.X - 10, (no.X + resultados.X) / 2);
        }
        return null;
    }

    private static List<UnidadBorrador> Unidades(IReadOnlyList<PalabraPdf> palabras, Tabla t, List<string> faltantes)
    {
        // Región de la tabla: bajo el encabezado, sobre el pie de página y hasta la fila de resultados de la asignatura.
        var region = new List<PalabraPdf>();
        foreach (var pagina in palabras.Select(p => p.Pagina).Distinct().Order().Where(p => p >= t.Pagina))
        {
            var ws = palabras.Where(p => p.Pagina == pagina && p.Y > 60 && (pagina != t.Pagina || p.Y < t.Y - 15)).ToArray();
            var cierre = ws.Where(p => p.X < t.Tematicas && (p.Texto == "Resultados" || p.Texto == "Total")).Select(p => (double?)p.Y).Max();
            // El resultado general de la asignatura comienza unos 10 pt sobre su etiqueta; la última unidad termina más arriba.
            region.AddRange(cierre is null ? ws : ws.Where(p => p.Y > cierre + 16));
            if (cierre is not null) break;
        }
        var marcas = region.Where(p => p.X < t.Tematicas && MarcaUnidad().IsMatch(p.Texto))
            .OrderBy(p => p.Pagina).ThenByDescending(p => p.Y).ToList();
        var unidades = new List<UnidadBorrador>();
        for (var i = 0; i < marcas.Count; i++)
        {
            var marca = marcas[i];
            var siguiente = i + 1 < marcas.Count ? marcas[i + 1] : null;
            bool Dentro(PalabraPdf p) => Despues(p, marca) && (siguiente is null || !Despues(p, siguiente));
            var celdas = region.Where(p => p.X >= t.Tematicas && Dentro(p)).ToArray();
            var numero = int.Parse(MarcaUnidad().Match(marca.Texto).Groups[1].Value, CultureInfo.InvariantCulture);
            var bloquesTema = Bloques(celdas.Where(p => p.X < t.Contenidos));
            var bloquesContenido = Bloques(celdas.Where(p => p.X >= t.Contenidos && p.X < t.Horas));
            var horasTexto = Lineas(celdas.Where(p => p.X >= t.Horas && p.X < t.Resultados)).Select(l => l.Texto).FirstOrDefault();
            int? horas = int.TryParse(horasTexto, NumberStyles.None, CultureInfo.InvariantCulture, out var h) && h > 0 ? h : null;
            var resultado = string.Join(' ', Lineas(celdas.Where(p => p.X >= t.Resultados)).Select(l => l.Texto));

            List<(Linea? Linea, string Nombre, List<string> Contenidos)> tematicas = bloquesTema.Count == 0
                ? [(null, "", [])]
                : bloquesTema.Select(b => ((Linea?)b.Inicio, b.Texto, new List<string>())).ToList();
            foreach (var bloque in bloquesContenido)
            {
                // Cada bloque de contenidos pertenece a la temática que empieza a su altura o por encima.
                var destino = tematicas.LastOrDefault(x => x.Linea is null || !Antes(bloque.Inicio, x.Linea, 5));
                (destino.Contenidos ?? tematicas[0].Contenidos).Add(bloque.Texto);
            }
            var etiqueta = $"U.{numero}";
            if (horas is null) faltantes.Add($"{etiqueta}: horas");
            if (resultado.Length == 0) faltantes.Add($"{etiqueta}: resultado de aprendizaje");
            if (tematicas.Any(x => x.Nombre.Length == 0)) faltantes.Add($"{etiqueta}: unidades temáticas");
            if (tematicas.Any(x => x.Contenidos.Count == 0)) faltantes.Add($"{etiqueta}: contenidos");
            var nombres = tematicas.Select(x => x.Nombre).Where(n => n.Length > 0).ToArray();
            var nombre = string.Join(" / ", nombres);
            if (nombre.Length > 200) nombre = nombres[0];
            unidades.Add(new(numero, nombre, horas, resultado, tematicas.Select(x => new TematicaBorrador(x.Nombre, x.Contenidos)).ToArray()));
        }
        return unidades;
    }

    /// <summary>La palabra está en la marca o debajo de ella, en lectura por páginas.</summary>
    private static bool Despues(PalabraPdf p, PalabraPdf marca) => p.Pagina > marca.Pagina || (p.Pagina == marca.Pagina && p.Y <= marca.Y + 3);
    private static bool Antes(Linea a, Linea b, double margen) => a.Pagina < b.Pagina || (a.Pagina == b.Pagina && a.Y > b.Y + margen);

    private static List<(Linea Inicio, string Texto)> Bloques(IEnumerable<PalabraPdf> palabras)
    {
        var bloques = new List<(Linea Inicio, List<string> Lineas)>();
        Linea? anterior = null;
        foreach (var linea in Lineas(palabras))
        {
            if (anterior is null || linea.Pagina != anterior.Pagina || anterior.Y - linea.Y > SaltoBloque) bloques.Add((linea, []));
            bloques[^1].Lineas.Add(linea.Texto);
            anterior = linea;
        }
        return bloques.Select(b => (b.Inicio, string.Join(' ', b.Lineas))).ToList();
    }

    private static List<Linea> Lineas(IEnumerable<PalabraPdf> palabras)
    {
        var lineas = new List<(int Pagina, double Y, List<PalabraPdf> Palabras)>();
        foreach (var p in palabras.OrderBy(p => p.Pagina).ThenByDescending(p => p.Y).ThenBy(p => p.X))
        {
            if (lineas.Count > 0 && lineas[^1].Pagina == p.Pagina && Math.Abs(lineas[^1].Y - p.Y) <= ToleranciaLinea) lineas[^1].Palabras.Add(p);
            else lineas.Add((p.Pagina, p.Y, [p]));
        }
        return lineas.Select(l => new Linea(l.Pagina, l.Y, l.Palabras.Min(p => p.X),
            string.Join(' ', l.Palabras.OrderBy(p => p.X).Select(p => p.Texto.Trim()).Where(t => t.Length > 0)))).ToList();
    }

    private static string? Siguiente(List<Linea> lineas, string etiqueta)
    {
        var i = lineas.FindIndex(l => Igual(l.Texto, etiqueta));
        return i >= 0 && i + 1 < lineas.Count ? lineas[i + 1].Texto : null;
    }

    private static readonly string[] Ordinales = ["PRIMERO", "SEGUNDO", "TERCERO", "CUARTO", "QUINTO", "SEXTO", "SEPTIMO", "OCTAVO", "NOVENO", "DECIMO"];
    private static int? Nivel(string? texto)
    {
        if (texto is null) return null;
        if (int.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is > 0 and <= 20) return n;
        var i = Array.IndexOf(Ordinales, SinTildes(texto).ToUpperInvariant());
        return i >= 0 ? i + 1 : null;
    }
    private static string SinTildes(string texto) => new(texto.Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    private static bool Igual(string a, string b) => string.Equals(a, b, StringComparison.Ordinal);

    [GeneratedRegex(@"^U\.?\s*(\d{1,2})$")]
    private static partial Regex MarcaUnidad();
    [GeneratedRegex(@"(\d+)\s*$")]
    private static partial Regex NumeroAlFinal();
}
