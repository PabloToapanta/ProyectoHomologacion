# Evidencia técnica HU-01 — T1

Esta es la evidencia de la implementación original en un proyecto. La estructura fue refactorizada posteriormente: consultar [evidencia de Clean Architecture](evidencia-hu-01-clean-architecture.md) y `src/auth-service/README.md` para rutas de proyectos, dependencias y verificaciones actuales.

**Fecha:** 19 de octubre de 2026 (America/Guayaquil).

**Vínculo:** [Issue #1 — Inicio de sesión y autorización](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1).

**Rama local:** `feature/hu-01-autenticacion`, cuyo punto de partida coincide con `develop`.

**Ejecución:** asistida por Codex para Pablo. **No constituye aceptación de Shirley ni revisión cruzada.**

## Alcance efectivamente implementado

Auth-service ASP.NET Core 8, Usuario/Rol, SQL Server con migración inicial, contraseñas protegidas con el hasher de ASP.NET Core, administrador inicial optativo por configuración externa y `POST /api/auth/login` con JWT HS256. Swagger y errores HTTP documentados. Ningún archivo previo de otro desarrollador o contrato compartido fue modificado.

Archivos agregados:

- `Homologacion.sln`: solución para el servicio y sus pruebas.
- `src/auth-service/AuthService/`: proyecto API; `Models/Usuario.cs` (Usuario/Rol), `Data/` (contexto, fábrica, semilla y migración con snapshot), `Security/` (opciones y emisor JWT), `Services/LoginService.cs`, `Contracts/LoginRequest.cs`, `Controllers/AuthController.cs`, `Program.cs` y configuración sin secretos.
- `src/auth-service/`: README de ejecución y propuesta de integración, Dockerfile, Compose exclusivo de T1, `.dockerignore` y `.env.example` sin credenciales.
- `tests/AuthService.Tests/`: proyecto xUnit, pruebas de comportamiento, API y seguridad JWT. SQLite relacional en memoria para aislamiento.
- `tests/auth-service/smoke.py`: verificación reproducible con SQL Server real y reinicio del servicio.
- `.github/workflows/auth-service.yml`: CI de build/tests para PR a `develop`; preparado, **no ejecutado en GitHub**.
- Este documento: evidencia técnica y pendientes.

## TDD y correcciones

Primero se ejecutaron 12 casos con la lógica de login/semilla/emisión JWT sin implementar: **10 fallaron y 2 pasaron**. Fallos esperados por `NotImplementedException`, registrados en `/tmp/hu01-tdd-red.log` de esta sesión.

Tras implementar, las pruebas HTTP detectaron que los atributos de validación del record estaban ubicados en sus propiedades en vez de en los parámetros del constructor. Se corrigió y se repitieron las pruebas. Se corrigió también el descubrimiento del contexto por las herramientas EF Core, separando la fábrica de diseño y validando la conexión después de construir el host.

## Resultados verificados

| Verificación ejecutada | Resultado real |
|---|---|
| `dotnet restore Homologacion.sln --disable-parallel` | Dependencias descargadas desde NuGet |
| `dotnet build Homologacion.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false` | Correcto, 0 advertencias y 0 errores |
| `dotnet test Homologacion.sln --configuration Release --no-build -m:1 --logger 'trx;LogFileName=hu01-t1.trx' --results-directory /tmp/hu01-test-results` | **36 aprobadas, 0 fallidas, 0 omitidas** |
| `dotnet-ef migrations add InitialAuth --project src/auth-service/AuthService --output-dir Data/Migrations --no-build` (herramienta 8.0.30 en `/tmp/hu01-tools`) | Migración generada correctamente |
| Compose aislado `hu01-verification`, SQL Server 2022 Developer + API compilada en imagen .NET 8 | Arranque y aplicación de migración correctos |
| `python3 tests/auth-service/smoke.py /tmp/hu01-verification.env` | Login con SQL Server, JWT firmado, 400/401, Swagger y reinicio idempotente correctos |
| `dotnet list <ruta-absoluta>/Homologacion.sln package --vulnerable --include-transitive` | NuGet no reportó paquetes vulnerables en los dos proyectos, con las fuentes consultadas en esta fecha |
| `git diff --check` | Sin errores en cambios rastreados; los archivos de implementación aún son nuevos y no se han agregado al índice |

El runner y Docker necesitaron ejecución con permisos ampliados de la sesión para sockets locales; las pruebas no se sustituyeron por una inspección estática. Los datos usados fueron sintéticos y las claves del smoke se generaron aleatoriamente fuera de Git. Los contenedores y el volumen desechable de verificación se retiran al terminar.

### Qué comprueban las 36 pruebas

Credenciales correctas y normalización del nombre; credenciales incorrectas; usuario inexistente o inactivo; semilla habilitada/deshabilitada, validación e idempotencia; prohibición de promover o reactivar usuarios mediante semilla; unicidad en base relacional; hashes distintos para la misma contraseña, rechazo de contraseña incorrecta y actualización de hash antiguo; firma HS256, claims y caducidad; validación HTTP de JSON y campos; respuesta genérica 401 sin token; ausencia de contraseña/hash en respuesta; `no-store`; documentación OpenAPI; configuración JWT insegura; rechazo de tokens faltantes, malformados, con firma distinta, expirados o de otro emisor/audiencia; autorización por roles en una ruta exclusiva de pruebas.

### Qué comprueba el smoke SQL Server

```text
PASS: login SQL Server emite JWT HS256 válido con rol y expiración de 15 minutos
PASS: credenciales incorrectas -> 401 sin token; cuerpo inválido -> 400; OpenAPI disponible
PASS: reinicio reaplica migraciones y semilla sin duplicar ni sobrescribir el hash
```

La prueba verifica criptográficamente HS256 con el secreto temporal, los claims de Administrador, emisor, audiencia y vigencia. Consulta el hash en SQL Server sin imprimirlo y comprueba que el reinicio conserva una sola fila y el mismo hash. No es una prueba de integración de todo el sistema.

## Criterios de aceptación del Issue #1

| Criterio | Estado y evidencia |
|---|---|
| Credenciales correctas: administrador recibe JWT y entra al panel | **Parcial:** JWT verificado en xUnit y SQL Server real. Acceso al panel pendiente de T3 |
| Credenciales incorrectas: error y ningún token | **Cumplido en API:** 401 genérico sin token para usuario/contraseña inválidos; evidencia xUnit y smoke. Mensaje en pantalla pendiente de T3 |
| Ruta protegida sin JWT válido: Gateway responde 401 | **Pendiente de T2:** se comprobó el middleware del auth-service con rutas exclusivas del host de pruebas; eso no certifica YARP |

No se marcan criterios globales como aceptados y no se cierra HU-01.

## Pendientes y siguiente paso

1. Revisar con el equipo la propuesta JSON/claims y acordar emisor, audiencia, secreto, caducidad, rutas internas y puertos antes de integrar Gateway/frontend. Está detallada en `src/auth-service/README.md`; el documento compartido sigue como propuesta inicial.
2. T2: implementar YARP con validación JWT y demostrar 401 para rutas protegidas, incluidos tokens expirados o inválidos.
3. T3: login React, tratamiento de errores y acceso al panel con rutas protegidas.
4. Revisión cruzada, ejecución del workflow remoto tras autorización de publicación, integración conjunta en Compose y aceptación de la Product Owner.

T1 no incluye registro público, CRUD de usuarios, recuperación de contraseña, refresh tokens ni revocación inmediata. Un JWT ya emitido conserva validez hasta su caducidad. Antes de despliegue institucional se deben definir TLS, límites de intentos de login, gestión/rotación de secretos y permisos SQL de producción; el Compose incluido es de desarrollo local.

La verificación descrita se realizó antes de crear commits o publicar la rama. Posteriormente, Pablo autorizó commits atómicos con Conventional Commits en español y el push de `feature/hu-01-autenticacion`. Esta autorización no incluye merges, cierre del Issue ni aceptación de la historia.
