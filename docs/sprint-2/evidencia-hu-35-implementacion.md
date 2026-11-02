# HU-35 — Evidencia de implementación (Issue #36)

Fecha de ejecución: 02/11/2026. Rama: `feature/hu-35-token-cifrado`.
La [ficha de la historia](https://github.com/PabloToapanta/ProyectoHomologacion/blob/feature/hu-35-documentacion/docs/sprint-2/hu-35-token-firmado-cifrado.md) documenta los criterios GIVEN, WHEN, THEN, las tareas T18–T21 y las pruebas de aceptación pendientes de Shirley.

## Decisión técnica verificada

Auth emite un JWS HS256 dentro de un JWE `A256KW` + `A256CBC-HS512` con una clave de cifrado aleatoria de 32 bytes, independiente de la clave de firma. El login entrega `name`, `role` y `expiresAtUtc`; React trata el token como opaco. Gateway, Catálogo, Homologación y Auth rechazan tokens no cifrados, alterados o cifrados con otra clave. La clave de cifrado es obligatoria al iniciar cada servicio y no se registra en Git.

La combinación inicial propuesta `A256KW` + `A256GCM` no fue compatible con IdentityModel 7.7.3 en este proyecto; la combinación final se verificó con emisión, descifrado y validación de firma en xUnit.

## Pruebas ejecutadas

| Comando desde la raíz | Resultado | Criterios |
| --- | --- | --- |
| `dotnet test tests/AuthService.Tests/AuthService.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo` | 52 correctas | CA1–CA4; arranque inválido, descifrado y firma, login y roles |
| `dotnet test tests/Gateway.Tests/Gateway.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo` | 68 correctas | CA2–CA4; rechazos 401, roles y proxy |
| `dotnet test tests/Catalogo.Tests/Catalogo.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo` | 80 correctas | CA2–CA3; token inválido y arranque |
| `dotnet test tests/Homologacion.Api.Tests/Homologacion.Api.Tests.csproj --no-restore -m:1 -p:UseSharedCompilation=false --nologo` | 90 correctas | CA2–CA4; token inválido, arranque y roles |
| `cd frontend && npm run build` | compilación correcta | CA4; contrato de respuesta tipado |
| `cd frontend && npm test -- --run` | 94 correctas | CA4; sesión opaca, metadatos y caducidad |
| `docker compose --env-file /tmp/hu35-compose.env -f compose.sprint1.yaml -p hu35-validation config --quiet` | configuración válida | Variables JWE exigidas en Compose |

Las cuatro suites xUnit se ejecutaron nuevamente tras el último ajuste de arranque. Las pruebas usan claves y usuarios sintéticos, sin publicar JWT completos. Las rutas HTTP se ejercitan mediante `WebApplicationFactory`; no se afirma aquí una ejecución con SQL Server real.

## Pendiente para la Definición de Hecho

- Ejecutar el stack integrado con Docker Compose, SQL Server y navegador. En este entorno `docker info` indicó que el daemon Docker Desktop no está iniciado; solo se verificó `compose config`.
- Obtener CI en verde, revisión cruzada de otro desarrollador y aceptación funcional de Shirley. Las pruebas de aceptación PA1 y PA2 de la ficha siguen pendientes.
