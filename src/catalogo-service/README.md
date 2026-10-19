# catalogo-service — HU-04, T4/T5, Issue #2

ASP.NET Core 8 / SQL Server / EF Core 8. Clean Architecture:

```text
Catalogo.Domain/         Carrera y validaciones sin EF ni HTTP
Catalogo.Application/    CarreraService, DTO e ICarreraRepository
Catalogo.Infrastructure/ CatalogoDbContext, repositorio SQL Server y Migrations
Catalogo.Api/            Controlador, ProblemDetails, JWT, Swagger y composición
Catalogo.sln             Solución independiente con los cuatro proyectos y pruebas
../../tests/Catalogo.Tests/ Dominio, casos de uso, HTTP y restricciones relacionales
```

No se modifica Homologacion.sln ni los otros servicios. Las dependencias apuntan hacia Domain/Application. No se agregan mediadores, mensajería ni repositorios genéricos.

## Restauración, compilación y pruebas

Desde la raíz del repositorio, con SDK .NET 8:

```powershell
dotnet restore src/catalogo-service/Catalogo.sln
dotnet build src/catalogo-service/Catalogo.sln --no-restore -c Release
dotnet test src/catalogo-service/Catalogo.sln --no-restore -c Release
```

Los tests HTTP usan el servidor ASP.NET real en memoria, JWT firmados sintéticos y SQLite relacional con claves foráneas activadas. No requieren Docker. **SQLite no comprueba el proveedor SQL Server ni sus códigos de concurrencia**. Los resultados y limitaciones reales están en `docs/sprint-1/evidencia-hu-04-t4-t5.md`.

## Configuración y ejecución

Variables requeridas, sin valores reales en Git:

| Variable | Uso |
|---|---|
| `ConnectionStrings__Catalogo` | Base SQL Server exclusiva del catálogo |
| `Jwt__Issuer` | Mismo emisor de Auth/Gateway |
| `Jwt__Audience` | Misma audiencia de Auth/Gateway |
| `Jwt__SigningKey` | Mismo secreto HS256 de Auth/Gateway, mínimo 32 bytes UTF-8 |
| `Database__ApplyMigrations` | `true` solo para aplicar migraciones explícitamente en desarrollo |
| `ASPNETCORE_ENVIRONMENT` | `Development` habilita Swagger |

Con las variables configuradas:

```powershell
dotnet run --project src/catalogo-service/Catalogo.Api --urls http://127.0.0.1:5082
```

Swagger: `http://127.0.0.1:5082/swagger`. Ingresar el JWT de un administrador emitido por HU-01. Las operaciones requieren rol `Administrador`; sin token devuelven 401 y con otro rol 403. No existen credenciales semilla en este servicio.

### Migraciones

Desde `src/catalogo-service`:

```powershell
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project Catalogo.Infrastructure
dotnet ef migrations script --project Catalogo.Infrastructure --idempotent --output Migrations.sql
dotnet ef database update --project Catalogo.Infrastructure
```

La factoría EF usa `ConnectionStrings__Catalogo`; si no existe usa autenticación integrada local únicamente como valor de diseño. La API exige su conexión explícita. La migración y `Migrations.sql` son para SQL Server; no se aplican con `EnsureCreated` en producción.

### Docker Compose local

Copiar `.env.example` a `.env` en esta carpeta y configurar valores locales. No versionar `.env`. Requiere motor Docker Linux activo:

```powershell
docker compose -f src/catalogo-service/compose.yaml --env-file src/catalogo-service/.env up --build
```

Publica solo la API en loopback:5082; SQL Server queda en la red interna con volumen persistente. En Compose se aplican migraciones tras el healthcheck de SQL Server. Este compose es exclusivo del catálogo, no configura Auth/Gateway/React. Para producción debe existir una composición revisada que desactive Swagger, gestione secretos y aplique migraciones como paso de despliegue.

## Contrato HTTP

Entrada y salida (salida añade `id` UUID):

```json
{ "nombre": "Carrera sintética", "facultad": "Facultad de prueba", "vigencia": true, "modalidad": null }
```

| Método | Ruta | Resultado |
|---|---|---|
| GET | `/api/carreras` | 200 arreglo, orden por nombre/facultad |
| GET | `/api/carreras/{id}` | 200 DTO / 404 |
| POST | `/api/carreras` | 201 DTO y Location / 400 / 409 |
| PUT | `/api/carreras/{id}` | 200 DTO / 400 / 404 / 409 |
| DELETE | `/api/carreras/{id}` | 204 / 404 / 409 si tiene asignaturas |

Nombre y facultad obligatorios, 200 caracteres; modalidad opcional, 100. Vigencia es booleana obligatoria (no se infiere false si falta). Duplicado por nombre/facultad normalizados: Unicode FormKC, espacios compactados, mayúsculas invariantes; se distinguen acentos. El índice único evita duplicados concurrentes; la FK sin cascada evita eliminar carreras relacionadas. Errores de negocio: ProblemDetails con `code` y `detail`; binding: ValidationProblemDetails con `errors`.

No hay endpoints de asignaturas/unidades/contenidos. La tabla Asignaturas contiene solo un ancla de FK; ver la propuesta de continuidad para HU-05/HU-06 y la integración pendiente en `docs/arquitectura/propuesta-integracion-hu-04.md`.
