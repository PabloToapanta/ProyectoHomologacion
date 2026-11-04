# HU-08 — carga masiva de programas analíticos / Issue #50

Responsable: Edwin Caraguay. Rama `feature/hu-08-carga-programas` desde `develop` `cbe9ac1`.
Fecha: 04/11/2026. Catálogo es el módulo de Leslie, que debe hacer la revisión cruzada.

## Decisiones aplicadas

- **Alcance sin agente obligatorio** (propuesta, decisión recomendada): según Edwin, Shirley lo confirmó.
  La extracción y el análisis son deterministas, seguidos de revisión humana. Sin OCR.
- **Formato:** se fijó con los 40 planes analíticos reales de la carrera de Software (páginas 2–7), leídos
  solo en el equipo local. **No se suben al repositorio**, que es público: las pruebas usan PDF sintéticos
  con la misma disposición de página.
- **PDF original:** se guarda **al confirmar**, vinculado a la asignatura, con su hash SHA-256, nombre, fecha
  y usuario, para trazabilidad (decisión de Edwin del 04/11/2026). Los PDF que no se confirman no se guardan.
- **Biblioteca:** PdfPig 0.1.11 (licencia Apache-2.0), solo en `Catalogo.Infrastructure`, detrás del puerto
  `IExtractorTextoPdf`.
- **Límites técnicos:** 20 archivos por lote, 5 MB por archivo, 25 MB por lote, 30 páginas y 50 000 palabras
  por PDF. nginx sube su límite de cuerpo de 1 MB a 26 MB solo para `/api/`.
- **Código existente:** se rechaza con 409 y no se sobrescribe (punto 6 de la propuesta).

## Contrato implementado (difiere de la propuesta)

La propuesta guardaba borradores en un lote. Como el PDF solo se conserva al confirmar, los borradores no se
persisten: el análisis los devuelve, el Administrador los corrige en pantalla y la confirmación lo guarda todo
de una vez. Si se cierra la pantalla, se vuelve a analizar el PDF.

| Método | Ruta (Gateway → Catálogo) | Resultado |
|---|---|---|
| POST | `/api/importaciones-programas/analisis` | Multipart `carreraId` + `archivos`: 200 con un resultado por archivo (`Borrador`, `SinTexto`, `Invalido`). No escribe en el catálogo. |
| POST | `/api/importaciones-programas/confirmacion` | Multipart `programa` (JSON revisado) + `archivo` (PDF): 201. Guarda asignatura, unidades, temáticas y PDF en una transacción; 409 si el código existe. |
| GET | `/api/asignaturas/{id}/programa-analitico` | Devuelve el PDF original con las cabeceras `X-Content-SHA256`, `X-Cargado-Por` y `X-Fecha-Carga`. |

Las tres rutas son solo para Administrador. Migración de Catálogo: `20261104083910_ProgramasAnaliticosHu08`,
que crea la tabla `ProgramasAnaliticos` con una fila por asignatura.

**Correspondencia del plan con el catálogo:**

| Dato del catálogo | Origen en el plan analítico |
|---|---|
| Código | Campo Código del encabezado |
| Nombre | Nombre de la asignatura |
| Nivel | Ordinal PRIMERO a DÉCIMO |
| Créditos | Créditos/Horas |
| Horas | TOTAL DE HORAS AL SEMESTRE |
| Unidades | Filas U.n de la tabla III. Contenido |
| Temáticas y contenidos | Bloques de Unidades temáticas y de Contenidos mínimos de cada fila |
| Resultado de aprendizaje | Columna Resultados de aprendizaje |

El plan no tiene nombre de unidad: se propone a partir de sus temáticas y se advierte que debe revisarse.
Si la suma de horas de las unidades no coincide con el total, también se advierte.

## Criterios de #50

| Criterio | Evidencia | Estado |
|---|---|---|
| CA1 — varios PDF con un resultado por archivo | API y smoke: lote de 4 archivos con 4 estados | Verificado |
| CA2 — borrador sin guardar | Pruebas del analizador y del servicio; smoke «analisis no guarda» (0 asignaturas) | Verificado |
| CA3 — corrección y confirmación con HU-05/HU-06 | Servicio, API e interfaz; smoke con unidades y temáticas consultables | Verificado |
| CA4 — no PDF, demasiado grande, sin texto o sin formato | Servicio, extractor y smoke; un fallo no afecta a los demás | Verificado |
| CA5 — no sobrescribe un código existente | Servicio, API y smoke (409) | Verificado |
| CA6 — 403/401 y validación por contenido | API, Gateway y smoke; un PDF con nombre `.txt` se acepta por contenido y un texto con nombre `.pdf` se rechaza | Verificado |

## Ejecuciones del 04/11/2026

TDD: el analizador, el servicio y la interfaz se escribieron primero como pruebas fallidas.

