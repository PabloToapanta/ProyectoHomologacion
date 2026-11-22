# HU-25 · Historial y auditoría de informes — contrato y evidencia

Issue [#74](https://github.com/PabloToapanta/ProyectoHomologacion/issues/74). Rama `feature/hu-25-auditoria-informes` desde `develop` (`4b1162c`). Responsables: Edwin y Shirley. Revisión cruzada: Leslie. Toca la configuración del Gateway, a cargo de Pablo.

## Alcance

HU-25 depende de HU-32 (remitir) y HU-33 (finalizar), que todavía no existen. Por eso:

- **Ya se registra** el evento «Generado», en la misma transacción que el informe (HU-23).
- **Se dejan definidos** los tipos «Remitido» y «Finalizado», y la regla del flujo: cada evento ocurre una vez y después del anterior. HU-32 y HU-33 los registran con `IAuditoriaInformes.RegistrarAsync` y `HistorialInforme.ValidarSiguiente`.
- **No se registran** eventos de días pasados: no hay datos inventados. Los informes generados antes de esta versión no tienen evento «Generado».

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
| CA1. Eventos con fecha y usuario | `HistorialInformeTests` (dominio); `InformeHomologacionTests.HU25_generar_registra_el_evento_*` y `HU25_sin_informe_guardado_no_hay_evento`; `EventosInformeSqlTests.Registra_y_lista_*` | «Generado»: verificado. «Remitido» y «Finalizado»: **pendientes de HU-32/33** |
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

## Pendiente

- Registrar «Remitido» (HU-32) y «Finalizado» (HU-33), y probarlos con este historial.
- Prueba manual en el stack integrado: generar un informe como docente y consultar el historial como coordinador. La migración se aplica sola al arrancar Homologación.
- Aceptación de la PO.
