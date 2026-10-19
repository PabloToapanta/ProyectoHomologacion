# auth-service — HU-01 / T1

Relacionado con [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1).
Servicio independiente ASP.NET Core 8, con base SQL Server exclusiva de Auth.
No consulta datos del catálogo ni implementa Gateway (T2) o frontend (T3).

## Decisiones de implementación

- EF Core 8 con migración versionada; `Usuarios` y `Roles` pertenecen únicamente a Auth.
- Cada usuario tiene un rol: `Administrador`, `Docente` o `Coordinador`. Se normaliza el nombre con `Trim().ToUpperInvariant()` y se exige unicidad en SQL. La contraseña se conserva exactamente al verificarla.
- `PasswordHasher<Usuario>` de ASP.NET Core protege las contraseñas con PBKDF2 y salt aleatorio. Se actualiza el hash después de un login válido si el hasher lo solicita. No se implementa criptografía propia.
- JWT HS256, vida predeterminada de 15 minutos (configurable entre 1 y 60); sin refresh token. Se validan firma, algoritmo, emisor, audiencia y expiración, sin tolerancia adicional. Referencia: [validación JWT de ASP.NET Core](https://learn.microsoft.com/es-es/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-8.0).
- Administrador semilla optativo y sin credenciales predeterminadas. La contraseña inicial necesita 12–1024 caracteres como medida técnica; no representa una norma académica. La semilla es idempotente en arranques secuenciales: nunca cambia el hash, rol o estado de una cuenta existente. Si el nombre corresponde a una cuenta inactiva o no administradora, el arranque falla.
- No hay registro público ni endpoints de administración de usuarios en T1. El modelo admite los tres roles, pero solo se aprovisiona el administrador inicial.
- Credenciales inválidas, usuario inexistente e inactivo reciben el mismo error. Para un usuario inexistente también se verifica un hash sintético. Login válido envía `Cache-Control: no-store`.

## API implementada y propuesta para integración

Se conserva la ruta propuesta `POST /api/auth/login`. Los nombres y claims de abajo son la propuesta concreta de T1 para revisión con el equipo antes de conectar T2/T3; no se ha cambiado `docs/arquitectura/decisiones-y-contratos.md` ni se afirma un acuerdo compartido.

Solicitud JSON:

```json
{ "nombreUsuario": "<usuario configurado>", "password": "<contraseña configurada>" }
```

Respuesta 200:

```json
{
  "accessToken": "<JWT>",
  "tokenType": "Bearer",
  "expiresAtUtc": "<fecha ISO 8601 en UTC>"
}
```

Claims: `sub` (UUID del usuario), `jti`, `iat`, `name`, `role`, `iss`, `aud`, `nbf`, `exp`.
Al configurar YARP, usar `MapInboundClaims=false`, `NameClaimType=name` y `RoleClaimType=role`, con el mismo emisor, audiencia y secreto suministrados externamente. Esto es preparación para T2, aún pendiente de acuerdo e implementación.

Errores: 400 (`ValidationProblemDetails`) para campos obligatorios, longitudes inválidas o JSON mal formado; 401 (`ProblemDetails`) con título `Credenciales incorrectas` y sin token; 500 genérico para errores inesperados. OpenAPI disponible en `/swagger/v1/swagger.json` y UI en `/swagger`, solo en Development.

## Configuración externa

La API requiere `ConnectionStrings__Auth`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (al menos 32 bytes UTF-8, generados aleatoriamente). No se incluyen valores predeterminados para estos campos. La aplicación falla al arrancar si falta la conexión o la configuración JWT no es válida.

Opcionales:

| Variable | Predeterminado | Uso |
|---|---|---|
| `Jwt__ExpirationMinutes` | `15` | Caducidad, 1–60 minutos |
| `Database__ApplyMigrations` | `false` | Aplicar migraciones al arrancar |
| `SeedAdmin__Enabled` | `false` | Activar semilla |
| `SeedAdmin__NombreUsuario` | vacío | Usuario inicial |
| `SeedAdmin__Password` | vacío | Contraseña inicial |

Desactivar la semilla y retirar su contraseña de la configuración después de aprovisionar. Para producción, aplicar migraciones como paso controlado con permisos separados; no arrancar varias réplicas ejecutándolas a la vez. Usar HTTPS en el acceso del usuario a través del Gateway y una cuenta SQL de mínimo privilegio. Los JWT ya emitidos conservan validez hasta su expiración; T1 no ofrece revocación inmediata.

## Ejecutar en desarrollo con Docker Compose

Desde la raíz del repositorio, copiar `src/auth-service/.env.example` a un archivo privado, p. ej. `/tmp/auth-local.env`, restringirlo con `chmod 600` y rellenarlo. Para la contraseña SQL usar un valor aleatorio con mayúsculas, minúsculas, números y símbolos; para `AUTH_JWT_KEY`, generar al menos 32 bytes aleatorios (p. ej. `openssl rand -hex 32`). Rellenar también usuario/contraseña semilla, emisor y audiencia. El ejemplo no contiene secretos funcionales.

```bash
docker compose --env-file /tmp/auth-local.env -f src/auth-service/compose.yaml -p hu01-local up -d --build --wait
python3 tests/auth-service/smoke.py /tmp/auth-local.env hu01-local
docker compose --env-file /tmp/auth-local.env -f src/auth-service/compose.yaml -p hu01-local down
```

Compose es exclusivo del desarrollo de T1. SQL no publica puertos al host; la API solo publica `127.0.0.1:5081` (configurable mediante `AUTH_HTTP_PORT`). Conserva datos en un volumen nombrado del proyecto Compose. `down` conserva ese volumen; eliminarlo borra los usuarios locales y debe hacerse solo si se desea reiniciar ese entorno. El smoke verifica la firma JWT y reinicia únicamente auth-service para verificar semilla y persistencia.

El entorno usa SQL Server Developer con `sa`, certificado autofirmado aceptado y Swagger activo; estas opciones son para desarrollo local. Mantener contraseñas hexadecimales con un sufijo de complejidad evita problemas de interpolación de `$` y delimitadores `;` en el archivo de configuración. El lector del smoke acepta el formato simple `VARIABLE=valor`, sin comillas ni interpolación.

Sin Docker: exportar las variables de la API y ejecutar `dotnet run --project src/auth-service/AuthService --urls http://127.0.0.1:5081`, con SQL Server accesible y esquema previamente migrado, o con `Database__ApplyMigrations=true` para desarrollo. Las herramientas `dotnet ef` 8.0.30 pueden generar/aplicar migraciones mediante `--project src/auth-service/AuthService`; la fábrica de diseño permite generar SQL sin una clave JWT.

## Compilación y pruebas

```bash
dotnet restore Homologacion.sln
dotnet build Homologacion.sln --configuration Release --no-restore
dotnet test Homologacion.sln --configuration Release --no-build
```

xUnit usa SQLite relacional en memoria para aislar las pruebas de lógica y HTTP; no sustituye la verificación SQL Server de Compose. Las rutas `/_test/*` se agregan únicamente al host de pruebas y no existen en la API publicada. El workflow `auth-service.yml` ejecutará build/tests al abrir un PR hacia `develop`; no se ha ejecutado remotamente en esta sesión.

Evidencia y pendientes: `docs/sprint-1/evidencia-hu-01-t1.md`.
