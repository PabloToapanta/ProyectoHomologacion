# Evidencia técnica HU-36

Fecha: 03/11/2026. Responsable: Leslie, implementación asistida. Issue #37.
Rama local `feature/hu-36-clave-interna`, creada desde `origin/develop` en
`e44b939` inicialmente y actualizada a `e01767b` al incorporarse HU-10.
Los resultados se obtuvieron sobre el incremento local antes de publicarlo.
No se modificaron los archivos locales anteriores de la carpeta principal.
Leslie autorizó posteriormente commits atómicos y push de esta rama, sin merge.

## Publicación autorizada

El 03/11/2026 Leslie autorizó guardar y publicar únicamente HU-36. Los cambios
de HU-18 permanecen en su propio worktree/rama. Al preparar la publicación,
develop remoto ya estaba en `afacebc`; esta rama conserva la base verificada
`e01767b`. Las 449 pruebas y las 52 comprobaciones Docker no acreditan la
combinación con esos cambios posteriores. Debe comprobarse antes de integrar.
Los SHA publicados pueden consultarse en el historial de la rama; CI y revisión
cruzada se registrarán cuando realmente se ejecuten. No se autoriza ni realiza merge.

## Implementación comprobada

- Auth, Catálogo, Homologación y Solicitudes requieren `X-Internal-Key` antes de Swagger,
  autenticación y casos de uso. Error 403 genérico para clave ausente, incorrecta
  o repetida. Retiran la cabecera antes de continuar. Comparan hashes de tamaño
  fijo con `FixedTimeEquals`, con entrada acotada.
- Gateway incorpora su clave privada por transformación YARP, reemplazando el
  valor enviado por el navegador. Login conserva su acceso público a través del
  Gateway; las demás políticas JWT/rol permanecen activas.
- Las cuatro API y Gateway fallan al arrancar sin configuración válida, sin revelar
  la clave. Las API resuelven la protección antes de inicializar la base.
- Compose requiere `APP_SECRET_KEY`, sin valor predeterminado; `.env.example`
  contiene solo una variable vacía. No se modificó React ni se envía la clave al
  frontend. Los servicios internos no publican puertos en los Compose modificados.
- Se documentó 403 y el requisito interno en Swagger de cada API. No se añadieron
  dependencias HTTP a Domain/Application ni se modificó el motor determinista.

## TDD y resultados reales

Antes del código productivo se ejecutó el filtro `InternalAccessTests`:
28 pruebas fallaron (8 Auth, 8 Catálogo, 8 Homologación, 4 Gateway). Se observó
200 al consultar Swagger sin clave y ausencia de reemplazo de la cabecera del
cliente; tampoco se rechazaba el arranque sin clave. Se corrigió también un
dato de prueba que nombraba espacios sin contenerlos para que probara espacios
reales. Las pruebas TRX iniciales se conservaron en TestResults/hu36-red.

Durante la implementación se detectaron y corrigieron dos problemas del incremento:
validación demasiado temprana para la configuración final de WebApplicationFactory
y doble registro de servicios YARP. Se repitieron las suites después de corregirlos.
No se desactivó la protección ni se redujeron las expectativas de las pruebas previas.
Las factorías aportan ahora una clave sintética explícita; las pruebas de rechazo
la retiran o sustituyen deliberadamente.

Tras integrarse HU-10 se ejecutaron otras 11 pruebas fallidas antes de ampliar la
implementación: acceso directo a Solicitudes con JWT válido pero sin clave o con
clave incorrecta, guarda general, arranque y cliente HTTP. El cliente no añadía
la clave y la API admitía peticiones sin ella. Se corrigió y repitió la regresión
sobre develop actualizado, sin debilitar los permisos de Coordinador.

Entorno: Windows, SDK .NET 8.0.130 y Docker 28.5.1. SQL Server real en imagen
`mcr.microsoft.com/mssql/server:2022-latest`, con base y volumen efímeros exclusivos.
Las pruebas de API xUnit utilizan SQLite aislado donde corresponde; no se presentan
como pruebas SQL Server. La comprobación Docker sí ejecuta migraciones reales,
inicializa Auth/Catálogo y consulta ambos servicios mediante Gateway.

