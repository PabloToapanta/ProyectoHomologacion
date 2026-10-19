# Evidencia HU-01 — T2: Gateway YARP

Fecha: 19 de octubre de 2026 (America/Guayaquil). Vinculado al [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1). Ejecución técnica asistida por Codex para Pablo; pendiente de revisión cruzada y aceptación de Shirley.

## Punto de partida y alcance

Git estaba limpio en `feature/hu-01-autenticacion`, HEAD `05ee1b3`, sincronizado con `origin` al comenzar. Se revisaron AGENTS.md, contexto, contratos, criterios de HU-01 y el Issue #1 accesible en GitHub.

Se incorporó un proyecto ASP.NET Core 8 con YARP 2.3.0, sin capas adicionales ni referencias a proyectos de negocio. **No se modificó ningún archivo de auth-service ni de sus pruebas existentes.** Se conservaron sus contratos JSON, JWT y rutas. Tampoco se modificó el documento compartido de contratos.

## Archivos y decisiones

- `src/gateway/Gateway.csproj`, `Program.cs`: proxy, middleware JWT, políticas y composición en un proyecto único.
- `src/gateway/Security/GatewayJwtOptions.cs`: validación de opciones compatibles con T1; sin secretos predeterminados ni dependencia de Infrastructure de Auth.
- `src/gateway/appsettings.json`: 10 patrones YARP que representan 12 combinaciones método/ruta del contrato: login público y 11 operaciones académicas protegidas; tres clusters con destinos externos.
- `src/gateway/GatewayConfiguration.cs`: exige destinos HTTP(S) válidos para auth, catalogo y homologacion; errores sin valores sensibles.
- `src/gateway/OpenApi/ProxyDocumentFilter.cs`: documentación de transporte y seguridad generada desde las rutas configuradas, sin duplicar DTO de negocio.
- `src/gateway/Dockerfile`, `.dockerignore`, `.env.example`, `compose.t2.yaml`: compilación y overlay local opcional que reutiliza el Compose de T1 sin editarlo. Solo añade Gateway.
- `tests/Gateway.Tests/`: proyecto xUnit con 59 casos de integración/configuración y prueba de ausencia de referencias del Gateway a Auth.
- `tests/gateway/smoke.py`: comprobación reproducible de Gateway + Auth + SQL Server reales.
- `Homologacion.sln`: añade Gateway y Gateway.Tests a los seis proyectos previos.
- `.github/workflows/gateway.yml`: CI preparado para PR hacia develop, sin ejecución remota en esta sesión.
- `src/gateway/README.md` y este documento: configuración, decisiones, comandos, evidencia y límites.

La política académica exige JWT válido y uno de los tres roles definidos. Los tres roles reconocidos superan la frontera del Gateway; no se creó una matriz de permisos CRUD que no estuviera acordada. Un token válido sin rol reconocido devuelve 403, mientras que JWT ausente/inválido devuelve 401. La política de respaldo protege rutas sin una política explícita y login usa `anonymous` de YARP.

Solo se configuran las rutas/métodos del Sprint 1. No hay consultas de negocio, registro de usuarios, solicitudes, informes, RabbitMQ ni capacidades de Sprint 2/3 en el Gateway.

## TDD y resultados

1. Línea base antes de añadir Gateway: **56 aprobadas**, 0 fallidas (43 AuthService.Tests + 13 Auth.Application.Tests).
2. Primeras pruebas contra Gateway vacío: **40 fallidas y 3 aprobadas**, 43 casos; los fallos detectaban ausencia de enrutamiento/autenticación. Registro temporal: `/tmp/hu01-t2-red.log`.
3. Implementación de YARP, JWT y políticas. Se corrigieron comparaciones en el host de pruebas para capturar el cuerpo esperado antes de consumir el stream HTTP, y la expectativa OpenAPI: una operación anónima puede omitir `security` al no existir seguridad global. Se eliminó también una advertencia de parámetro de teoría no utilizado. La clave sintética del fixture se amplió para generar un HS384 criptográficamente válido y comprobar su rechazo por algoritmo, sin confundirlo con una clave demasiado corta.
4. Compilación y pruebas finales en verde, con evidencia TRX separada automáticamente por ejecución/proyecto.

