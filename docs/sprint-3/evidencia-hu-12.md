# HU-12 — adjuntos del expediente

**Estado al 15/11/2026:** implementación local en `feature/hu-12-adjuntos-solicitud`; criterios CA1–CA5 confirmados por Shirley como PO y límite técnico fijado en 20 MiB por archivo. La historia aún no está aceptada ni integrada.

## Alcance implementado localmente

- API de subida multipart para Coordinador, lista de metadatos y descarga para Coordinador/docente asignado.
- Validación de PDF por extensión, tipo MIME y cabecera; rechazo de archivos vacíos y mayores a 20 MiB.
- Persistencia de bytes y metadatos en SQL Server del servicio Solicitudes: tipo, nombre saneado, tamaño, SHA-256, `sub` del JWT y fecha UTC.
- Migración `AdjuntosSolicitudHu12` y panel de adjuntos en vistas de coordinación y docencia.
- Pruebas automatizadas añadidas en `tests/Solicitudes.Tests/AdjuntosSolicitudTests.cs` y `frontend/src/features/solicitudes/AdjuntosSolicitud.test.tsx`.

## Verificación al 15/11/2026, después de corregir errores de prueba

- Frontend: `npm run typecheck` aprobado; Vitest aprobó 143 pruebas en 22 archivos.
- Backend: con .NET 8.0.34, `dotnet test tests/Solicitudes.Tests/Solicitudes.Tests.csproj -c Release` terminó correctamente: 154 pruebas, 149 aprobadas, 0 fallidas y 5 omitidas. Las omitidas requieren SQL Server aislado (`HU10_TEST_SQL`) y se ejecutan en CI. Los mensajes `fail` de excepciones de catálogo/directorio corresponden a pruebas que verifican respuestas de error; no causaron fallos en la suite.
- La migración aún no se aplicó en SQL Server ni se ejecutó Docker Compose.
- No revisado por Leslie; falta aceptación integrada de Shirley.
- Edwin debe revisar que la transacción serializable de la carga se coordine correctamente con la finalización concurrente de HU-33 (#64).

La suite de pruebas backend quedó verificada localmente. La migración en SQL Server, Docker Compose y revisión cruzada siguen pendientes; esta evidencia no declara la historia terminada.
