# HU-07 — Contrato para revisar con Pablo y Edwin

Relacionado con [Issue #68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68), [spec](hu-07-versiones-malla.md) y [evidencia T58/T59](evidencia-hu-07-t58-t59.md). El acuerdo técnico permite continuar Catálogo/T59. Este documento concreta la ampliación de Homologación e informe que debe revisarse conjuntamente antes de integrar; **no está implementada en esos módulos**.

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

## Ampliación propuesta para Pablo

1. En `Homologacion.Application/AnalisisInterno/ContratoSolicitud.cs`, añadir propiedad init nullable de malla a `AsignaturaParaAnalisis`, con un DTO propio de Homologación; no referenciar el proyecto de Catálogo ni cambiar constructores posicionales históricos. `FuentesAnalisisHttp` podrá deserializarla con sus opciones Web actuales.
2. Al construir cada análisis nuevo, capturar `id`, `carreraId`, `nombre` y **vigenteAlAnalizar** de origen/receptora en metadatos del resultado; `AnalisisGuardado` conservará la instantánea mediante su JSON existente. No consultar la vigencia actual al volver a leer el análisis.
3. En agrupaciones, guardar una lista de instantáneas por asignatura: `asignaturaCatalogoId`, `asignaturaAprobadaId` cuando corresponda y malla capturada. No confundir el UUID de la aprobada de Solicitudes con el de Catálogo. Conservar resultados históricos, reservas y cierre transaccional.
4. En análisis externo, la malla receptora proviene del Catálogo; si no existe programa/malla autorizados de origen externo, el dato será null, sin inventarlo ni abrir D2.
5. No cambiar porcentajes, umbrales ni veredicto del motor. Una malla no vigente por sí sola no inventa una equivalencia ni invalida evidencia revisada.

Esta ampliación es aditiva: los JSON históricos sin esos metadatos deben seguir deserializando y mostrarse como «versión no registrada». No ejecutar backfill que les atribuya una versión académica no guardada.

## Consumo propuesto para Edwin

- Informe: copiar los metadatos del análisis guardado, tanto parejas como grupos. No resolverlos de nuevo contra Catálogo al generar/consultar el informe.
- HU-22: usar `vigenteAlAnalizar` de la malla de origen si está disponible; `Carrera.Vigencia` no sirve como sustituto. Dato ausente significa versión no registrada/información pendiente, sin afirmar que sea no vigente. Conservar el carácter informativo de la alerta y las fuentes ya acordadas.
- UI/PDF: mostrar nombre/id de la versión, vigencia capturada y ausencia histórica explícita. No reabrir ni reescribir informes emitidos para añadirlo.

## Pruebas que faltan antes de cerrar CA3

1. Analizar origen/receptora con versiones conocidas, guardar y activar otras: al consultar el análisis se conserva la instantánea original.
2. Generar informe desde ese análisis: parejas y grupos reflejan las versiones guardadas, sin nuevas consultas que alteren su significado.
3. Deserializar análisis/informes anteriores sin campos nuevos: se conservan sus resultados y muestran ausencia de versión.
4. Origen de malla no vigente: HU-22 usa esa instantánea; carrera no vigente con malla vigente no se confunde con el caso anterior.
5. Repetir seguridad, cierre, generación de informe, PDF y regresión HU-21 después de acordar los contratos.

No se enviaron comentarios o mensajes externos en esta entrega. Pablo/Edwin pueden revisar este documento junto con los cambios de Catálogo antes de autorizar el incremento de sus módulos.
