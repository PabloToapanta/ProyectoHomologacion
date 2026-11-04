# feat(homologacion): generar informe de homologación HU-23

**Preparación local; no publicar como lista mientras persista el bloqueo de CA4.**
Refs #61. Base prevista: `develop`. Depende de #56 (HU-19) y #60 (HU-31), todavía
sin integrar al revisar esta rama. El incremento propio empieza sobre `cd034ca`.

## Comportamiento

Un docente asignado genera un informe fijo por solicitud, con el análisis automático,
las revisiones de los dos docentes y sugerencia por pareja: `Convalidar`,
`NoConvalidar` o `SinAcuerdo`. Las parejas pendientes impiden generar; las asignaturas
sin revisión se listan sin sugerencia. Nueva tabla, migración, GET/POST y pantalla
docente. No incluye PDF, remisión ni resolución oficial.

## Bloqueo técnico

El control de cierre de revisiones es una lectura previa separada del guardado.
Una corrección iniciada antes de generar el informe puede guardarse después.
Hace falta protección atómica compartida y una prueba determinista de ese orden;
el índice único del informe solo resuelve generaciones simultáneas.

## Decisiones y revisión

El contrato local recoge el adelanto de Sprint 3 autorizado por Shirley, según Edwin.
La regla de discrepancias y el cierre siguen sujetos a consulta con su docente de la
asignatura. Registrar el acuerdo en #61 antes de aceptar. Requiere revisión cruzada,
CI, navegador y aceptación de la PO. Sin merge ni cierre automático.

## Evidencia

`docs/sprint-3/evidencia-hu-23.md`. Los resultados de pruebas existentes no acreditan
el caso concurrente pendiente. Se excluyen los puertos locales de Docker,
SeleccionCatalogo.tsx, temporales y herramientas ajenas.
