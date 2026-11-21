# feat(homologacion): generar informe de homologación HU-23

Refs #61. Base: `develop`, que ya integra HU-19 (#56), HU-20 (#57), HU-08 (#58) y HU-31 (#60).

## Comportamiento

Un docente asignado genera un informe fijo por solicitud, con el análisis automático,
las revisiones de los dos docentes y sugerencia por pareja: `Convalidar`,
`NoConvalidar` o `SinAcuerdo`. Las parejas pendientes impiden generar; las asignaturas
sin revisión se listan sin sugerencia. Nueva tabla, migración, GET/POST y pantalla
docente. No incluye PDF, remisión ni resolución oficial.

## Cierre atómico (CA4)

La generación del informe y el guardado de un análisis docente comparten una
transacción con `sp_getapplock` exclusivo por solicitud. Una corrección en curso
entra en el informe; una que llega durante la generación recibe 409. Pruebas de
intercalado en ambos órdenes y prueba de bloqueo con SQL Server real.

## Otros cambios

- Fusión de `develop` sin conflictos.
- La prueba de OpenAPI del Gateway pasa a 33 rutas. En `develop` fallaba porque esperaba 29 y ya había 32.

## Decisiones y revisión

Adelanto de Sprint 3 autorizado por Shirley, según Edwin. La regla de discrepancias
y el cierre siguen sujetos a consulta con el docente de la asignatura; si cambian,
se ajustan contrato, pruebas e implementación. Pendiente: revisión cruzada, prueba en
navegador y aceptación de la PO.

## Evidencia

`docs/sprint-3/evidencia-hu-23.md`: Homologación 71/109/133, Infrastructure con SQL
Server real 56/56 (×3), Gateway 124, frontend 141 y build, smoke 47/47.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
