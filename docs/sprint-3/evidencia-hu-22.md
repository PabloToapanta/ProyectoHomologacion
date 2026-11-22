# HU-22 — Alertas de ruta alternativa

Issue: [#76](https://github.com/PabloToapanta/ProyectoHomologacion/issues/76). Rama: `feature/hu-22-ruta-alternativa`.

## Alcance implementado

El informe conserva el veredicto de cada pareja y añade advertencias informativas cuando puede determinar que no se alcanza un requisito configurable de HU-16:

- Muestra el mínimo y el valor alcanzado para asignaturas homologables, contado únicamente desde resultados completos del motor; las advertencias no sustituyen ni editan sus sugerencias.
- En solicitudes externas, muestra el mínimo y los períodos declarados cuando están por debajo; también advierte si hay cambios previos de carrera o faltan datos declarados.
- Recomienda revisar el proceso vigente de admisión y cambio de carrera de la UTA, con referencias al RRA del CES, art. 79, e Instructivo UTA RES CAU-P-005-2023. La advertencia dice que la decisión corresponde a la institución, que es una regla de análisis del proyecto pendiente de confirmación y que no bloquea ni modifica el veredicto.
- Si faltan análisis o datos, se muestra «información incompleta» y no se presenta un conteo parcial como incumplimiento confirmado.

Los requisitos se comparan con los parámetros vigentes de HU-03. Los informes ya generados son inmutables y no se recalculan.

## Verificación automatizada

Ejecución en contenedor .NET SDK 8.0:

- `Homologacion.Application.Tests`: 137 superadas.
- `Homologacion.Api.Tests`: 139 superadas.
- Las pruebas focalizadas de informes pasaron durante la iteración; la suite completa de Application incluye los nuevos casos de HU-22.

## Pendiente / fuera de lo verificable en esta rama

- HU-07 (#68) aún no está integrada en `develop`; no existe versión histórica de malla. Por ello esta entrega no puede identificar una malla de origen no vigente ni mostrar su ruta de cambio de plan sin inferirla de la vigencia general de la carrera.
- Las solicitudes internas no registran períodos cursados; el informe señala que ese requisito de HU-16 no se puede verificar en el expediente actual. No se inventa un valor.
- Falta el recorrido manual integrado con Docker Compose y la aceptación de la Product Owner.
- Las fuentes citadas son referencias indicadas en las decisiones del ingeniero; no representan una confirmación institucional ni pronunciamiento oficial.
