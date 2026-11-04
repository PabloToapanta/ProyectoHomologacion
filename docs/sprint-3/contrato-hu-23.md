# Contrato HU-23 — informe de homologación

Historia del Sprint 0 (Sprint 3, prioridad Alta, 5 PH): «Como docente, quiero generar el informe con el
análisis del agente, el mío y la sugerencia final de convalidar o no.» Adelantada el 04/11/2026 con
autorización de Shirley, transmitida por Edwin. Base: `feature/hu-31-revision-docente` (issue #59, PR #60); se integra
después de HU-19 (#56) y HU-31.

Seguimiento: [Issue #61](https://github.com/PabloToapanta/ProyectoHomologacion/issues/61).

## Decisiones (Edwin, 04/11/2026)

1. **Cualquiera de los dos docentes asignados** genera el informe. Es una versión fija: no se regenera y,
   desde ese momento, los análisis docentes de la solicitud quedan cerrados (409 al intentar corregirlos).
2. **Con discrepancia**: si los docentes deciden distinto sobre una pareja, el informe se genera igual con
   la sugerencia final «Sin acuerdo» y ambas decisiones visibles. Decide el coordinador (HU-33).

**Sujeto a cambios:** Edwin consultará al docente de la asignatura la forma más adecuada de combinar las
decisiones de los dos docentes y de cerrar sus análisis. Si la indicación cambia las decisiones 1 y 2, se
ajustan este contrato y su implementación antes de aceptar la historia. HU-23 es la última historia
adelantada por ahora.

## Reglas

- **Parejas incluidas:** las que tienen al menos un análisis docente (HU-31). Cada una debe tener el análisis de
  **los dos** docentes asignados; si falta alguno, no se genera y se listan las parejas pendientes.
  La asignación de HU-29 es de exactamente dos docentes, y HU-31 solo acepta análisis de docentes asignados.
- **Sugerencia final por pareja:**
  - Homologable + Homologable → `Convalidar`.
  - NoHomologable + NoHomologable → `NoConvalidar`.
  - Decisiones distintas → `SinAcuerdo`.
- Se copia el último análisis del agente de cada pareja (estado, sugerencia y los tres porcentajes) y el de
  cada docente (postura, decisión, observación y fecha). El informe no cambia aunque después se modifique otro dato.
- Las asignaturas aprobadas sin ninguna pareja analizada aparecen como «sin análisis», sin sugerencia.
- Las receptoras se identifican con su código y nombre del catálogo, consultados al generar.
- No incluye remisión al coordinador (HU-32), finalización (HU-33) ni PDF (HU-24). No es una resolución oficial.

## API (Homologación, rol Docente asignado)

| Método | Ruta | Resultado |
|---|---|---|
| POST | `/api/homologaciones/solicitudes/{id}/informe` | 201 con el informe. 400 si no hay parejas o faltan análisis docentes; 404 si la solicitud no está asignada; 409 si ya existe; 503 si una fuente o la base no responde. |
| GET | `/api/homologaciones/solicitudes/{id}/informe` | 200 con el informe; 404 si no está asignada o no se ha generado. |

Persistencia: tabla `InformesHomologacion` (una fila por solicitud, contenido JSON, fecha y docente que lo generó)
en `HomologacionAnalisis`. La clave primaria impide que dos generaciones simultáneas creen dos informes.

## Criterios de aceptación propuestos

- **CA1.** Un docente asignado genera el informe cuando cada pareja analizada tiene el análisis de ambos docentes; incluye agente, docentes y sugerencia final.
- **CA2.** Las decisiones coincidentes dan «Convalidar» o «No convalidar»; las distintas dan «Sin acuerdo» con ambas decisiones.
- **CA3.** Sin parejas analizadas o con un análisis docente faltante no se genera y se indica qué falta.
- **CA4.** El informe es único y fijo: una segunda generación da 409 y, después de generarlo, los análisis docentes no se pueden corregir.
- **CA5.** Solo los docentes asignados generan o consultan; otros roles reciben 403 y un docente ajeno, 404.
- **CA6.** El informe se conserva tras reiniciar el servicio; un fallo de la base no confirma la generación.
