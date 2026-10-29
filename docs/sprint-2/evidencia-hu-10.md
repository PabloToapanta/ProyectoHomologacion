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

## Pendientes de aceptación

- Ejecutar SQL Server real y Compose completo: Docker estaba apagado durante esta sesión; no se levantó automáticamente.
- Preparar cuenta Coordinador autorizada y datos sintéticos de catálogo para prueba funcional.
- Revisión de Leslie y coordinación de los puntos compartidos con Pablo/Leslie; ver propuesta de contrato.
- Ejecutar CI después de publicar; revisión y aceptación de Shirley con evidencias por criterio.
- Coordinar HU-35/HU-36 para incluir el nuevo servicio sin cambios incompatibles de seguridad.

No se marca la historia como hecha. No incluye HU-11, HU-29/30, carga masiva ni informes.