| Comprobación ejecutada | Resultado real |
|---|---|
| `dotnet restore Homologacion.sln --disable-parallel` | Correcto para 8 proyectos |
| `dotnet build Homologacion.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false` | Correcto, **0 advertencias, 0 errores** |
| `dotnet test Homologacion.sln --configuration Release --no-build -m:1 --logger trx --results-directory /tmp/hu01-t2-test-results` | **115 aprobadas, 0 fallidas, 0 omitidas**: Auth 43, Application 13, Gateway 59 |
| Consulta NuGet `dotnet list <ruta-absoluta>/src/gateway/Gateway.csproj package --vulnerable --include-transitive` | No reportó paquetes vulnerables con las fuentes consultadas en esta fecha |
| Docker Compose base T1 + overlay T2, proyecto temporal `hu01-t2-verification` | Construcción y arranque correctos de Gateway, Auth y SQL Server |
| `python3 tests/gateway/smoke.py /tmp/hu01-t2.env` | Las cinco comprobaciones del smoke pasaron |
| `git diff --name-only -- src/auth-service tests/AuthService.Tests tests/Auth.Application.Tests` | Vacío: Auth y sus pruebas existentes permanecen sin cambios |
| `git diff --check` | Sin errores en diferencias rastreadas; archivos nuevos revisados antes de entrega |

Los runners y Docker utilizaron los permisos de la sesión necesarios para sockets/descargas. Los contenedores, volumen y credenciales aleatorias del entorno de verificación se retiran al terminar. No se imprimieron ni incorporaron a Git contraseñas, claves o JWT del smoke.

### Cobertura efectiva de las 59 pruebas de Gateway

- Las 11 operaciones académicas documentadas devuelven 401 sin token, con desafío Bearer y sin contactar a ningún destino.
- Las mismas 11 operaciones con JWT válido se reenvían al cluster correspondiente, conservando método, ruta, query, cuerpo y Authorization.
- Login público a través de YARP con el auth-service existente: devuelve JWT y permite luego acceder a una ruta académica; funciona incluso si llega un bearer inválido al login.
- Credenciales incorrectas y cuerpo de login inválido conservan los errores 401/400 del auth-service sin emitir token.
- Rechazo 401 de token malformado, firma distinta, emisor/audiencia incorrectos, expirado, todavía no vigente, sin expiración, sin firma y firmado con HS384 en vez de HS256.
- Aceptación de Administrador, Docente y Coordinador; rechazo 403 de rol ausente, desconocido o con mayúsculas incorrectas, sin contactar al destino.
- Cabeceras de identidad forjadas no sustituyen el JWT.
- Solicitudes, informes y registro público no se reenvían: con JWT/rol válidos devuelven 404.
- Configuración insegura impide el arranque, destinos inválidos/faltantes se rechazan sin exponer valores, destinos HTTP/HTTPS externos son aceptados.
- Una ruta sin política explícita sigue protegida por la política de respaldo.
- OpenAPI declara las rutas y los requisitos de seguridad; el ensamblado de Gateway no referencia Auth.

### Smoke Docker real

```text
PASS: login público Gateway -> auth-service -> SQL Server devuelve JWT
PASS: rutas académicas sin JWT o con JWT inválido devuelven 401
PASS: Gateway conserva errores 401/400 de auth-service sin emitir token
PASS: JWT real aceptado; destinos académicos inaccesibles devuelven 502, no 401
PASS: documentación OpenAPI del Gateway disponible
```

Catálogo y Homologación no se levantaron ni se simularon como microservicios del producto. En este smoke se configuraron destinos deliberadamente inaccesibles; el 502 posterior a un JWT válido comprueba aceptación y despacho. No certifica funcionalidad académica integrada. Las respuestas exitosas de esos destinos se cubren mediante transporte simulado en las pruebas xUnit de YARP.

## Criterios de aceptación HU-01

| Criterio | Estado después de T2 |
|---|---|
| Credenciales correctas: administrador recibe JWT y entra al panel | JWT recibido a través del Gateway verificado en pruebas y Docker; panel pendiente de T3 |
| Credenciales incorrectas: error y ningún token | API a través del Gateway verificada; mensaje de pantalla pendiente de T3 |
| Ruta protegida sin JWT válido: Gateway responde 401 | **Verificado** en xUnit para las 11 operaciones académicas y en el smoke Docker |

T2 queda implementada y verificada técnicamente. La historia completa sigue pendiente de T3, integración con servicios académicos reales, revisión/CI remoto y aceptación de la Product Owner. No se cerró ni modificó el Issue. Durante la implementación y verificación no se realizaron commits, pushes ni merges. Posteriormente, Pablo autorizó commits atómicos y el push de la rama; esta autorización no implica aceptación de la historia ni autoriza merges.
