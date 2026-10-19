# API Gateway YARP — HU-01 / T2

Relacionado con [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1).
Un único proyecto ASP.NET Core 8 con YARP 2.3.0. Actúa como proxy HTTP y frontera de autenticación, sin persistencia, entidades ni reglas de homologación. No referencia proyectos de auth-service ni cambia su implementación Clean Architecture.

## Rutas existentes

Se conservan las rutas y métodos de `docs/arquitectura/decisiones-y-contratos.md`. No se transforman sus rutas ni se añaden endpoints de negocio.

| Método | Ruta | Destino | Autorización |
|---|---|---|---|
| POST | `/api/auth/login` | auth | Pública |
| GET, POST | `/api/carreras` | catalogo | JWT + rol reconocido |
| PUT, DELETE | `/api/carreras/{id}` | catalogo | JWT + rol reconocido |
| GET | `/api/carreras/{id}/asignaturas` | catalogo | JWT + rol reconocido |
| POST | `/api/asignaturas` | catalogo | JWT + rol reconocido |
| GET | `/api/asignaturas/{id}` | catalogo | JWT + rol reconocido |
| POST | `/api/asignaturas/{id}/unidades` | catalogo | JWT + rol reconocido |
| POST | `/api/homologaciones/evaluar` | homologacion | JWT + rol reconocido |
| POST | `/api/homologaciones/validar-antiguedad` | homologacion | JWT + rol reconocido |
| POST | `/api/homologaciones/validar-requisitos` | homologacion | JWT + rol reconocido |

El Gateway conserva método, ruta, query, cuerpo y respuesta del destino. Reenvía `Authorization` para permitir que los servicios también validen el token. YARP gestiona las cabeceras de transporte y proxy; no se generan identidades a partir de cabeceras `X-User-*`.

## JWT y roles

Usar los mismos valores externos de `Jwt__Issuer`, `Jwt__Audience` y `Jwt__SigningKey` que auth-service. La clave requiere al menos 32 bytes UTF-8 y no tiene valor predeterminado. Validación: firma obligatoria, emisor, audiencia, algoritmo exclusivamente HS256, expiración obligatoria y `nbf`, con `ClockSkew=0`. `MapInboundClaims=false`, `NameClaimType=name` y `RoleClaimType=role`, compatibles con los claims de T1.

La política `AcademicAccess` exige autenticación y uno de los roles `Administrador`, `Docente` o `Coordinador`, respetando mayúsculas. Los tres roles superan esta barrera de acceso; la documentación vigente no define una matriz de permisos por operación para Docente/Coordinador. Los servicios conservan la responsabilidad de sus autorizaciones funcionales. No se introdujeron reglas de administración de usuarios o de catálogo.

- Sin JWT o con JWT inválido: **401**, `WWW-Authenticate: Bearer`, sin contactar al destino.
- JWT válido sin un rol reconocido: **403**, sin contactar al destino.
- Ruta configurada sin política explícita: protegida por la política de respaldo.
- Solo `POST /api/auth/login` tiene política YARP `anonymous`; un bearer inválido no impide iniciar sesión con credenciales correctas.
- Ruta no configurada con JWT/rol válidos: **404**. La política de respaldo puede devolver 401/403 antes para peticiones no autenticadas/autorizadas.
- Fallo de conexión al destino después de superar la autorización: **502**.

El Gateway no emite tokens ni decide su duración; respeta `exp` de auth-service. Los errores 400/401 y el JSON de login siguen perteneciendo a auth-service. Los errores generados por el Gateway usan el middleware de ProblemDetails, sin revelar detalles del JWT rechazado.

## Configuración externa

`appsettings.json` contiene rutas y clusters, con direcciones vacías para exigir configuración por entorno. No contiene secretos ni destinos funcionales predeterminados.

| Variable | Contenido |
|---|---|
| `Jwt__Issuer` | Mismo emisor que Auth |
| `Jwt__Audience` | Misma audiencia que Auth |
| `Jwt__SigningKey` | Mismo secreto HS256 que Auth |
| `ReverseProxy__Clusters__auth__Destinations__primary__Address` | URL de auth-service |
| `ReverseProxy__Clusters__catalogo__Destinations__primary__Address` | URL de catalogo-service |
| `ReverseProxy__Clusters__homologacion__Destinations__primary__Address` | URL de homologacion-service |

