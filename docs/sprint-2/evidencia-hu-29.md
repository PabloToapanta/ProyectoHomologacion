# HU-29 — asignación de dos docentes / Issue #40

Rama `feature/hu-29-asignar-docentes`, creada desde `2904c3b` de `feature/hu-11-asignaturas-aprobadas` (PR #51 abierto al preparar la entrega). Se apila sobre HU-11 porque ambas cambian los mismos archivos de Solicitudes, del Gateway y la instantánea de migraciones. Se publica como borrador para revisión; antes de habilitar su integración, incorporar #51 y sincronizar la rama con `develop`. Sin revisión cruzada ni aceptación de la PO.

## Contrato

- **Auth**, nuevo y de lectura: `GET /api/usuarios/docentes`, solo Coordinador. Devuelve `id` y `nombreUsuario` de las cuentas activas con rol Docente, ordenadas por nombre. No expone contraseñas ni administra usuarios (HU-02 sigue fuera).
- **Solicitudes:** `GET /api/solicitudes/{id}/docentes-disponibles` reenvía esa consulta con el JWT del Coordinador; Solicitudes no accede a la base de Auth.
- **Solicitudes:** `POST /api/solicitudes/{id}/docentes` con `{ "docenteIds": [a, b] }`, solo Coordinador. 200 con el detalle; 400 si no son exactamente dos, se repiten o alguno no es Docente activo; 404 si la solicitud no existe; 409 si ya tiene docentes; 503 si Auth no responde.
- La solicitud pasa de `Registrada` a `Docentes asignados`. El detalle añade `docentesAsignados` con `docenteId`, `nombreUsuario` y `orden`.
- Tabla `DocentesAsignados` (migración `AsignacionDocentesHu29`) con índices únicos por solicitud y orden, y por solicitud y docente: dos asignaciones simultáneas no pueden dejar más de dos docentes.
- Configuración nueva `Auth:BaseUrl`; `compose.hu10.yaml` la apunta a `auth-service`.

## Decisiones y límites

- **Reasignación:** su política sigue pendiente (Issue #40), así que una segunda asignación devuelve 409. No se inventa cómo cambiar docentes.
- **Rol en el momento de asignar:** se comprueba en Auth en cada asignación y se guarda el nombre de usuario de ese momento. Si después se desactiva la cuenta, la asignación no cambia; HU-30 debe decidir cómo tratarlo.
- **Archivos de otros responsables:** el controlador nuevo de Auth no toca los archivos que modifica PR #49 (HU-35); solo cambian `IUserRepository`, `UserRepository` y `DependencyInjection.cs`. Debe acordarse con Pablo, al igual que las dos rutas nuevas en `src/gateway/appsettings.json`.
- **HU-35 y HU-36:** el reenvío del JWT de Solicitudes a Auth debe incorporarse cuando se integren el token cifrado y la clave interna.

## Verificación del 03/11/2026

Entorno: cambios locales sobre `2904c3b`, stack desechable `compose.sprint1.yaml` + `compose.hu10.yaml` con secretos generados y base vacía, y SDK 8.0.427 en contenedor porque el equipo no tiene runtime .NET 8. Datos sintéticos.

1. TDD: fallaron antes de implementar 7 pruebas de Solicitudes, 3 del Gateway, 4 de Auth y 6 de interfaz. Otras 4 de arquitectura de Auth fallaron por el entorno: el contenedor no tenía `Homologacion.sln`; tras copiarlo pasaron.
2. Con SQL Server real, sin omitidas: Solicitudes 92 (incluida la asignación concurrente), Catálogo 77, Gateway 79, AuthService 48 y Auth.Application 13 aprobadas.
3. `npx vitest run`: 114 aprobadas en 15 archivos. `npm run build` correcto.
4. Flujo por Gateway con Auth real: 23 comprobaciones aprobadas. Disponibles solo activos con rol Docente, asignación, detalle y listado con estado, reasignación 409, rechazos 400 sin cambios (uno, tres, repetido, docente inactivo, Coordinador, inexistente, sin cuerpo), 404, 401 y 403.
5. Con `auth-service` detenido: 503 y la solicitud sigue `Registrada` sin docentes. Es el único `fail` en registros.
6. Persistencia tras reiniciar Solicitudes; rutas en Swagger; sin contraseñas de la sesión en registros; `tests/sprint-1/smoke.py` con `PYTHONUTF8=1` sin fallos (9 PASS).
7. Navegador como Coordinador: el segundo selector excluye al primero; tras guardar, el detalle y el listado muestran «Docentes asignados» y persisten al reabrir. A 375 px sin desbordamiento. Sin errores de consola.

Las cuentas Coordinador y Docente se insertaron por SQL solo en la base desechable: no sustituye la carga controlada que HU-29 necesita para la aceptación. **No verificado:** CI, revisión cruzada y aceptación de la PO. Las capturas no se revisaron.

## Revisión previa a publicación — 03/11/2026

Se revisaron los cambios frente a #40 y se repitieron las suites actuales: Solicitudes 92 con SQL Server real temporal (ninguna omitida), AuthService 48, Auth.Application 13, Gateway 79, Catálogo 77 y frontend 114; TypeScript y compilación Vite correctos. Las pruebas .NET trabajaron sobre copias temporales del código; la base SQL y su red de pruebas se eliminaron al finalizar. TRX local: `tmp/hu29-review-20261101/hu29-review.trx` (el directorio conserva el nombre histórico del script reutilizado).

Esta repetición verifica las suites, sin un nuevo recorrido de navegador contra el stack completo. El PR se publica como borrador dependiente de #51; no acredita aceptación. En un commit separado se incluyen las correcciones documentales de HU-08 (#50), sin implementación de importación PDF. Se excluyen los ajustes locales de puertos en `compose.hu05.yaml` y `compose.sprint1.yaml`, y el selector anterior inconcluso.