| Verificación | Resultado ejecutado |
|---|---|
| Restauración de Homologacion.sln y Catálogo | Correcta |
| Build Release Homologacion.sln | 0 errores, 0 advertencias |
| Build Release src/catalogo-service/Catalogo.sln | 0 errores, 0 advertencias |
| Auth.Application.Tests | 13 correctas |
| AuthService.Tests | 52 correctas |
| Gateway.Tests | 75 correctas |
| Homologacion.Domain.Tests | 71 correctas |
| Homologacion.Application.Tests | 32 correctas |
| Homologacion.Api.Tests | 93 correctas |
| Catalogo.Tests | 84 correctas |
| Solicitudes.Tests | 29 correctas, 1 omitida |
| Total xUnit final Release | 449 correctas, 0 fallidas, 1 omitida |
| Formato whitespace de archivos C# nuevos en ambas soluciones | Correcto |
| git diff --check | Correcto |

Comandos principales reproducibles:

```text
dotnet build Homologacion.sln -c Release --no-restore
dotnet build src/catalogo-service/Catalogo.sln -c Release --no-restore
dotnet test Homologacion.sln -c Release --no-build --no-restore
dotnet test src/catalogo-service/Catalogo.sln -c Release --no-build --no-restore
dotnet test src/solicitudes-service/Solicitudes.sln -c Release
python tests/security/hu36-smoke.py
```

Los TRX actuales están en TestResults/hu36-actual y
TestResults/hu36-final-solicitudes. Los anteriores de tres API se conservan en
TestResults/hu36-final (411 correctas). El resumen conservado en
[evidencias/hu36-20261103/resumen.json](evidencias/hu36-20261103/resumen.json)
contiene contadores y comprobaciones integradas sin secretos. Los logs locales
de build y pruebas permanecen ignorados por Git. La prueba omitida es
`MigracionDurabilidadYQuipuxUnicoInclusoConcurrente`, de HU-10, porque requiere
HU10_TEST_SQL. No se configuró contra bases persistentes. Su omisión no se
presenta como éxito; el workflow original de Solicitudes la ejecuta con SQL
aislado en CI, pendiente de ejecución sobre este incremento.

La comprobación Docker verifica configuración de Compose, ausencia de clave en la
configuración frontend, fallo real de arranque de las cuatro API y Gateway, login,
sobrescritura de cabecera, JWT obligatorio e inválido, consulta de carreras y
validación de antigüedad por Gateway. Desde la red interna prueba Swagger sin clave,
con clave incorrecta y con clave correcta en las cuatro API. Comprueba ausencia del
secreto aleatorio en logs y ausencia efectiva de bindings de puertos internos.
La ampliación crea únicamente dos carreras, una solicitud y una cuenta Coordinador
sintéticas dentro del proyecto efímero: verifica la llamada Solicitudes–Catálogo
con Bearer y clave privada, persistencia/lectura de solicitud y prohibición al
Administrador de suplantar el permiso de Coordinador. La cuenta conserva contraseña
con hash. No utiliza ni modifica bases existentes del equipo. Las 52 comprobaciones
pasaron y los contenedores/volumen exclusivos se eliminaron al terminar.

## Estado por criterio y pendientes

| Criterio del Issue #37 | Estado de este incremento |
|---|---|
| CA1: rechazo directo sin clave en cuatro servicios | Comprobado en las cuatro API, incluso Solicitudes con JWT válido |
| CA2: Gateway con clave, JWT y roles | Comprobado con el contrato JWT de develop; combinación JWE/HTTPS pendiente |
| CA3: configuración obligatoria sin filtración | Comprobado en las cuatro API y Gateway |
| CA4: clave solo en servidor, sin Git/logs/respuestas/frontend | Sin valor real versionado; comprobado en el entorno aislado; revisar de nuevo al integrar los otros PR |

1. Pablo: revisar el contrato de `hu-36-clave-interna.md` y combinar Program.cs,
   Compose y healthchecks con PR #47 (HTTPS) y #49 (JWE). No se integraron esos PR
   ni se afirma haber probado HTTPS/JWE sobre esta rama.
2. Edwin: revisar la integración ya implementada en Solicitudes y su cliente HTTP
   a Catálogo. Se mantiene JWT/roles y no se añade ningún bypass por origen interno.
3. CI: workflow `hu36-security.yml` preparado, todavía no ejecutado en GitHub.
4. Shirley: aceptación manual pendiente. Comprobar login y roles en el incremento
   combinado, rechazo directo desde la red interna y acceso por Gateway; verificar
   errores de configuración y evidencias sin mostrar claves ni JWT completos.

HU-36 está preparada para revisión del incremento local. La comprobación combinada
con HTTPS/JWE, CI, revisión cruzada y aceptación humana siguen pendientes. HU-18
se trabaja por separado; no se declara implementada en esta rama.
