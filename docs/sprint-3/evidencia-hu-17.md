# HU-17 · Regla de tercera matrícula (RN-07) — contrato y evidencia

Issue [#71](https://github.com/PabloToapanta/ProyectoHomologacion/issues/71). Rama `feature/hu-17-tercera-matricula` desde `develop` (`4b1162c`). Responsables: Edwin y Shirley; revisión cruzada de otro integrante; aceptación de la PO.

## Criterio aplicado

Se aplica la decisión del 20/11/2026 registrada en `docs/sprint-3/decisiones-validacion-ingeniero.md` (rama `feature/planificacion-sprint-3`, PR #81). Se basa en la lectura del ingeniero del RRA del CES (art. 74) y del instructivo UTA RES CAU-P-005-2023. Es una **regla de análisis del proyecto, pendiente de confirmación institucional**; no es un pronunciamiento oficial.

- La asignatura de origen aprobada en **tercera matrícula queda excluida** de la homologación: ninguna pareja que la use produce «Homologable».
- **La solicitud no se rechaza.** Las demás asignaturas se evalúan normalmente.
- Aplica al cambio interno y al externo.
- Si Solicitudes no informa el intento de matrícula, se registra «información incompleta». No se asume que es primera matrícula.

## Cambios de contrato

| Servicio | Cambio |
| --- | --- |
| Solicitudes | `GET /api/solicitudes/{id}/contexto-analisis-interno` y `…/contexto-analisis-externo` añaden `intentoMatricula` (1–3) a cada asignatura aprobada. El número de matrícula sigue sin enviarse. |
| Homologación (contrato de lectura) | `AprobadaParaAnalisis` y `AprobadaExternaParaAnalisis` aceptan `IntentoMatricula` opcional; null significa «no informado». |
| Homologación (análisis) | Con intento 3, el análisis interno o externo devuelve `estado: "Completo"`, `evaluacion.estado: NoHomologable`, la sugerencia «No homologable» y la justificación con el motivo RN-07. **No invoca al agente.** Se conservan los porcentajes del motor como información. |
| Homologación (revisión docente) | Para una asignatura excluida, una decisión «Homologable» se rechaza con 400 y el motivo RN-07, aunque provenga de un análisis anterior a la regla. «No homologable» se registra normalmente. |
| Homologación (informe HU-23) | `ParejaEnInforme` añade `exclusion` (texto o null). Una pareja excluida queda en «NoConvalidar», con el motivo y una advertencia general. Los informes ya generados no cambian. |
| Frontend | Detalle de la solicitud: «Tercera · excluida (RN-07)». Informe: el motivo aparece junto a la sugerencia final. |

La regla pura está en `Homologacion.Domain/TerceraMatricula/ReglaTerceraMatricula.cs`. Su aplicación sin agente está en `Homologacion.Application/TerceraMatricula/ExclusionTerceraMatricula.cs`.

## Criterios de aceptación y su prueba

| Criterio (#71, 20/11) | Prueba | Estado |
| --- | --- | --- |
| Intento 3 → excluida; 1 y 2 no aplican; fuera de rango es inválido; sin dato es desconocido | `TerceraMatriculaTests` (dominio) | Verificado |
| Interno: excluida sin agente; segunda matrícula se evalúa; sin intento, incompleto | `AnalizarSolicitudInternaTests.Tercera_matricula_*`, `Segunda_matricula_*`, `Sin_intento_*` | Verificado |
| Externo: excluida; la otra asignatura de la misma solicitud no se afecta | `AnalizarSolicitudExternaTests.Tercera_matricula_*`, `Sin_intento_*` | Verificado |
| El docente no puede registrar «Homologable» para la asignatura excluida | `RegistrarRevisionDocenteTests.Tercera_matricula_no_admite_decidir_homologable` | Verificado |
| Informe: «No convalidar» con motivo; las demás parejas se mantienen | `InformeHomologacionTests.Tercera_matricula_*` | Verificado |
| Contrato HTTP de Solicitudes → Homologación | `ContextoAnalisisInternoTests`, `ContextoAnalisisExternoTests`, `FuentesAnalisisHttpTests`, `AnalisisExternoInfraestructuraTests` | Verificado |
| Interfaz | `SolicitudesPage.test.tsx` y `InformePanel.test.tsx` (HU-17) | Verificado |
| Evidencia: intento respaldado por historial o certificado adjunto (HU-12) | — | **Pendiente**: HU-12 está en la PR #82, sin integrar en `develop`. Se añade al integrar HU-12, en coordinación con Shirley. |
| Ejecución integrada con Docker Compose | — | **Pendiente** |

## Ejecución registrada (20/11/2026)

- .NET 8 (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`):

  | Suite | Pasan | Omitidas | Fallan |
  | --- | --- | --- | --- |
  | Homologacion.Domain.Tests | 79 | 0 | 0 |
  | Homologacion.Application.Tests | 116 | 0 | 0 |
  | Homologacion.Infrastructure.Tests | 50 | 8 | 0 |
  | Homologacion.Api.Tests | 133 | 0 | 0 |
  | Solicitudes.Tests | 141 | 5 | 0 |

  Las 8 omitidas de infraestructura son las del modelo Ollama real.
- Frontend (Vitest): 143/143 pruebas y `npm run build` correcto.
- TDD: las pruebas nuevas se ejecutaron primero y fallaron (no compilaba `ReglaTerceraMatricula`; las 2 de interfaz fallaron). Después se implementó.
- En esta máquina, las pruebas de API con el runtime local .NET 10 fallan por `PipeWriter.UnflushedBytes`, igual que en `develop`. Es un problema del entorno; la referencia es .NET 8.

## Pendiente

- Prueba manual en el stack integrado:
  1. Registrar una asignatura con intento «Tercera».
  2. Analizarla.
  3. Intentar decidir «Homologable».
  4. Generar el informe.
- Vincular el intento con el historial adjunto cuando HU-12 esté en `develop`.
- Confirmación institucional de la regla, como indica el documento de decisiones.
