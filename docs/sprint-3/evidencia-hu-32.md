# HU-32 · Remisión del informe al coordinador — contrato y evidencia

Issue [#63](https://github.com/PabloToapanta/ProyectoHomologacion/issues/63). Rama `feature/hu-32-remitir-informe`, creada sobre `feature/hu-25-auditoria-informes` (PR #85) porque registra el evento «Remitido» del historial de HU-25. Responsables: Edwin y Shirley. Revisión cruzada: Leslie (Homologación) y Pablo (Gateway y contrato de estados).

## Contrato de estados entre Homologación y Solicitudes (propuesta para revisión de Pablo)

La planificación pide acordar este contrato antes de T44. Se propone en esta PR y se ajusta si la revisión lo pide.

| Tema | Decisión propuesta |
| --- | --- |
| Dueño del estado | **Solicitudes**: `Solicitud.Estado`. Homologación no escribe en la base de Solicitudes. |
| Transición | «Docentes asignados» → «Remitida». Es la única que agrega HU-32; «Finalizada» corresponde a HU-33. |
| Quién la pide | Solo Homologación, al remitir el informe, con `POST /api/solicitudes/{id}/remision`. Usa el JWT del docente (reenviado) y la clave interna del servidor (HU-36), igual que las lecturas de contexto de HU-18. |
| Publicación | La ruta interna **no** se publica en el Gateway: el navegador no puede marcar «Remitida» sin pasar por las comprobaciones del informe. Prueba: `HU32_la_remision_interna_de_Solicitudes_no_se_publica`. |
| Autorización en Solicitudes | Rol Docente y docente asignado (claim `sub`). 404 si la solicitud no existe o no está asignada, sin distinguir ambos casos. |
| Respuestas | 204 si cambia o si ya estaba «Remitida» (idempotente, para admitir reintentos). 409 desde cualquier otro estado. `Estado` sigue siendo token de concurrencia: si dos peticiones cambian el estado a la vez, una recibe 409. |
| Consistencia | Homologación inserta el evento «Remitido» y llama a Solicitudes **dentro** del cierre transaccional del informe (`ConCierreAsync`, `sp_getapplock`). Si Solicitudes falla, la transacción no se confirma y no queda remisión. Si Solicitudes confirma y luego falla la confirmación local, el reintento converge porque marcar «Remitida» es idempotente. |
| Qué queda bloqueado | Los análisis docentes ya están cerrados desde la generación del informe (HU-23). Al remitir: no se puede remitir otra vez (409) y, como antes, los datos de RN-05 solo se corrigen en «Registrada». El bloqueo completo de la edición corresponde a «Finalizada» (HU-33). |
| Fuera de alcance | Devolver el informe a los docentes y reabrir (decisión del 20/11/2026). |

## Contrato de la API pública

`POST /api/homologaciones/solicitudes/{id}/informe/remision`, solo para el rol **Docente** asignado (Gateway: ruta `homologacion-informe-remision`, política `AcademicAccess`).

| Respuesta | Cuándo |
| --- | --- |
| 200 `{ solicitudId, fecha, remitidoPorId, remitidoPor }` | Remisión registrada |
| 400 | No hay informe generado, o una pareja no tiene el análisis de los dos docentes |
| 401 / 403 | Sin sesión / rol Coordinador o Administrador |
| 404 | Docente no asignado o solicitud inexistente |
| 409 | Ya remitido, o el estado de la solicitud no lo permite |
| 503 | Solicitudes o la base no responden; no queda remisión |

**Regla del flujo (20/11/2026, pendiente de confirmación institucional):** se remite solo cuando todas las parejas tienen el análisis de los dos docentes; las discrepancias quedan como «Sin acuerdo» para el coordinador. El informe no es una resolución oficial.

**Informes anteriores a HU-25:** no tienen evento «Generado». Se pueden remitir porque el informe guardado acredita la generación; no se inventa el evento que falta.

## Interfaz

- **Docente:** en el detalle con el informe generado aparece «Remitir al coordinador». Tras remitir, se muestra quién lo remitió y cuándo, y el estado «Remitida» se ve en el detalle y en la lista. Si la solicitud ya está «Remitida», no se ofrece el botón.
- **Coordinador:** ve «Remitida» en su lista y en el detalle. El historial del informe (HU-25) muestra el evento «Remitido» con fecha, usuario y rol.

## Criterios de aceptación y su prueba

| Criterio | Prueba | Estado |
| --- | --- | --- |
| CA1. Remitir registra la fecha y el docente | `RemitirInformeTests.CA1_*`; `InformeEndpointTests.HU32_el_docente_remite_*` (historial con «Generado» y «Remitido») | Verificado |
| CA2. Una sola vez; el segundo intento da 409 sin cambios | `RemitirInformeTests.CA2_*`; `InformeEndpointTests.HU32_el_docente_remite_*` | Verificado |
| CA3. Sin informe, 400 con mensaje claro | `RemitirInformeTests.CA3_*`; `InformeEndpointTests.HU32_el_docente_remite_*`; `InformePanel.test.tsx` (sin informe no hay botón) | Verificado |
| CA4. La solicitud pasa a «Remitida»; docentes y coordinador ven el estado | `RemisionSolicitudTests.El_docente_asignado_marca_*` (coordinador y el otro docente ven «Remitida»); `SolicitudesDocentePage.test.tsx` (HU-32) | Verificado con pruebas automáticas; falta la prueba manual en Compose |
| CA5. Solo docentes asignados; 401, 403 y 404; identidad del JWT | `RemitirInformeTests.CA5_*`; `InformeEndpointTests.HU32_solo_el_docente_remite` y `HU32_docente_no_asignado_recibe_404`; `RemisionSolicitudTests.Solo_el_rol_docente_*` y `Docente_ajeno_*` | Verificado |
| Remisión solo con los dos análisis por pareja | `RemitirInformeTests.Una_pareja_sin_el_analisis_*` | Verificado |
| Sin estado confirmado no queda remisión | `RemitirInformeTests.Si_Solicitudes_no_confirma_*` y `Si_el_estado_*`; `EventosInformeSqlTests.HU32_si_falla_la_accion_*` (transacción real en SQLite) | Verificado |
| Cliente HTTP: ruta, 404, 409 y fallos cerrados | `RemisionSolicitudHttpTests` | Verificado |
| Ruta pública en el Gateway; la interna no se publica | `AnalisisExternoRouteTests` y `GatewayHttpTests.OpenApi_*` (35 rutas) | Verificado |

## Ejecución registrada (20/11/2026)

- .NET 8 (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`):

  | Suite | Pasan | Omitidas | Fallan |
  | --- | --- | --- | --- |
  | Homologacion.Domain.Tests | 76 | 0 | 0 |
  | Homologacion.Application.Tests | 120 | 0 | 0 |
  | Homologacion.Infrastructure.Tests | 62 | 9 | 0 |
  | Homologacion.Api.Tests | 142 | 0 | 0 |
  | Gateway.Tests | 127 | 0 | 0 |
  | Solicitudes.Tests | 151 | 5 | 0 |

- **Frontend (Vitest):** 150/150 pruebas y `npm run build` correcto.
- **TDD:** las pruebas nuevas se ejecutaron primero y fallaron porque no compilaban (`MarcarRemitida`, `RemitirInformeHomologacion`, `IEstadoSolicitudes`) o porque faltaba el botón. Después se implementó.
- **No ejecutado:** la suite contra SQL Server real ni la prueba manual en Docker Compose.

## Pendiente

- Revisión del contrato de estados por Pablo y revisión cruzada de Leslie.
- Prueba manual en el stack integrado: generar el informe, remitirlo como docente y ver «Remitida» y el historial como coordinador.
- HU-33: «Finalizada», la decisión del coordinador en «Sin acuerdo» y el evento «Finalizado».
- Aceptación de la PO.
