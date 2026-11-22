# Evidencia HU-02 / T54 — Auth y Gateway

Relacionado con [Issue #66](https://github.com/PabloToapanta/ProyectoHomologacion/issues/66) y la propuesta [`hu-02-gestion-usuarios-roles.md`](hu-02-gestion-usuarios-roles.md). Verificado el 20/11/2026 (America/Guayaquil) en la rama `feature/HU-2-Manejo-de-Roles`. **Estado:** T54 verificada técnicamente en un entorno aislado; T55, revisión cruzada y aceptación de la Product Owner pendientes.

## Resultados ejecutados

| Comprobación | Resultado |
| --- | --- |
| `dotnet test tests/AuthService.Tests/AuthService.Tests.csproj --no-restore --nologo -m:1 -p:UseSharedCompilation=false` | 79 aprobadas, 0 fallidas. Incluye administración HTTP, login, autorización, HU-29 y OpenAPI de Auth. |
| `dotnet test tests/Auth.Application.Tests/Auth.Application.Tests.csproj --nologo -m:1 -p:UseSharedCompilation=false` | 13 aprobadas, 0 fallidas. |
| `dotnet test tests/Gateway.Tests/Gateway.Tests.csproj --no-restore --nologo -m:1 -p:UseSharedCompilation=false` | 138 aprobadas, 0 fallidas. Incluye rutas, rol Administrador, 401/403 y OpenAPI del Gateway. |
| `python3 tests/security/hu02-smoke.py` | 22 comprobaciones aprobadas. HTTPS verificado con certificado sintético local; Auth y Gateway reales con SQL Server en Docker Compose aislado. |

El recorrido Docker cubrió login de Administrador; 401 sin token; alta de Docente y Coordinador; 403 para ambos roles; duplicado normalizado 409; listado y detalle sin secretos; rol inválido 400; cambio de rol reflejado en el siguiente login; desactivación, rechazo de login y reactivación; reemplazo de contraseña y rechazo de la anterior; autodesactivación rechazada. Usó datos sintéticos y secretos efímeros fuera del repositorio. El script ejecutó `down --volumes --remove-orphans` para su proyecto aislado; no quedaron contenedores `hu02-check-*` al verificarlo.

El archivo local `TestResults/hu02-integration/resultado.json` enumera las 22 comprobaciones sin credenciales y está excluido de Git. El script reproducible queda versionado en `tests/security/hu02-smoke.py`.

## Límites de la evidencia

- El recorrido Docker verificó Auth y Gateway por HTTPS, sin levantar la interfaz React ni Solicitudes. La compatibilidad de `GET /api/usuarios/docentes` se cubre en `AuthService.Tests`; el flujo completo de asignación HU-29 no se repitió aquí.
- El entorno HTTPS `Hu34` no publica Swagger; las rutas OpenAPI se comprobaron en las suites de Auth y Gateway con entorno de pruebas. Dos ejecuciones previas del script terminaron en su comprobación de Swagger (401 sin sesión y 404 en `Hu34`); se corrigió el guion y la ejecución final terminó sin fallos.
- La entrega cara a cara de la contraseña es una actividad humana acordada, no algo que pueda validar automáticamente el script.
- No se declara HU-02 terminada: faltan T55, revisión cruzada, aceptación de Shirley y la verificación final de la historia.