Las direcciones deben ser absolutas HTTP(S), sin credenciales, query ni fragmento. Se recomienda terminar la URL base en `/`. El arranque rechaza configuración JWT insegura o destinos inválidos/faltantes sin mostrar sus valores. El Gateway no comprueba la disponibilidad de los servicios durante el arranque; así puede devolver 401 aunque un servicio académico esté caído o pendiente.

Después de exportar esas variables:

```bash
dotnet run --project src/gateway/Gateway.csproj --urls http://127.0.0.1:5080
```

En Development hay Swagger UI en `/swagger` y OpenAPI en `/swagger/v1/swagger.json`, accesibles sin JWT para documentación local. El documento se genera desde las rutas reales de YARP, describe destinos, parámetros de ruta, seguridad y errores. Los esquemas completos de los payloads se documentan en los microservicios; el Gateway no duplica sus DTO. En producción, los endpoints Swagger no se habilitan.

## Overlay opcional Docker Compose

`compose.t2.yaml` añade Gateway al Compose existente de T1 sin editar archivos de auth-service ni crear servicios académicos. Las rutas relativas del overlay se resuelven desde el primer archivo Compose, por eso su contexto de compilación es `../gateway`.

Copiar `.env.example` a un archivo privado, por ejemplo `/tmp/hu01-t2.env`, aplicar `chmod 600` y rellenar secretos, usuario inicial, emisor y audiencia. La clave JWT debe ser aleatoria (p. ej. `openssl rand -hex 32`); las contraseñas SQL deben cumplir complejidad. Los campos `AUTH_*` alimentan la configuración existente de Auth y la configuración compatible de Gateway.

```bash
docker compose --env-file /tmp/hu01-t2.env \
  -f src/auth-service/compose.yaml -f src/gateway/compose.t2.yaml \
  -p hu01-t2-local up -d --build --wait
```

Gateway publica solo `127.0.0.1:5080`, configurable mediante `GATEWAY_HTTP_PORT`. El Compose base sigue publicando Auth en loopback como entorno local de T1. `CATALOGO_SERVICE_URL` y `HOMOLOGACION_SERVICE_URL` se suministran externamente para la futura integración; los nombres del ejemplo requieren que esos destinos existan en la red correspondiente.

Para reproducir únicamente el smoke de T2, usar destinos deliberadamente inaccesibles, por ejemplo `http://127.0.0.1:1/` para ambos servicios académicos dentro del contenedor, y ejecutar:

```bash
python3 tests/gateway/smoke.py /tmp/hu01-t2.env
```

El smoke exige 502 después de un JWT válido porque no pretende ejecutar Catálogo/Homologación: comprueba aceptación del token y despacho, además de login real con SQL Server y rechazos 401. No usar este smoke sin ajustes contra servicios académicos ya integrados. El lector del archivo acepta `VARIABLE=valor` sin comillas/interpolación; usar secretos sin delimitadores `;` ni `$` evita problemas de conexión e interpolación en Compose.

Al terminar:

```bash
docker compose --env-file /tmp/hu01-t2.env \
  -f src/auth-service/compose.yaml -f src/gateway/compose.t2.yaml \
  -p hu01-t2-local down
```

`down` conserva el volumen SQL del proyecto. Borrarlo elimina sus usuarios de desarrollo. El overlay es para desarrollo; la integración institucional debe exponer el Gateway mediante HTTPS y mantener los microservicios en la red interna.

## Pruebas y evidencia

```bash
dotnet restore Homologacion.sln
dotnet build Homologacion.sln --configuration Release --no-restore
dotnet test Homologacion.sln --configuration Release --no-build
```

`Gateway.Tests` prueba YARP real con `WebApplicationFactory`. Su transporte de salida usa el auth-service existente con SQLite en memoria y respuestas técnicas simuladas para Catálogo/Homologación; no son servicios nuevos del producto. Incluye login extremo a extremo en el host de pruebas, 11 combinaciones académicas método/ruta, preservación de solicitudes, roles, fallos JWT, configuración, respaldo de autorización y documentación.

El workflow `gateway.yml` ejecutará la solución en PR hacia `develop`; no se ha ejecutado remotamente. Resultados efectivos y pendientes: `docs/sprint-1/evidencia-hu-01-t2.md`.

Referencias técnicas: [autenticación y autorización en YARP](https://learn.microsoft.com/aspnet/core/fundamentals/servers/yarp/authn-authz), [paquete YARP 2.3.0](https://www.nuget.org/packages/Yarp.ReverseProxy/2.3.0). La implementación utiliza las políticas de ASP.NET Core y el valor `anonymous` de YARP para la excepción de login.
