# HU-12 — adjuntos del expediente

**Estado al 15/11/2026:** implementación local en `feature/hu-12-adjuntos-solicitud`; criterios CA1–CA5 confirmados por Shirley como PO y límite técnico fijado en 20 MiB por archivo. La historia aún no está aceptada ni integrada.

## Alcance implementado localmente

- API de subida multipart para Coordinador, lista de metadatos y descarga para Coordinador/docente asignado.
- Validación de PDF por extensión, tipo MIME y cabecera; rechazo de archivos vacíos y mayores a 20 MiB.
- Persistencia de bytes y metadatos en SQL Server del servicio Solicitudes: tipo, nombre saneado, tamaño, SHA-256, `sub` y nombre de usuario (`name`) del JWT y fecha UTC.
- Migración `AdjuntosSolicitudHu12` y panel de adjuntos en vistas de coordinación y docencia.
- Pruebas automatizadas añadidas en `tests/Solicitudes.Tests/AdjuntosSolicitudTests.cs` y `frontend/src/features/solicitudes/AdjuntosSolicitud.test.tsx`.

## Verificación al 15/11/2026, después de corregir errores de prueba

- Frontend: `npm run typecheck` aprobado; Vitest aprobó 143 pruebas en 22 archivos.
- Backend: con .NET 8.0.34, `dotnet test tests/Solicitudes.Tests/Solicitudes.Tests.csproj -c Release` terminó correctamente: 154 pruebas, 149 aprobadas, 0 fallidas y 5 omitidas. Las omitidas requieren SQL Server aislado (`HU10_TEST_SQL`) y se ejecutan en CI. Los mensajes `fail` de excepciones de catálogo/directorio corresponden a pruebas que verifican respuestas de error; no causaron fallos en la suite.
- La migración aún no se aplicó en SQL Server ni se ejecutó Docker Compose (al 15/11; ver la verificación del 21/11).
- No revisado por Leslie; falta aceptación integrada de Shirley.
- Edwin debe revisar que la transacción serializable de la carga se coordine correctamente con la finalización concurrente de HU-33 (#64).

## Cambios de la revisión cruzada del 20/11/2026 (PR #82)

| Observación | Cambio | Prueba |
| --- | --- | --- |
| Bloqueante: el Gateway no publicaba los adjuntos | Rutas `solicitud-adjuntos` (`/api/solicitudes/{id}/adjuntos`, POST y GET) y `solicitud-adjunto-descarga` (`/api/solicitudes/{id}/adjuntos/{adjuntoId}`, GET), clúster `solicitudes`, política `AcademicAccess`. Los límites de cuerpo ya admiten 20 MiB: nginx 26 MiB y Kestrel del Gateway 30 MB por defecto. | `AnalisisExternoRouteTests` (3 casos nuevos); `GatewayHttpTests.OpenApi_*` (35 rutas) |
| CA3 «quién lo subió» | Nueva columna `SubidoPorNombre` con el claim `name` del token (si falta, el `sub`); `SubidoPor` conserva el identificador. Migración `AdjuntoSubidoPorNombreHu12`. La interfaz muestra «subido por X el fecha». Los adjuntos previos quedan con nombre vacío y la interfaz omite «por». | `AdjuntosSolicitudTests` (nombre del token y caso sin claim `name`); `AdjuntosSolicitud.test.tsx` |
| Descarga | `URL.revokeObjectURL` se difiere 1 s después del clic. | `AdjuntosSolicitud.test.tsx` (no se revoca en el mismo turno) |

Las pruebas nuevas se ejecutaron primero y fallaron (4 en Gateway, 2 en Solicitudes, 1 en el frontend); después se implementó.

**Verificación al 21/11/2026** (contenedor `mcr.microsoft.com/dotnet/sdk:8.0`):

- `Gateway.Tests`: 127/127.
- `Solicitudes.Tests`: 150 superadas y 5 omitidas sin SQL Server; con SQL Server 2022 real, temporal y aislado (`HU10_TEST_SQL`, incluye aplicar las migraciones): 155/155.
- Frontend: Vitest 143/143 (`--maxWorkers=2`) y `npm run build` correcto. Con el paralelismo por defecto, dos o tres pruebas de `UnidadesPanel` y `SolicitudesPage` superan el límite de 5 s por la carga de la máquina; ocurre igual sin estos cambios y pasan ejecutadas solas.
- No ejecutado: Docker Compose ni la revisión visual en el navegador.

La suite de pruebas backend quedó verificada localmente. Docker Compose, la revisión del Gateway por Pablo y la nueva revisión cruzada siguen pendientes; esta evidencia no declara la historia terminada.
