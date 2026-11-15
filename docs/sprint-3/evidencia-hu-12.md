# HU-12 — adjuntos del expediente

**Estado al 15/11/2026:** implementación local en `feature/hu-12-adjuntos-solicitud`; criterios CA1–CA5 confirmados por Shirley como PO y límite técnico fijado en 20 MiB por archivo. La historia aún no está aceptada ni integrada.

## Alcance implementado localmente

- API de subida multipart para Coordinador, lista de metadatos y descarga para Coordinador/docente asignado.
- Validación de PDF por extensión, tipo MIME y cabecera; rechazo de archivos vacíos y mayores a 20 MiB.
- Persistencia de bytes y metadatos en SQL Server del servicio Solicitudes: tipo, nombre saneado, tamaño, SHA-256, `sub` del JWT y fecha UTC.
- Migración `AdjuntosSolicitudHu12` y panel de adjuntos en vistas de coordinación y docencia.
- Pruebas automatizadas añadidas en `tests/Solicitudes.Tests/AdjuntosSolicitudTests.cs` y `frontend/src/features/solicitudes/AdjuntosSolicitud.test.tsx`.

## Verificación pendiente

- Backend: no se pudo restaurar NuGet (`api.nuget.org` no resolvió); migración no aplicada y suite .NET no ejecutada.
- Frontend: `npm run typecheck` y la suite Vitest no se ejecutaron porque no está disponible el ejecutable `tsc`/`vitest` en esta copia.
- No ejecutado en Docker Compose ni revisado por Leslie; falta aceptación integrada de Shirley.
- Edwin debe revisar que la transacción serializable de la carga se coordine correctamente con la finalización concurrente de HU-33 (#64).

La verificación de migración, pruebas, Compose y revisión cruzada debe actualizarse aquí cuando ocurra; esta evidencia no declara la historia terminada.
