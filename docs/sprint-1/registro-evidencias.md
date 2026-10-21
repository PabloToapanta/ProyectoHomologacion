# Registro de evidencias del Sprint 1

**Estado inicial:** ninguna historia marcada como terminada. Completar únicamente con ejecuciones y comprobaciones reales.

## Bitácora diaria (Scrum)

| Fecha | Realizado | Siguiente actividad | Impedimentos | Responsable |
|---|---|---|---|---|
| 19/10/2026 | Pendiente de completar | | | |
| 20/10/2026 | Pendiente de completar | | | |
| 21/10/2026 | Pendiente de completar | | | |

## Evidencia por historia de usuario

| Historia | PR | Resultado pruebas | Evidencia API/UI | PO acepta | Observaciones |
|---|---|---|---|---|---|
| HU-01 | | Pendiente | | No evaluado | |
| HU-04 | [PR #19](https://github.com/PabloToapanta/ProyectoHomologacion/pull/19) y [PR #22](https://github.com/PabloToapanta/ProyectoHomologacion/pull/22), aprobados y fusionados; SC-01 en rama local | SC-01: 71 catálogo, 85 frontend, HTTP/SQL Server real y 2 E2E correctos | [T4/T5](evidencia-hu-04-t4-t5.md), [integración](evidencia-hu-05-integracion.md) y [SC-01 FISEI](evidencia-hu-04-fisei.md) | Alcance SC-01 acordado según Leslie; aceptación funcional pendiente | Facultad FISEI fija; se conservan los registros anteriores. Ajuste SC-01 pendiente de revisión e integración |
| HU-05 | [PR #20](https://github.com/PabloToapanta/ProyectoHomologacion/pull/20) y [PR #22](https://github.com/PabloToapanta/ProyectoHomologacion/pull/22), aprobados por Pablo y fusionados | Original: 62 catálogo, 84 frontend y 6 demo; seguimiento: 61 Gateway, HTTP/SQL Server y 2 E2E; regresión SC-01: 71 catálogo, 85 frontend y 2 E2E correctos | [T6/T7/T8](evidencia-hu-05-t6-t7-t8.md), [integración](evidencia-hu-05-integracion.md) y [SC-01](evidencia-hu-04-fisei.md) | Pendiente de Shirley | PR #22 integrado; T8 compatible con FISEI. Issue #3 sigue abierto hasta aceptación |
| HU-06 | | Pendiente | | No evaluado | |
| HU-14 | | Pendiente | | No evaluado | |
| HU-15 | | Pendiente | | No evaluado | |
| HU-16 | | Pendiente | | No evaluado | |

## Integración local de Pablo — 20/10/2026

Composición HU-15 corregida con TDD (8 fallos HTTP 500 antes, 9 pruebas nuevas
correctas después) y HU-16 (10 fallos antes, 11 pruebas nuevas correctas después).
Compose integrado y demo conservando datos. Resultado final:
374 pruebas .NET, 85 frontend y 2 E2E de autenticación aprobadas, más smoke HTTP
real por Gateway para HU-01/04/05/14/15/16. Ver [informe y salidas](evidencia-integracion-pablo.md).
Evidencia técnica local; no marca aceptación de la PO ni cierre del Sprint.

## Verificación final disponible — 21/10/2026

HU-06 y pantallas integradas en develop `34bcc67`. Corrección del smoke con TDD:
3 fallos previos y 7 pruebas Python finales aprobadas. Verificación local:
380 pruebas .NET, 89 frontend, 2 E2E de Auth y 2 comprobaciones adicionales de
pantallas/unidades aprobadas; smoke por Gateway con 4 unidades registradas.
Demo actualizada con respaldo SQL y datos anteriores conservados. Ver el
[informe](evidencia-verificacion-final.md) y el [recorrido para Shirley](preparacion-aceptacion-sprint-1.md).
Pendientes de publicación/revisión y aceptación humana; las casillas de la PO
no se completan automáticamente.

## Evidencia XP

- **TDD:** enlace a pruebas previas al cambio y evidencia de fallo previo/corrección.
- **Revisión cruzada:** PR y comentario de un compañero.
- **Integración continua:** enlace al resultado de GitHub Actions cuando se configure.
- **Pequeños lanzamientos:** versión `v0.1.0`, cuando exista y esté aprobada.
- **Refactorización:** cambio concreto apoyado por pruebas en verde.
- **Programación en parejas:** fecha, participantes y cambio realizado, si efectivamente ocurrió.

## Sprint Review — 21/10/2026

- **Asistentes:**
- **Historias mostradas:**
- **Historias aceptadas:**
- **Historias no aceptadas y motivo:**
- **Comentarios del docente/stakeholder:**
- **Siguientes pasos:**

## Retrospectiva

- **Qué funcionó:**
- **Qué no funcionó:**
- **Qué mejorar:**
- **Acción concreta para el Sprint 2 y responsable:**

**No llenar casillas con resultados hipotéticos.**
