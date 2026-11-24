# HU-07 — Contrato para revisar con Pablo y Edwin

Relacionado con [Issue #68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68), [spec](hu-07-versiones-malla.md) y [evidencia T58/T59](evidencia-hu-07-t58-t59.md). Pablo confirmó este contrato en el mensaje transmitido por Leslie el 22/11/2026. Se implementó el incremento de Homologación e informe descrito aquí; consultar [evidencia CA3](evidencia-hu-07-ca3.md). La revisión cruzada y la aceptación funcional siguen pendientes.

## Lo que Catálogo ya proporciona

Los DTO de asignaturas conservan sus campos anteriores y añaden `mallaId` (UUID) y `malla`:

```json
{
  "mallaId": "80000000-0000-0000-0000-000000000001",
  "malla": {
    "id": "80000000-0000-0000-0000-000000000001",
    "carreraId": "80000000-0000-0000-0000-000000000002",
    "nombre": "Plan sintético",
    "vigente": true
  }
}
```

`GET /api/asignaturas/{id}/analisis-interno/contexto` añade el mismo objeto `malla` y mantiene `carreraVigente` independiente. El listado privado de receptoras añade estos campos de forma compatible. Las lecturas privadas mantienen JWT de docente y clave interna; no se publican esas rutas en Gateway ni se abre la gestión a otros roles.

El identificador de asignatura no cambia al activar otra malla y nunca se traslada a una versión diferente. Asignaturas de versiones distintas con el mismo código tienen UUID distintos. Los vínculos revisados siguen siendo por UUID de asignatura y elementos; no se infieren correspondencias por código.

## Ampliación acordada e implementada

1. En `Homologacion.Application/AnalisisInterno/ContratoSolicitud.cs`, añadir propiedad init nullable de malla a `AsignaturaParaAnalisis`, con un DTO propio de Homologación; no referenciar el proyecto de Catálogo ni cambiar constructores posicionales históricos. `FuentesAnalisisHttp` podrá deserializarla con sus opciones Web actuales.
2. Al construir cada análisis nuevo, capturar `id`, `carreraId`, `nombre` y **vigenteAlAnalizar** de origen/receptora en metadatos del resultado; `AnalisisGuardado` conservará la instantánea mediante su JSON existente. No consultar la vigencia actual al volver a leer el análisis.
3. En agrupaciones, guardar una lista de instantáneas por asignatura: `asignaturaCatalogoId`, `asignaturaAprobadaId` cuando corresponda y malla capturada. No confundir el UUID de la aprobada de Solicitudes con el de Catálogo. Conservar resultados históricos, reservas y cierre transaccional.
4. En análisis externo, la malla receptora proviene del Catálogo; si no existe programa/malla autorizados de origen externo, el dato será null, sin inventarlo ni abrir D2.
5. No cambiar porcentajes, umbrales ni veredicto del motor. Una malla no vigente por sí sola no inventa una equivalencia ni invalida evidencia revisada.

Esta ampliación es aditiva: los JSON históricos sin esos metadatos deben seguir deserializando y mostrarse como «versión no registrada». No ejecutar backfill que les atribuya una versión académica no guardada.

## Consumo acordado en informe y HU-22

- Informe: copiar los metadatos del análisis guardado, tanto parejas como grupos. No resolverlos de nuevo contra Catálogo al generar/consultar el informe.
- HU-22: usar `vigenteAlAnalizar` de la malla de origen si está disponible; `Carrera.Vigencia` no sirve como sustituto. Dato ausente significa versión no registrada/información pendiente, sin afirmar que sea no vigente. Conservar el carácter informativo de la alerta y las fuentes ya acordadas.
- UI/PDF: mostrar nombre/id de la versión, vigencia capturada y ausencia histórica explícita. No reabrir ni reescribir informes emitidos para añadirlo.

## Pruebas requeridas del contrato

1. Analizar origen/receptora con versiones conocidas, guardar y activar otras: al consultar el análisis se conserva la instantánea original.
2. Generar informe desde ese análisis: parejas y grupos reflejan las versiones guardadas, sin nuevas consultas que alteren su significado.
3. Deserializar análisis/informes anteriores sin campos nuevos: se conservan sus resultados y muestran ausencia de versión.
4. Origen de malla no vigente: HU-22 usa esa instantánea; carrera no vigente con malla vigente no se confunde con el caso anterior.
5. Repetir seguridad, cierre, generación de informe, PDF y regresión HU-21 después de acordar los contratos.

Las comprobaciones y los límites reales están en la evidencia CA3. No se atribuyó una versión a registros históricos ni se modificó el motor. Pablo y Edwin deben revisar el incremento y Shirley conserva la aceptación; una implementación probada no equivale a aceptación humana.

## Forma implementada de la instantánea

Las respuestas y los JSON guardados añaden `mallas` nullable. Contiene `origenes[]` y `receptoras[]`; cada entrada tiene `asignaturaAprobadaId` nullable, `asignaturaCatalogoId` nullable y `malla` nullable con `id`, `carreraId`, `nombre` y `vigenteAlAnalizar`. En Application, los tipos propios, unificados con develop el 23/11, son MallaParaAnalisis (entrada), InstantaneaMalla (instantánea), MallaAsignaturaAnalizada y MallasAnalisis.

El informe copia este objeto desde el análisis guardado. La alerta de HU-22 se calcula desde sus orígenes con `vigenteAlAnalizar: false`; la ausencia de versión nunca se convierte en false. Esta ampliación usa los JSON existentes y no requiere una migración nueva de Homologación.