1. Catálogo: 126/126, de las cuales 36 son de HU-08 (analizador, servicio, extractor con PDF sintético y API).
2. Gateway: 113/113 (OpenAPI de 24 a 27 rutas y enrutamiento de las tres nuevas).
3. Frontend: 127/127 y `npm run build`.
4. Verificación local con los **40 planes analíticos reales** (sin publicarlos): todos generan borrador sin
   datos faltantes, y en los 40 la suma de horas de las unidades coincide con el total. Revisión manual de
   tres planes (Álgebra Lineal, Estructura de Datos y Computación Visual) contra el PDF: la estructura coincide.
   Imperfecciones que corrige la revisión humana: un contenido que cruza de página puede quedar partido en dos
   bloques, y dos bloques muy próximos pueden quedar unidos.
5. `tests/security/hu08-smoke.py` con Docker Compose, Gateway, JWT cifrado y SQL Server real, sobre una
   copia limpia de la rama: **17/17** en la revisión independiente (conteo corregido;
   antes constaba 19/19).
   - Comprueba 401/403, el lote de 4 archivos, que el análisis no guarda, la confirmación con unidades y temáticas, el 409 y el PDF idéntico tras reiniciar Catálogo, con su SHA-256 y usuario.
   - Un trigger que hace fallar el guardado del PDF deja la base sin asignatura huérfana: la transacción se deshace.
   - Con `HU08_REAL_DIR` apuntando a una copia local de los 40 planes reales (solo se registran conteos): 40 borradores completos, 0 con faltantes, 0 sin texto y 0 inválidos.
   - Proyecto Docker aleatorio con volúmenes eliminados. HTTP local aislado: no acredita HTTPS.

**No verificado:** CI en GitHub, revisión cruzada de Leslie (Catálogo) y de Pablo (Gateway), aceptación de
Shirley y la prueba en navegador real de la pantalla de carga.

## Revisión independiente para PR del 04/11/2026

Sobre `9aa159e` se reprodujeron y corrigieron tres casos:

- Unidades nulas en el JSON: una prueba de servicio falló con `NullReferenceException`;
  ahora se rechazan como validación antes de escribir.
- Dos archivos con el mismo nombre: una prueba de interfaz falló porque confirmar uno
  ocultaba ambos. La confirmación ahora identifica la posición del archivo en el lote.
- Borrador sin unidades: la prueba falló porque no existía un control para completarlo.
  La revisión ahora permite añadir/eliminar unidades y temáticas y editar sus números.
  Horas, nombres, resultados y contenidos nuevos quedan vacíos; no se inventan datos.

Nueva ejecución: Catálogo **126/126**, Gateway **113/113**, frontend **129/129**,
TypeScript y compilación Vite aprobados. Backend con SDK .NET 8 en contenedor.
`git diff --check` sin errores.

Smoke integrado sobre copia aislada de los archivos versionados más las correcciones,
excluyendo los ajustes locales de puertos: **17/17** comprobaciones, Gateway/JWE y SQL
Server 2022 reales. Se verificaron permisos, no guardar al analizar, confirmación,
duplicados, PDF idéntico tras reinicio, SHA-256, usuario y reversión de la transacción
ante fallo de guardado. Proyecto temporal eliminado con sus volúmenes. Transporte
HTTP local: no acredita HTTPS. No se repitió la extracción sobre los 40 planes reales
ni la prueba manual en navegador. La aprobación reportada del alcance sin agente y
del contrato aplicado debe registrarse en #50 antes de aceptar la historia.

## Prueba en navegador del 04/11/2026

Sobre `785b42f`, con un stack aislado de Docker Compose (HTTP local, credenciales aleatorias),
en el navegador integrado de la aplicación de escritorio:

1. Inicio de sesión como Administrador y paso de Catálogo a la pestaña **Programas analíticos**.
2. Carrera sintética y un lote de dos archivos: el plan sintético y un texto con nombre `.pdf`.
   El navegador integrado no tiene herramienta de subida, así que los archivos se asignaron al
   campo de archivo con `DataTransfer` desde JavaScript. Los demás pasos fueron clics y escritura reales.
3. Resultado por archivo: `plan-sintetico.pdf · Borrador` y `disfrazado.pdf · Invalido · El archivo no es un PDF.`
4. La revisión muestra la advertencia del nombre de unidad propuesto y precarga código, nombre, nivel 2,
   3 créditos, 144 h y dos unidades de 72 h con sus temáticas y contenidos.
5. Se corrigió el nombre de la unidad 1 y se confirmó: aparece «plan-sintetico.pdf guardado en el catálogo.»
   y el archivo pasa a «Confirmado».
6. Por la API: la asignatura `DEMO-SINT-01` tiene la unidad 1 con el nombre corregido y sus dos temáticas.
   El PDF descargado es idéntico al cargado, la cabecera SHA-256 coincide y figura como cargado por `admin.demo`.

No se probó un plan real en el navegador: superaba el tamaño práctico de la inyección por JavaScript y la CSP
de la página impide descargarlo de otro origen. Los 40 planes reales pasaron por el stack en el smoke.
Pasa a verificado «prueba en navegador real de la pantalla de carga» con plan sintético; siguen pendientes
CI, revisión cruzada y aceptación de Shirley. Stack y copia temporal eliminados.
