# HU-01 / Issue #1 — Refactorización a Clean Architecture

Fecha: 19 de octubre de 2026 (America/Guayaquil). Ejecución técnica asistida por Codex para Pablo. No sustituye revisión cruzada ni aceptación de Shirley.

## Estado inicial y diagnóstico

`git status --short --branch` mostró `feature/hu-01-autenticacion` sin cambios locales. HEAD: `feb10c4`, después de los cuatro commits de T1. No se cambiaron commits existentes, rama, remotos ni contratos compartidos.

Antes había un único `AuthService.csproj`. Controladores, DTO HTTP, entidades, EF, hashing, semilla y JWT compartían ensamblado. `LoginService` dependía de `AuthDbContext`, EF Core, `IPasswordHasher<Usuario>` y `JwtTokenIssuer`; `LoginRequest/LoginResponse` se usaban también fuera de HTTP. La organización por carpetas no impedía esas dependencias.

Se volvió a consultar el [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1), junto con AGENTS.md, contexto, aceptación y contratos locales. El trabajo conserva T1 de HU-01 y no adelanta T2/T3 ni otras historias.

## Estructura y referencias finales

```text
src/auth-service/
├── Auth.Domain/
│   ├── Auth.Domain.csproj
│   └── Entities/                 # Usuario, Rol
├── Auth.Application/
│   ├── Auth.Application.csproj
│   ├── Abstractions/             # IUserRepository, IPasswordService, ITokenIssuer
│   └── Login/                    # LoginService, LoginCommand, LoginResult
├── Auth.Infrastructure/
│   ├── Auth.Infrastructure.csproj
│   ├── Data/                     # DbContext, fábrica, semilla, inicialización
│   │   └── Migrations/           # Migración original y snapshot
│   ├── Repositories/            # UserRepository
│   ├── Security/                # PasswordService, JwtOptions, JwtTokenIssuer
│   └── DependencyInjection.cs   # Registro de adaptadores, llamado desde Api
├── Auth.Api/
│   ├── Auth.Api.csproj
│   ├── Contracts/              # LoginRequest y LoginResponse HTTP
│   ├── Controllers/            # POST /api/auth/login
│   ├── Program.cs              # Composición, Swagger, JWT bearer, middleware
│   └── appsettings.json
├── Dockerfile
└── compose.yaml

tests/
├── Auth.Application.Tests/      # Caso de uso con puertos simulados manualmente
└── AuthService.Tests/           # Pruebas existentes de regresión y arquitectura
```

| Capa | Referencias directas a proyectos |
|---|---|
| Domain | Ninguna |
| Application | Domain |
| Infrastructure | Application, Domain |
| Api | Application, Infrastructure |

Domain y Application usan `Microsoft.NET.Sdk`, sin paquetes ni referencias a frameworks adicionales. No conocen EF Core, ASP.NET, opciones de configuración ni JWT. Las pruebas verifican tanto los csproj como las referencias reales de sus ensamblados.

`LoginService` recibe las tres interfaces de Application. `IUserRepository` consulta el usuario con su rol y persiste el hash actualizado; `IPasswordService` verifica/protege contraseñas y encapsula el hash sintético para usuarios inexistentes; `ITokenIssuer` produce un resultado del caso de uso. El controlador transforma DTO HTTP a comando y resultado a DTO HTTP.

Infrastructure conserva EF SQL Server/Design 8.0.30 y declara explícitamente `System.IdentityModel.Tokens.Jwt` 7.7.3, que ya era dependencia transitiva de T1. Su referencia a `Microsoft.AspNetCore.App` permite usar el hasher del framework. No se incorporaron MediatR, frameworks de mocking, repositorios genéricos o unidades de trabajo adicionales.

Api es el punto de composición: invoca `AddAuthInfrastructure`, registra el caso de uso y enlaza las opciones. La inicialización de la base está encapsulada en Infrastructure y se llama desde el arranque; Api no contiene consultas ni operaciones EF.

## Pasos comprobados

1. Línea base: **36/36** pruebas existentes aprobadas.
2. Extracción de Domain y adaptación de los nombres CLR del modelo: **36/36** aprobadas.
3. Pruebas de proyectos/referencias antes de crear las otras capas: **3 fallidas, 1 aprobada**; detectaron Application, Infrastructure y Api pendientes. Evidencia temporal: `/tmp/hu01-clean-architecture-red.log`.
4. Extracción de Application/Infrastructure/Api y adaptación de constructores en las pruebas originales: **40/40** aprobadas.
5. Adición de comprobaciones de ensamblados y pruebas puras del caso de uso; actualización de solución, Dockerfile, Compose, filtros de CI y README.
6. Verificación final de restore/build/test, herramientas EF y transición Docker con una base SQL Server creada por la imagen anterior.

