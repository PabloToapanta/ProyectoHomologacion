# HU-25 · Historial y auditoría de informes — contrato y evidencia

Issue [#74](https://github.com/PabloToapanta/ProyectoHomologacion/issues/74). Rama `feature/hu-25-auditoria-informes` desde `develop` (`4b1162c`). Responsables: Edwin y Shirley. Revisión cruzada: Leslie. Toca la configuración del Gateway, a cargo de Pablo.

## Alcance

Se registran los tres eventos del informe, cada uno una sola vez y después del anterior:

| Evento | Quién | Dónde se registra |
| --- | --- | --- |
| Generado | Docente | `GenerarInformeHomologacion`, en la misma transacción que el informe (HU-23) |
| Remitido | Docente | `RemitirInforme` (HU-32) |
| Finalizado | Coordinador | `FinalizarInforme` (HU-33) |

No se registran eventos de días pasados: no hay datos inventados. Los informes generados antes de esta versión no tienen evento «Generado», pero se pueden remitir igual (`RemitirInformeTests.Un_informe_anterior_a_HU25_sin_evento_generado_tambien_se_remite`).

*Actualización del 25/11/2026:* al escribir esta evidencia (20/11), HU-32 y HU-33 no existían y «Remitido» y «Finalizado» figuraban como pendientes. Ambas historias ya están en `develop` y registran sus eventos.

## Contrato

| Elemento | Detalle |
| --- | --- |
| Dominio | `Homologacion.Domain/Auditoria/HistorialInforme.cs`: `TipoEventoInforme` (Generado, Remitido, Finalizado) y `EventoInforme` inmutable (fecha, usuario, rol). Incluye la creación validada, el orden cronológico (a igual fecha, el del flujo) y la regla de secuencia. |
| Aplicación | `IAuditoriaInformes` solo ofrece `RegistrarAsync` y `ListarAsync`, sin modificar ni eliminar. `ConsultarHistorialInforme`. `GenerarInformeHomologacion` registra «Generado» dentro de `ConCierreAsync`: si el evento falla, el informe no se confirma. |
| Persistencia | Tabla `EventosInforme`, migración `20261120000100_EventosInformeHu25`, con índice único (`SolicitudId`, `Tipo`). En SQL Server, el trigger `TR_EventosInforme_SoloInsercion` impide UPDATE y DELETE. |
| API | `GET /api/homologaciones/solicitudes/{id}/informe/historial`, solo para el rol **Coordinador**. Devuelve una lista cronológica `{ id, tipo, fecha, usuarioId, usuario, rol }`; vacía si no hay informe. 401 sin sesión; 403 para Docente y Administrador. No existen PUT, PATCH ni DELETE (405). |
| Gateway | Ruta `homologacion-informe-historial` (GET) con la política `AcademicAccess`. La autorización por rol la aplica Homologación. |
| Frontend | El detalle de la solicitud del coordinador muestra «Historial del informe»: tipo, fecha, usuario y rol. Solo lectura. |

## Criterios de aceptación y su prueba

| Criterio | Prueba | Estado |
| --- | --- | --- |
| CA1. Eventos con fecha y usuario | `HistorialInformeTests` (dominio); `InformeHomologacionTests.HU25_generar_registra_el_evento_*` y `HU25_sin_informe_guardado_no_hay_evento`; `RemitirInformeTests`; `FinalizarInformeTests.CA3_finaliza_con_fecha_y_coordinador_*`; por API, `InformeEndpointTests.HU32_el_docente_remite_una_sola_vez_y_el_coordinador_ve_el_evento` (historial «Generado, Remitido») y `HU33_el_coordinador_revisa_decide_y_finaliza_una_sola_vez` («Generado, Remitido, Finalizado»); `EventosInformeSqlTests.Registra_y_lista_*` | Verificado para los tres eventos (véase la ejecución del 25/11) |
| CA2. El coordinador consulta en orden cronológico | `HistorialInformeTests.Ordena_*`; `InformeHomologacionTests.HU25_el_historial_se_consulta_en_orden_cronologico`; `InformeEndpointTests.HU25_el_coordinador_consulta_*`; `HistorialInforme.test.tsx`; `SolicitudesPage.test.tsx` (HU-25) | Verificado |
| CA3. Los eventos no se modifican ni se eliminan | `EventosInformeSqlTests.El_contrato_no_ofrece_*` y `El_mismo_evento_*`; el endpoint responde 405 a DELETE; `SQL_Server_real_impide_modificar_o_eliminar_eventos` | Verificado, con el trigger probado en SQL Server real |
| CA4. 401 sin sesión y 403 para roles sin permiso | `InformeEndpointTests.HU25_otros_roles_no_consultan_el_historial` | Verificado |
| Ruta publicada en el Gateway y en su OpenAPI | `AnalisisExternoRouteTests` y `GatewayHttpTests.OpenApi_*` (34 rutas) | Verificado |

## Ejecución registrada (20/11/2026)

- .NET 8 (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`):

  | Suite | Pasan | Omitidas | Fallan |
  | --- | --- | --- | --- |
  | Homologacion.Domain.Tests | 76 | 0 | 0 |
  | Homologacion.Application.Tests | 112 | 0 | 0 |
  | Homologacion.Infrastructure.Tests | 54 | 9 | 0 |
  | Homologacion.Api.Tests | 137 | 0 | 0 |
  | Gateway.Tests | 125 | 0 | 0 |

- **SQL Server 2022 real**, temporal y aislado, con una contraseña aleatoria no guardada; se eliminó después de la prueba. `Homologacion.Infrastructure.Tests` dio 62 superadas, 1 omitida y 0 fallos. Esto incluye la migración, el trigger de solo inserción y las pruebas de concurrencia existentes.
- **Frontend (Vitest):** 145/145 pruebas y `npm run build` correcto.
- **TDD:** las pruebas nuevas se ejecutaron primero y fallaron (no compilaba `Homologacion.Domain.Auditoria`; no existía `HistorialInforme.tsx`). Después se implementó.

## Ejecución registrada (25/11/2026, `develop` en `3df758a`)

Pruebas de HU-25, HU-32 y HU-33 filtradas por nombre (`HU25`, `HU32`, `HU33`, `RemitirInformeTests`, `FinalizarInformeTests`, `HistorialInformeTests`), en el contenedor `mcr.microsoft.com/dotnet/sdk:8.0`:

| Suite | Pasan | Fallan |
| --- | --- | --- |
| Homologacion.Api.Tests | 14 | 0 |
| Homologacion.Application.Tests | 21 | 0 |
| Homologacion.Domain.Tests | 5 | 0 |

En esta fecha no se repitió la prueba del trigger en SQL Server real; la del 20/11 sigue siendo la referencia.

## Pendiente

- Comprobación manual en el stack integrado: en una solicitud finalizada, el coordinador ve «Generado», «Remitido» y «Finalizado» con fecha, usuario y rol.
- Aceptación de la PO.
