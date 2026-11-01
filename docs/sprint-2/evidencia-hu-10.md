# HU-10 — registro de solicitudes / Issue #38

Rama `feature/hu-10-registro-solicitudes`, iniciada desde `origin/develop` d01ece3. Implementación local; no acredita aceptación PO, revisión cruzada ni ejecución integrada con Docker.

## Decisión funcional

Edwin confirmó que cada trámite Quipux corresponde a una única solicitud: el estudiante envía el cambio al coordinador. Se aplican validación del número normalizado, índice único SQL y error 409; se mantiene el formulario para corregir el intento duplicado.

## Cambios

- Servicio Solicitudes con cuatro capas, persistencia SQL Server y migración propia; registro/listado/detalle y consulta de carreras, solo Coordinador.
- Consulta mínima nueva en Catálogo para carreras FISEI vigentes, sin conceder permisos CRUD al coordinador.
- Rutas Gateway y frontend `/solicitudes`, formulario modal, listado paginado, detalle y mensajes de error; login del Coordinador lleva al módulo.
- Complemento Compose HU-10 y workflow CI con SQL Server efímero. Cambios locales de puertos del usuario conservados y excluidos de los commits HU-10.

## TDD y verificación observada

1. Siete pruebas de dominio fallaron con una implementación vacía antes de agregar las validaciones; después pasaron.
2. API y pantalla se comprobaron con casos válidos/invalidación y duplicado. Se corrigió el entorno de pruebas a .NET 8, porque la ejecución inicial sobre .NET 10 no es compatible con el TestHost 8.
3. `dotnet test src/solicitudes-service/Solicitudes.sln -c Release`: 15 aprobadas; 1 SQL Server omitida por no disponer de HU10_TEST_SQL.
4. `dotnet test src/catalogo-service/Catalogo.sln -c Release`: 76 aprobadas, incluida lectura del Coordinador sin escritura en el catálogo.
5. `dotnet test tests/Gateway.Tests/Gateway.Tests.csproj -c Release`: 71 aprobadas, incluidas rutas Solicitudes y conservación de rutas fuera de alcance.
6. Frontend: 101 pruebas aprobadas; TypeScript y compilación Vite correctos. Las pruebas anteriores que suponían que el Coordinador terminaba en el panel de administrador se ajustaron a su nuevo destino, manteniendo el rechazo de acceso al catálogo administrativo.

Las pruebas API locales usan SQLite y un doble del catálogo; no equivalen a una ejecución integrada de los contenedores. Resultados TRX locales en `tmp/hu10-tests` (no versionados). La advertencia NU1900 de un restore inicial correspondió a acceso restringido al índice de vulnerabilidades; no se declara una auditoría de dependencias completada por estas pruebas.

## Verificación integrada del 01/11/2026

Entorno: commit 3e6e5e4, `compose.sprint1.yaml` + `compose.hu10.yaml`, proyecto Compose desechable con secretos generados para la sesión y base vacía; no se usó el volumen de datos existente. El equipo no tiene runtime .NET 8, por lo que las pruebas se ejecutaron en un contenedor con SDK 8.0.427 dentro de la red del stack. Datos sintéticos.

1. Los siete servicios arrancaron con `--wait`; las migraciones crearon `HomologacionSolicitudes` como base separada, con índice único `IX_Solicitudes_QuipuxNormalizado`.
2. `dotnet test src/solicitudes-service/Solicitudes.sln -c Release` con `HU10_TEST_SQL`: 16 aprobadas, 0 omitidas, incluida la prueba SQL Server de migración y concurrencia.
3. Flujo por Gateway: 21 comprobaciones aprobadas. Login de los tres roles, carreras para Coordinador sin CRUD de catálogo, registro interno y externo, detalle, listado, duplicado normalizado 409, referencias y obligatorios 400, sin sesión 401, Docente y Administrador 403, inexistente 404.
4. Persistencia: el detalle sigue disponible tras reiniciar el contenedor de Solicitudes.
5. Navegador, Coordinador: login redirige a `/solicitudes`, duplicado muestra el mensaje y conserva el formulario, y un registro nuevo aparece en el listado.
6. `tests/sprint-1/smoke.py` sin fallos visibles con `PYTHONUTF8=1`; sin esa variable termina con UnicodeEncodeError en la consola de Windows.
7. Las contraseñas de la sesión no aparecieron en los registros de los contenedores.
8. CI del PR #45 en verde el 01/11/2026.

Las cuentas Coordinador y Docente se insertaron por SQL solo en la base desechable: no sustituye la carga controlada. Esta verificación técnica no acredita aceptación de la PO.

### Observaciones corregidas el 01/11/2026

Cambios locales sobre 3e6e5e4, pendientes de commit, revisión y CI. Las cifras anteriores corresponden a 3e6e5e4; las siguientes, a esta corrección.

- `Location` del POST devolvía el host interno `http://solicitudes-service:8080/...` a través del Gateway. Ahora es relativa: `/api/solicitudes/{id}`.
- Los errores esperados 400 y 409 se registraban como `fail` porque `ExceptionHandlerMiddleware` registra todo lo que recibe. Ahora los resuelve un filtro MVC, con el mismo estado, título y `application/problem+json`. El 503 y el 500 siguen en el middleware y se registran.

TDD: dos pruebas nuevas fallaron antes de corregir y la de registro del 503 ya pasaba. Después, en SDK 8 con SQL Server real: 19 aprobadas, 0 omitidas. Stack reconstruido: flujo por Gateway sin fallos, `Location` relativa y ningún `fail` tras los 400 y 409. Con el contenedor de catálogo detenido, `GET /api/solicitudes/carreras` devolvió 503 y registró un `fail`, lo que comprueba el 503 de esa ruta con contenedores reales.

## Pendientes de aceptación

Reverificación del 01/11/2026 antes de publicar las correcciones: suite actual ejecutada en SDK .NET 8 con SQL Server temporal aislado, 19 aprobadas y 0 omitidas. Resultado local: `tmp/hu10-review-20261101/hu10-review.trx`. El contenedor SQL y la red de prueba se eliminaron al finalizar; no se usaron las bases existentes. Esta ejecución comprueba la suite del servicio, no repite por sí sola el flujo de navegador del stack completo. La CI debe comprobar el nuevo commit tras subirlo.

- Preparar cuenta Coordinador autorizada para la prueba funcional de Shirley.
- Revisión de Leslie y coordinación de los puntos compartidos con Pablo/Leslie; ver propuesta de contrato.
- Revisión y aceptación de Shirley con evidencias por criterio.
- Coordinar HU-35/HU-36 para incluir el nuevo servicio sin cambios incompatibles de seguridad.

No se marca la historia como hecha. No incluye HU-11, HU-29/30, carga masiva ni informes.
