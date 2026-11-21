# HU-19 — análisis asistido de cambio externo / Issue #43

Responsables: Edwin Caraguay y Shirley Amaguaña. Rama `feature/hu-19-analisis-externo`
desde `develop` `cbe9ac1`. Fecha: 04/11/2026.

## Decisiones aplicadas

El contrato sigue la [propuesta original de HU-19](propuesta-contrato-hu-19.md),
incluida en esta rama para que el PR pueda revisarse sin depender de la rama de HU-20.
Edwin informó el 04/11/2026 que Shirley aprobó D1–D4 y que Leslie está de acuerdo con los
cambios en Homologación. Esa aprobación no consta todavía por escrito en el Issue #43: debe
registrarse allí.

| # | Decisión | Implementación |
|---|---|---|
| D1 | SC-01: origen externo = universidad ecuatoriana; sin apostilla ni institución extranjera | No hay campo de tipo de institución ni de apostilla; el resultado no las menciona. |
| D2 | Sin contenidos externos, criterio académico = información incompleta, sin agente | `AnalizarSolicitudExterna` no depende del agente; estado siempre `Incompleto`, `resultado` null. |
| D3 | Horas y créditos solo con unidad y sistema idénticos (sin espacios ni mayúsculas) | Si coinciden, el motor determinista calcula el porcentaje acotado a 100 %; si no, «sin conversión aprobada». |
| D4 | `periodosCursados` y `cambiosPreviosCarrera` declarados por el Coordinador | Campos opcionales en la solicitud externa; null = sin dato, nunca «cumple» ni «incumple». |

Precisiones de implementación respecto a la propuesta:

- Los datos de D4 se corrigen mientras la solicitud está «Registrada», es decir, antes de asignar
  docentes. Solo los docentes asignados pueden analizar, así que equivale a «mientras no tenga análisis».
- Se añadió `GET .../analisis-externo/opciones`, simétrico a HU-18, para que la vista ofrezca solo
  receptoras de la carrera de la solicitud.
- El resultado por pareja reutiliza el contrato de respuesta de HU-18 (`AnalisisInternoResponse`).
- La persistencia comparte con HU-20 la política de «último intento por pareja» mediante un almacén
  común; la tabla `AnalisisInternos` no cambia.

## Cambios

- **Solicitudes (Edwin)**
  - `Solicitud.PeriodosCursados` y `Solicitud.CambiosPreviosCarrera`: entre 0 y 100 como límite técnico de entrada, rechazados en solicitudes internas.
  - Migración `20261104062923_DatosCambioExternoHu19`: añade dos columnas `int` anulables.
  - `PUT /api/solicitudes/{id}/datos-cambio-externo` (Coordinador): 400 en internas y 409 tras asignar docentes.
  - `GET /api/solicitudes/{id}/contexto-analisis-externo` (Docente asignado, clave interna HU-36): sin Quipux, identificación, nombre ni matrícula. No se publica en el Gateway.
- **Homologación (módulo de Leslie, acordado)**
  - `Application/AnalisisExterno`: `AnalizarSolicitudExterna` y `GestionarAnalisisExterno`. RN-05 se calcula con `ValidadorRequisitosCambio` de HU-16; los datos ausentes reciben valores neutros, de modo que solo un dato declarado bajo el mínimo produce incumplimiento.
  - Rutas `POST/GET .../analisis-externo`, `GET .../analisis-externo/opciones` y `GET .../requisitos-externos` (Docente).
  - Tabla `AnalisisExternos` con la migración `20261104000200_AnalisisExternosHu19`; 503 sin confirmar el guardado si SQL falla.
- **Gateway**: cuatro rutas nuevas con `AcademicAccess`. La prueba de OpenAPI pasa de 24 a 28 rutas.
- **Frontend**
  - `AnalisisInternoPanel` recibe `tipo` («Interno» o «Externo») y, en externo, muestra RN-05.
  - La vista docente elige el panel según `tipoCambio`.
  - El Coordinador registra y corrige los datos de D4 (`DatosCambioExterno`).
- **Smoke**: `tests/security/hu19-smoke.py`.

## Criterios de #43

