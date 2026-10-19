# homologacion-service — HU-14 / T9–T10 / Issue #5

Motor determinista del Sprint 1. Issue: https://github.com/PabloToapanta/ProyectoHomologacion/issues/5.
No persiste evaluaciones, no consulta otras bases y no utiliza LLM ni mensajería.

## Capas y referencias

```text
src/homologacion-service/
├── Homologacion.Domain/
│   └── Equivalencias/           Porcentaje, decisión y estado explícito
├── Homologacion.Application/
│   └── Equivalencias/           caso de uso, DTO, validaciones e IUmbralEquivalencia
├── Homologacion.Infrastructure/
│   └── Configuration/          adaptador real de configuración externa
├── Homologacion.Api/
│   ├── Contracts/              contratos HTTP separados de los DTO internos
│   ├── Controllers/            POST /api/homologaciones/evaluar
│   ├── Security/               opciones JWT
│   └── Program.cs              composición, JWT, autorización y Swagger
├── examples/evaluar.json       fixture sintética; no representa un sílabo real
├── Dockerfile
└── compose.hu14.yaml

tests/
├── Homologacion.Domain.Tests/
├── Homologacion.Application.Tests/
├── Homologacion.Api.Tests/      incluye integración YARP real con TestServer
└── homologacion-service/smoke.py
```

Referencias de proyecto: `Application → Domain`; `Infrastructure → Application`;
`Api → Application, Infrastructure`; `Domain → ninguna capa ni paquete`.
Infrastructure recibe Domain transitivamente. Application tampoco contiene dependencias técnicas.
El único puerto de aplicación es `IUmbralEquivalencia`, implementado con Options en Infrastructure.
No se necesitan repositorios, EF, base de datos ni un adaptador del catálogo todavía.
Las comprobaciones de referencias de ensamblados forman parte de las pruebas.

## Reglas y límite académico

- Horas/créditos: `min(100, origen / receptora × 100)`, con cantidades positivas.
  Se compara antes de dividir para evitar desbordamiento; se usa `decimal`, sin redondear
  antes de decidir. La representación decimal tiene precisión finita.
- Contenido: propuesta del equipo `temas receptores cubiertos / total de temas receptores × 100`.
  Los identificadores son opacos, únicos y sensibles a mayúsculas. Las correspondencias se
  proporcionan explícitamente; no se deduce cobertura por igualdad de nombres o similitud textual.
  Varios temas de origen asociados al mismo receptor cuentan una sola vez. Un tema de origen
  puede cubrir varios receptores cuando así se declara; esa declaración requiere validación académica.
- Profundidad: **no hay una fórmula ni una escala acordada** para alinear niveles/resultados.
  No se inventa una ponderación, una escala de Bloom ni un porcentaje. El caso de uso devuelve
  `profundidadPct: null`, estado interno `InformacionIncompleta`, `homologable: false` y explicación.
  El cliente no puede aportar un porcentaje ni reducir el umbral desde el cuerpo HTTP.
- Decisión del dominio: las cuatro dimensiones válidas deben alcanzar individualmente el umbral.
  Una dimensión insuficiente no se compensa con otra. Porcentajes ausentes o inválidos producen
  `InformacionIncompleta`, nunca una homologación automática.
- Umbral inicial 80 %, configurable en `(0, 100]`. **Es propuesta del equipo, no normativa oficial**.
  Un umbral mal configurado impide arrancar. Cambiar configuración requiere reiniciar el servicio.

Las pruebas positivas de cuatro dimensiones al 80 % usan porcentajes **sintéticos** para verificar
la decisión pura. No prueban una fórmula de profundidad ni una aprobación institucional.
Actualmente ningún request HTTP puede producir `homologable: true` hasta definir e integrar ese criterio.
La prioridad ante dimensiones desconocidas es información incompleta, aunque una dimensión conocida sea insuficiente.

## Contrato HTTP implementado para HU-14

Ruta pública existente en Gateway: `POST /api/homologaciones/evaluar`.
`Content-Type: application/json`, `Authorization: Bearer <JWT>`.

La solicitud nueva se define localmente para comparar instantáneas; su adaptación al catálogo
queda como propuesta de integración, sin modificar el contrato compartido ni archivos de Leslie.

```json
{
  "origen": { "id": "origen-sintetico", "horas": 180, "creditos": 4, "temas": ["O1", "O2"] },
  "receptora": { "id": "receptora-sintetica", "horas": 144, "creditos": 3, "temas": ["R1", "R2", "R3", "R4", "R5"] },
  "correspondencias": [
    { "temaOrigen": "O1", "temaReceptor": "R1" },
    { "temaOrigen": "O1", "temaReceptor": "R2" },
    { "temaOrigen": "O2", "temaReceptor": "R3" },
    { "temaOrigen": "O2", "temaReceptor": "R4" }
  ]
}
```

Con el umbral por defecto, la respuesta conserva todas las claves propuestas en
`docs/arquitectura/decisiones-y-contratos.md`:

```json
{
  "asignaturaOrigen": "origen-sintetico",
  "asignaturaReceptora": "receptora-sintetica",
  "contenidoPct": 80,
  "profundidadPct": null,
  "cargaHorariaPct": 100,
  "creditosPct": 100,
  "umbralPct": 80,
  "homologable": false,
  "justificacion": "Información incompleta: criterio académico verificable de profundidad pendiente de definición. La propuesta del equipo requiere validación académica.",
  "temasSinCobertura": ["R5"]
}
```

Semántica de datos y errores:

| Caso | Resultado |
|---|---|
| Datos presentes y válidos, profundidad pendiente | 200, porcentaje desconocido `null`, explicación y `homologable: false` |
| `{}` o instantáneas/cantidades/temas ausentes | 200, información incompleta y porcentajes calculables conservados |
| `correspondencias: null` o ausente | cobertura desconocida; `contenidoPct: null` |
| `correspondencias: []` con temas válidos en ambas asignaturas | cobertura declarada cero, temas receptores sin cobertura listados |
| Sin evidencia de contenido | `temasSinCobertura: []`; no se afirma que los temas estén cubiertos, consultar `contenidoPct: null` |
| Horas/créditos presentes ≤ 0, temas nulos/vacíos/duplicados o referencias inexistentes | 400 `application/problem+json` |
| Cuerpo nulo, malformado o valores incompatibles con el modelo | 400 Problem Details de ASP.NET Core |
| JWT ausente, firma/emisor/audiencia/algoritmo/expiración inválidos | 401 |
| JWT válido con rol ajeno a los tres roles académicos | 403 |

No se añaden claves a la respuesta compartida. El estado explícito del dominio se representa
externamente con porcentaje nullable, `homologable` y `justificacion`.
`temasSinCobertura` contiene identificadores receptores, en el orden de entrada.
El cuerpo máximo en Kestrel es 1 MiB. Swagger está disponible en Development en
`/swagger` y `/swagger/v1/swagger.json`, incluyendo esquema Bearer y respuestas documentadas.

## Configuración y ejecución local

Variables para ejecutar la API directamente:

```text
Jwt__Issuer=<mismo emisor que Auth y Gateway>
Jwt__Audience=<misma audiencia>
Jwt__SigningKey=<misma clave externa, al menos 32 bytes UTF-8>
Equivalencia__UmbralPct=80
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://127.0.0.1:5083
```

```bash
dotnet run --project src/homologacion-service/Homologacion.Api
```

Se valida HS256, firma, emisor, audiencia, expiración obligatoria y token firmado, sin tolerancia
adicional de reloj. Roles: Administrador, Docente, Coordinador. Gateway y servicio validan JWT.
No hay secretos de ejemplo en Git ni se confía en cabeceras de identidad sin firma.

Compose usa las configuraciones de Auth y Gateway existentes y agrega el motor sin puerto público:

```bash
docker compose --env-file /ruta/privada/hu14.env -p hu14-local \
  -f src/auth-service/compose.yaml \
  -f src/gateway/compose.t2.yaml \
  -f src/homologacion-service/compose.hu14.yaml up -d --build
```

El archivo privado necesita las variables existentes `AUTH_SQL_PASSWORD`, `AUTH_JWT_ISSUER`,
`AUTH_JWT_AUDIENCE`, `AUTH_JWT_KEY`, `AUTH_ADMIN_USERNAME`, `AUTH_ADMIN_PASSWORD`,
`CATALOGO_SERVICE_URL` y `HOMOLOGACION_SERVICE_URL` (el overlay sustituye el último destino por
`http://homologacion-service:8080/`, pero Compose interpola primero el archivo T2).
`CATALOGO_SERVICE_URL` debe ser una URL sintácticamente válida: esta prueba no llama al catálogo.
Opcionales: `GATEWAY_HTTP_PORT` (5080), `AUTH_HTTP_PORT` (5081), `EQUIVALENCIA_UMBRAL_PCT` (80).
Los valores secretos se generan o suministran fuera del repositorio.

El overlay es de desarrollo; no constituye una configuración de publicación.
Las imágenes ejecutan el motor como usuario sin privilegios. No se agrega una base al motor.

## Verificación

Desde la raíz:

```bash
dotnet restore Homologacion.sln
dotnet build Homologacion.sln --configuration Release --no-restore
dotnet test Homologacion.sln --configuration Release --no-build
python3 tests/homologacion-service/smoke.py
```

El smoke necesita el stack levantado y `AUTH_ADMIN_USERNAME`, `AUTH_ADMIN_PASSWORD` exportados
sin imprimirlos; `GATEWAY_URL` por defecto es `http://127.0.0.1:5080` y el umbral esperado se lee
con `EQUIVALENCIA_UMBRAL_PCT` (80 por defecto). No registra el JWT ni la respuesta de login.
El workflow `homologacion-service.yml` ejecuta restore/build/test y conserva TRX; su ejecución
remota queda pendiente de autorización de push/PR. Ver evidencias y coordinación:

- [Evidencias T9–T10](../../docs/sprint-1/evidencia-hu-14-t9-t10.md).
- [Propuesta de coordinación](../../docs/arquitectura/coordinacion-homologacion-service.md).
