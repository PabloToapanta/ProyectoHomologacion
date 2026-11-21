# HU-33 · Revisión del informe y finalización por el coordinador — contrato y evidencia

Issue [#64](https://github.com/PabloToapanta/ProyectoHomologacion/issues/64). Rama `feature/hu-33-finalizar-solicitud`, creada sobre `feature/hu-32-remitir-informe` (PR #86) porque solo se finalizan solicitudes con el informe remitido. Responsables: Edwin y Shirley. Revisión cruzada: Leslie (Homologación) y Pablo (Gateway y contrato de estados).

## Reglas aplicadas

Decisiones del 20/11/2026 (respuestas del ingeniero). Son una lectura de fuentes públicas que no sustituye el pronunciamiento institucional; se implementan como reglas del flujo del proyecto.

- **«Sin acuerdo»:** el coordinador registra una decisión académica propia y motivada: resultado (Convalidar o NoConvalidar), justificación, autor y fecha. No elige automáticamente entre los docentes. Si la decisión corresponde a otro órgano, la justificación puede indicarlo como propuesta.
- **Finalizar** bloquea la edición ordinaria de la solicitud, los análisis docentes y el informe.
- **Fuera del Sprint 3:** devolver el informe a los docentes y reabrir una solicitud finalizada.
- **Acceso:** cualquier Coordinador revisa las solicitudes FISEI (confirmación de la PO del 15/11/2026, la misma de HU-24).
- **Pendiente de confirmar por Coordinación:** la atribución del coordinador para decidir las parejas «Sin acuerdo».

## Contrato de estados con Solicitudes (amplía el de HU-32; para revisión de Pablo)

| Tema | Decisión propuesta |
| --- | --- |
| Transición | «Remitida» → «Finalizada». «Finalizada» no vuelve a «Remitida». |
| Quién la pide | Solo Homologación, al finalizar, con `POST /api/solicitudes/{id}/finalizacion`. Usa el JWT del coordinador (reenviado) y la clave interna del servidor (HU-36). |
| Publicación | La ruta interna **no** se publica en el Gateway. Prueba: `HU33_la_finalizacion_interna_de_Solicitudes_no_se_publica`. |
| Respuestas | Rol Coordinador. 204 si cambia o si ya estaba «Finalizada» (idempotente). 409 desde otro estado. 404 si no existe. `Estado` sigue siendo token de concurrencia. |
| Consistencia | Homologación inserta el evento «Finalizado» y llama a Solicitudes dentro del cierre transaccional del informe (`ConCierreAsync`, `sp_getapplock`). Si Solicitudes falla, no queda finalización; el reintento converge porque la marca es idempotente. |
| Qué queda bloqueado | En Solicitudes, registrar asignaturas aprobadas responde 409. Asignar docentes y corregir los datos de RN-05 ya solo se admiten en «Registrada». En Homologación, los análisis docentes están cerrados desde el informe (HU-23), y las decisiones y la finalización responden 409. Los adjuntos de HU-12 (PR #82) ya responden 409 con el estado «Finalizada». |
| No bloqueado | El análisis del agente (HU-18/HU-19) no consulta el estado: un análisis nuevo no cambia el informe fijo ni la decisión. Bloquearlo exige tocar el código de HU-18 y HU-19; se propone a Pablo y Leslie. |

## API pública

Todas solo para el rol **Coordinador** (política `AcademicAccess` en el Gateway; Homologación exige el rol).

| Método y ruta | Respuestas |
| --- | --- |
| `GET /api/homologaciones/solicitudes/{id}/informe/revision` | 200 `{ informe, remitidoEl, remitidoPor, decisiones, decisionesPendientes, finalizacion }`; 404 si no hay informe remitido |
| `PUT /api/homologaciones/solicitudes/{id}/informe/decisiones` | 200 con la decisión `{ resultado, justificacion, coordinador, fecha, … }`. 400 si: no hay informe remitido, la pareja no está en el informe o no es «Sin acuerdo», el resultado no es válido o falta la justificación (máx. 2000). 409 si ya está finalizada |
| `POST /api/homologaciones/solicitudes/{id}/informe/finalizacion` | 200 `{ solicitudId, fecha, finalizadoPorId, finalizadoPor }`. 400 sin informe remitido o con decisiones pendientes. 409 si ya está finalizada o el estado no lo permite. 503 si Solicitudes o la base no responden |

Sin sesión: 401. Docente o Administrador: 403. La decisión se puede corregir hasta finalizar; queda la última, con su autor y fecha. La finalización queda en el historial de HU-25 como «Finalizado», con rol Coordinador.

## Interfaz

- **Coordinador:** en el detalle de una solicitud «Remitida» o «Finalizada» aparece «Revisión del informe y finalización»:
  - una tabla por pareja con la asignatura, la receptora, el agente, los porcentajes, ambos docentes, la sugerencia final y la decisión del coordinador;
  - un formulario por pareja «Sin acuerdo» (Decisión y Justificación);
  - «Finalizar solicitud», desactivado mientras falte una decisión.

  Al finalizar se muestra quién finalizó y cuándo. El estado cambia a «Finalizada» en el detalle y en la lista, y el historial se vuelve a consultar.
- **Docente:** con la solicitud «Finalizada», el panel del informe indica «Solicitud finalizada por el coordinador de carrera» y no ofrece remitir.

## Criterios de aceptación y su prueba

| Criterio | Prueba | Estado |
| --- | --- | --- |
| CA1. El coordinador ve el informe remitido con las parejas, el agente y ambos docentes | `FinalizarInformeTests.CA1_*`; `InformeEndpointTests.HU33_el_coordinador_revisa_*`; `RevisionInformeCoordinador.test.tsx` (CA1) | Verificado |
| CA2. Registra su decisión en «Sin acuerdo»; no finaliza mientras falte | `DecisionCoordinadorTests`; `FinalizarInformeTests.CA2_*`; `InformeEndpointTests.HU33_*`; `RevisionInformeCoordinador.test.tsx` | Verificado |
| CA3. «Finalizada» con fecha y coordinador; después 409 | `FinalizarInformeTests.CA3_*`; `FinalizacionSolicitudTests` (estado, idempotencia y asignaturas 409); `InformeEndpointTests.HU33_*` (historial «Finalizado»); `SolicitudesPage.test.tsx` y `InformePanel.test.tsx` (HU-33) | Verificado |
| CA4. Sin informe remitido no se finaliza (400) | `FinalizarInformeTests.CA4_*`; `InformeEndpointTests.HU33_*` | Verificado |
| CA5. Solo el rol Coordinador: 401 y 403 | `InformeEndpointTests.HU33_solo_el_coordinador_*`; `FinalizacionSolicitudTests.Solo_el_coordinador_*` | Verificado |
| Sin estado confirmado no queda finalización | `FinalizarInformeTests.Si_Solicitudes_no_confirma_*`; `DecisionesCoordinadorSqlTests.Si_falla_la_accion_*` (transacción real en SQLite) | Verificado |
| Persistencia, migración y snapshot | `DecisionesCoordinadorSqlTests` (SQLite y SQL Server real) | Verificado |
| Cliente HTTP de la finalización | `FinalizacionSolicitudHttpTests` | Verificado |
| Rutas en el Gateway; la interna no se publica | `AnalisisExternoRouteTests` (3 casos y `HU33_*`); `GatewayHttpTests.OpenApi_*` (38 rutas) | Verificado |

## Ejecución registrada (21/11/2026)

- **TDD:** las pruebas nuevas de .NET se ejecutaron primero y fallaron, porque no compilaban o, en el Gateway, por 4 fallos. Después se implementó.
  - En el frontend, la prueba de `SolicitudesPage` y la de `InformePanel` (solicitud finalizada) fallaron antes de implementar.
  - Las de `RevisionInformeCoordinador.test.tsx` se escribieron antes que el componente, pero no se ejecutaron en rojo.
- **.NET 8** (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`):

  | Suite | Pasan | Omitidas | Fallan |
  | --- | --- | --- | --- |
  | Homologacion.Domain.Tests | 90 | 0 | 0 |
  | Homologacion.Application.Tests | 130 | 0 | 0 |
  | Homologacion.Infrastructure.Tests | 73 | 10 | 0 |
  | Homologacion.Api.Tests | 147 | 0 | 0 |
  | Gateway.Tests | 131 | 0 | 0 |
  | Solicitudes.Tests | 160 | 5 | 0 |

- **SQL Server 2022 real**, temporal y aislado, con una contraseña aleatoria no guardada y eliminado después:
  - `Homologacion.Infrastructure.Tests`: 82 superadas y 1 omitida (necesita el modelo de Ollama);
  - `Solicitudes.Tests`: 165/165.
- **Frontend (Vitest, `--maxWorkers=2`):** 158/158 y `npm run build` correcto.
- **Integración en Docker Compose** (`tests/security/hu33-smoke.py`): 70/70 comprobaciones superadas.
  - **Entorno:** el stack corrió aislado (Gateway, JWE, Auth, Catálogo, Solicitudes, Homologación y SQL Server) sobre una copia del árbol de trabajo, con los `compose.*.yaml` del commit, credenciales aleatorias por ejecución y usuarios y datos sintéticos. Al terminar se eliminó el proyecto, sin contenedores ni volúmenes restantes.
  - **Comprobado**, además del flujo de HU-32:
    - sin informe remitido, la revisión da 404 y la finalización 400;
    - sin sesión 401; el docente recibe 403 al revisar, decidir y finalizar;
    - la ruta interna de finalización de Solicitudes no se publica (404 en el Gateway);
    - la revisión muestra las 2 parejas con el agente y ambos docentes, y 1 decisión pendiente;
    - con la decisión pendiente, finalizar da 400;
    - una decisión sin justificación, o en una pareja con acuerdo, da 400;
    - la decisión queda con el resultado, el coordinador y la fecha;
    - la finalización queda con el coordinador y la fecha; un segundo intento da 409;
    - tras finalizar dan 409: una nueva decisión, registrar una asignatura y un análisis docente;
    - «Finalizada» aparece para el coordinador y para el docente;
    - el historial queda «Generado» (doc1), «Remitido» (doc2) y «Finalizado» (coord, Coordinador);
    - la revisión no expone datos personales.

  Transporte HTTP local: esta prueba no acredita HTTPS.
- **No ejecutado:** la revisión visual en el navegador.

## Pendiente

- Revisión del contrato de estados y del Gateway por Pablo; revisión cruzada de Leslie.
- Propuesta a Pablo y Leslie: bloquear el análisis del agente en solicitudes finalizadas.
- Confirmación de Coordinación sobre su atribución en «Sin acuerdo».
- Aceptación de la PO.