| Criterio | Evidencia | Estado |
|---|---|---|
| CA1 — adaptador común y tipo de institución | Reutiliza fuentes HTTP y almacenamiento de HU-18/HU-20, sin agente por D2 y limitado a Ecuador por D1 | Ajustado por D1/D2; pendiente reflejarlo en #43 |
| CA2 — mínimos externos (≥ 2 períodos, ≥ 2 homologables, sin cambios previos) | Pruebas de `GestionarAnalisisExterno` (Cumple, NoCumple por cada dato, Incompleto); datos ausentes no se presumen | Verificado según D4; «Cumple» solo con resultados completos sintéticos |
| CA3 — apostilla/institución extranjera | Fuera de alcance por D1; prueba y smoke «sin apostilla» | Ajustado por D1 |
| CA4 — horas y créditos sin conversiones inventadas | Pruebas de unidades iguales, normalizadas, distintas y ausentes; tope de 100 %; smoke D3 | Verificado |
| CA5 — persistencia y acceso por asignación | SQLite y SQL Server real; smoke de reinicio, sin duplicados, 404 para ajeno y fallo controlado mediante trigger | Verificado |

El criterio académico de un cambio externo queda siempre como información incompleta mientras
no exista la historia de Sprint 3 que registre contenidos del programa externo (D2). Por eso, hoy
ningún análisis externo es `Completo` y RN-05 externo no puede resultar «Cumple» con datos reales.

## Ejecuciones del 04/11/2026

Entorno: Windows con Docker, SDK .NET 8 en contenedor y Node local. TDD: cada bloque se ejecutó
primero en rojo (fallos de compilación o de pruebas) y después en verde.

1. Solicitudes.sln con SQL Server 2022 temporal: 142/142.
2. Homologacion.sln sin SQL: todas aprobadas; Application 84/84.
3. Homologacion.Infrastructure con SQL Server real: 43 aprobadas y 1 omitida (`OllamaRealTests`).
4. Gateway.Tests: 117/117.
5. Frontend: `vitest run` 128/128 y `npm run build` (incluye `tsc --noEmit`).
6. `python tests/security/hu19-smoke.py` sobre una copia limpia de la rama: 46/46. Proyecto
   Docker aleatorio con volúmenes eliminados. HTTP local aislado: no acredita HTTPS.
7. Regresión `python tests/security/hu18-smoke.py` sobre la misma copia: 49/49 (análisis interno,
   persistencia de HU-20 y fallo controlado sin cambios).

**No verificado:** CI en GitHub, revisión cruzada de Leslie sobre Homologación, aceptación de
Shirley y registro escrito de las decisiones D1–D4 y del bloqueo desde la asignación en el Issue #43.

## Revisión para PR del 04/11/2026

Ejecución independiente sobre el código de `3e200d8`, con SDK .NET 8 y SQL Server
2022 en contenedores temporales: 655 pruebas backend aprobadas y 1 omitida (Ollama
real). Desglose: dominio 71, aplicación 84 y API 121 de Homologación; infraestructura
43; Auth Application 13 y AuthService 64; Gateway 117; Solicitudes 142.
Frontend: 128/128; TypeScript y compilación Vite aprobados. Sin errores en
`git diff --check`. Contenedores y red de esta revisión eliminados.

El primer intento de esta revisión utilizó una ruta inexistente de la solución y
no ejecutó pruebas; se corrigió para usar `Homologacion.sln` en la raíz y se volvió
a ejecutar. No se repitieron los smokes: sus resultados anteriores se conservan
como evidencia registrada, no como nuevas ejecuciones de esta revisión.

**Hallazgo (corregido):** el bloqueo de corrección después de asignar docentes se
validaba en memoria antes de `SaveChanges`, sin condición de estado en la actualización.
Una edición que leyera «Registrada» podía guardarse tras una asignación concurrente.

## Corrección de concurrencia del 04/11/2026

- `Solicitud.Estado` es token de concurrencia de EF: toda actualización de la solicitud
  incluye `WHERE Estado = <estado leído>`. Sin cambio de esquema: la migración generada
  quedó vacía y solo se actualizó el snapshot.
- `GuardarCambios` traduce el conflicto a `DatosExternosNoEditablesException` (409);
  `GuardarAsignacion` lo traduce a `AsignacionNoPermitidaException` (409), nunca a 500.
- Shirley confirmó, según Edwin el 04/11/2026, que la edición se bloquea desde la
  asignación de docentes, aunque todavía no exista análisis.
- TDD: `ConcurrenciaDatosExternosTests` falló primero en SQLite y SQL Server real
  (la edición tardía se guardaba) y pasó tras la corrección:
  - intercalado determinista (lectura «Registrada» → asignación confirmada → guardado) en
    SQLite y SQL Server real: 409 y períodos sin cambios;
  - edición confirmada antes de asignar se conserva;
  - 10 rondas de edición y asignación en paralelo con SQL Server real, repetidas 5 veces:
    la asignación nunca falla y la edición tardía solo recibe el conflicto controlado.
- Ejecuciones: Solicitudes.sln con SQL Server 2022 temporal 146/146 (incluye la asignación
  concurrente de HU-29); `hu19-smoke.py` sobre copia limpia 46/46. Contenedores eliminados.