No se eliminaron pruebas originales ni capacidades. Git muestra las rutas originales retiradas y los archivos en sus nuevas ubicaciones mientras los cambios permanecen sin agregar al índice; corresponden al traslado de capas.

## Resultados reales

| Comprobación ejecutada | Resultado |
|---|---|
| `dotnet restore Homologacion.sln --disable-parallel` | Correcto para los seis proyectos: cuatro de producción y dos de pruebas |
| `dotnet build Homologacion.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false` | Correcto, **0 advertencias, 0 errores** |
| `dotnet test Homologacion.sln --configuration Release --no-build -m:1` | **56 aprobadas, 0 fallidas, 0 omitidas**: 43 en AuthService.Tests y 13 en Auth.Application.Tests |
| `dotnet-ef migrations has-pending-model-changes --project src/auth-service/Auth.Infrastructure --configuration Release --no-build` | `No changes have been made to the model since the last migration.` |
| `dotnet-ef migrations script --idempotent --project src/auth-service/Auth.Infrastructure --configuration Release --no-build --output /tmp/hu01-clean-migrations.sql` | Script SQL generado correctamente sin arrancar Api ni suministrar JWT |
| Docker Compose, imagen refactorizada con base de la imagen anterior | Construcción, arranque, migración idempotente y semilla correctos |
| `python3 tests/auth-service/smoke.py /tmp/hu01-clean-refactor.env` | JWT HS256, login SQL, errores 400/401, Swagger y reinicio aprobados |
| Comparación antes/después sobre la base temporal | Usuario, UUID, nombre, nombre normalizado, estado, rol, huella del hash y migraciones idénticos; roles idénticos |
| Comparación de `/swagger/v1/swagger.json` antes/después | Documento JSON idéntico, incluidos esquemas y respuestas |

El primer registro TRX de la ejecución conjunta utilizó un nombre fijo y el segundo proyecto lo sobrescribió. Se guardaron nuevamente los resultados por proyecto en `/tmp/hu01-clean-test-results/integracion` y `/tmp/hu01-clean-test-results/aplicacion` para conservar ambos reportes. Los archivos temporales no se incorporan a Git.

Los comandos se ejecutaron con los permisos de sesión necesarios para sockets de xUnit y Docker. Para la transición se utilizó exclusivamente una base temporal con credenciales aleatorias externas a Git. Primero arrancó la imagen previa de T1 y se capturó el estado; luego se reconstruyó el servicio refactorizado conservando el mismo volumen. La comparación no imprimió contraseñas, hashes ni JWT.

El identificador `20261019123906_InitialAuth` y sus operaciones Up/Down se conservaron. Solo cambiaron la ubicación, el namespace del contexto/migración y los nombres CLR del modelo/snapshot. No hubo nueva migración ni recreación de tablas. La generación de runtimeconfig en Infrastructure permite ejecutar las herramientas EF directamente sobre esta biblioteca y mantener EF Design fuera de Api.

## Comportamiento y límites conservados

- Misma ruta `POST /api/auth/login` y mismos nombres JSON.
- Mismos errores 400/401, detalle genérico de credenciales inválidas, middleware de errores y cabeceras de no almacenamiento.
- Mismos claims, algoritmo HS256, validación de firma/emisor/audiencia/caducidad y duración configurable.
- Mismos nombres de configuración de conexión, JWT, migraciones y semilla; sin secretos predeterminados.
- Contraseñas protegidas, rehash al autenticar, verificación sintética para usuario inexistente, rechazo de inactivos y semilla que no sobrescribe/promueve/reactiva cuentas.
- Pruebas originales mantenidas, con ajustes de namespace y construcción de adaptadores. Nuevas pruebas del caso de uso no requieren EF Core ni ASP.NET; las de arquitectura previenen referencias entre capas no permitidas.

T1 continúa verificada en el backend. El acceso al panel React (T3), el 401 desde Gateway YARP (T2), la revisión y aceptación de la historia siguen pendientes. CI se actualizó localmente y no se ejecutó remotamente. No se realizaron commits, pushes ni merges durante esta refactorización.
