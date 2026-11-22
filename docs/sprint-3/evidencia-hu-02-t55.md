# Evidencia HU-02 / T55 — interfaz e integración

Relacionada con [Issue #66](https://github.com/PabloToapanta/ProyectoHomologacion/issues/66) y [PR borrador #87](https://github.com/PabloToapanta/ProyectoHomologacion/pull/87). Verificada el 20/11/2026 (America/Guayaquil) en `feature/HU-2-Manejo-de-Roles`.

## Implementación

- Ruta `/admin/usuarios` protegida para Administrador, accesible desde el panel y el menú lateral.
- Listado de Docentes y Coordinadores; alta con contraseña inicial, cambio de rol, activación/desactivación y cambio de contraseña. La interfaz muestra errores y estado de las operaciones.
- La contraseña solo se mantiene en el formulario hasta enviar la solicitud; se limpia después de guardar o ante un error. La respuesta y el listado no la muestran. La entrega cara a cara corresponde al administrador.
- Ruta del frontend incorporada al Gateway HTTPS. Se conservó el contrato de lectura de docentes de HU-29.

## Comprobaciones ejecutadas

| Comprobación | Resultado |
| --- | --- |
| `npm test -- --maxWorkers=2` en `frontend` | 156 pruebas aprobadas, 0 fallidas; incluye interfaz, API y protección de ruta de HU-02. |
| `npm run build` en `frontend` | TypeScript y build de Vite aprobados. |
| `dotnet test tests/Gateway.Tests/Gateway.Tests.csproj --no-restore` | 139 pruebas aprobadas, 0 fallidas; incluye `/admin/usuarios` en HTTPS. |
| `python3 tests/security/hu02-smoke.py` | 24 comprobaciones aprobadas con Auth, frontend, Gateway HTTPS y SQL Server en Docker Compose aislado. |

El recorrido integrado comprobó que el Gateway sirve el HTML de la ruta `/admin/usuarios` por HTTPS y que las operaciones reales de Auth conservan permisos, validaciones y ausencia de secretos en respuestas. El certificado y las credenciales fueron sintéticos y efímeros. `TestResults/hu02-integration/resultado.json` contiene el detalle local sin credenciales y está excluido de Git.

## Pendiente de la historia

La entrega cara a cara de la contraseña requiere acción humana. Falta la revisión cruzada de Edwin y la aceptación funcional de Shirley; no se declara la HU-02 terminada ni integrada en `develop`.
