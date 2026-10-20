# homologacion-service — HU-14 / T9–T10 / Issue #5

Contrato actual: **tres criterios**, según aclaración del docente transmitida por el equipo el
19/10/2026. Sustituye el diseño de cuatro dimensiones. No se atribuye a una norma institucional
no aportada. Issue: https://github.com/PabloToapanta/ProyectoHomologacion/issues/5.

## Arquitectura y alcance

```text
Homologacion.Domain/Equivalencias/         fórmula combinada, porcentajes y decisión pura
Homologacion.Application/Equivalencias/    caso de uso, DTO, evidencia e IUmbralEquivalencia
Homologacion.Infrastructure/Configuration/ adaptador de configuración del umbral
Homologacion.Api/                         contratos, controlador, JWT, Swagger y composición
```

Referencias: Application → Domain; Infrastructure → Application; Api → Application/Infrastructure.
Domain no tiene paquetes ni dependencias técnicas. No se añade persistencia, EF, repositorios,
LLM, RabbitMQ ni acceso a bases ajenas. Se conserva la separación por funcionalidades para Edwin.
HU-14 recibe la constancia de aprobación; no determina aprobación, antigüedad ni requisitos de cambio.

## Reglas actuales

1. `compatibilidadAcademicaPct = (contenidos receptores cubiertos + resultados receptores cubiertos)
   / (total contenidos receptores + total resultados receptores) × 100`.
   Cada elemento tiene el mismo peso. No se promedian porcentajes de las dos categorías.
2. La cobertura puede provenir de varias asignaturas de origen aprobadas. Se cuentan identificadores
   receptores distintos por categoría; varias correspondencias hacia un elemento cuentan una sola vez.
   Contenido y resultado con el mismo identificador siguen siendo elementos de categorías diferentes.
3. Las correspondencias son explícitas y requieren el origen, ambos identificadores, referencia al
   plan analítico de cada asignatura y ubicaciones de evidencia dentro de esos planes. No se infieren
   coincidencias por texto ni se utiliza IA. Se valida la coherencia estructural de las referencias;
   el equipo debe verificar su sustento semántico en los planes autorizados. No se descarga ni abre
   ninguna referencia desde el servicio, ni se inventan sílabos.
4. Para **un único origen**, con cantidades positivas y unidades comparables:
   horas y créditos mantienen `min(100, origen / receptora × 100)`. Se usa `decimal` sin redondear
   antes de comparar; se compara antes de dividir para evitar desbordar. Precisión decimal finita.
   La comparabilidad exige el mismo identificador explícito de unidad horaria y de sistema de créditos,
   respectivamente (igualdad ordinal exacta, sin alias ni factores de conversión implícitos).
5. Para **varios orígenes**, se calcula la cobertura académica; horas y créditos quedan `null` porque
   no hay regla aprobada para su agregación. Tampoco se calcula una conversión de unidades o sistemas
   externos diferentes. No se aceptan totales agregados o porcentajes enviados por el cliente.
6. Los tres criterios deben alcanzar individualmente el umbral externo (80 % inicial, rango `(0,100]`).
   Profundidad deja de existir como requisito independiente. El umbral inválido impide arrancar.
   Cambiar la configuración requiere reiniciar el servicio.
7. Datos obligatorios ausentes o evidencia insuficiente: `InformacionIncompleta`, sin recomendación.
   Cuatro porcentajes anteriores y la profundidad independiente no forman parte de este contrato.

## Contrato HTTP nuevo — incompatible con la versión anterior

`POST /api/homologaciones/evaluar`, `Content-Type: application/json`, `Authorization: Bearer <JWT>`.
Ejemplo mínimo **sintético**, sin correspondencias académicas reales:

```json
{
  "origenes": [{
    "id": "O", "aprobada": true, "planAnalitico": "plan-sintetico-O-v1",
    "horas": 80, "unidadHoras": "hora-reloj", "creditos": 8, "sistemaCreditos": "sistema-sintetico-A",
    "contenidos": ["CO"], "resultadosAprendizaje": ["RO"]
  }],
  "receptora": {
    "id": "D", "planAnalitico": "plan-sintetico-D-v1",
    "horas": 100, "unidadHoras": "hora-reloj", "creditos": 10, "sistemaCreditos": "sistema-sintetico-A",
    "contenidos": ["CD"], "resultadosAprendizaje": ["RD"]
  },
  "correspondenciasContenidos": [{
    "asignaturaOrigen": "O", "elementoOrigen": "CO", "elementoReceptor": "CD",
    "evidencia": {"ubicacionOrigen": "unidad-sintetica/CO", "ubicacionReceptora": "unidad-sintetica/CD"}
  }],
  "correspondenciasResultadosAprendizaje": [{
    "asignaturaOrigen": "O", "elementoOrigen": "RO", "elementoReceptor": "RD",
    "evidencia": {"ubicacionOrigen": "unidad-sintetica/RO", "ubicacionReceptora": "unidad-sintetica/RD"}
  }]
}
```

Con el umbral 80, ese ejemplo devuelve:

```json
{
  "asignaturasOrigen": ["O"], "asignaturaReceptora": "D",
  "compatibilidadAcademicaPct": 100, "compatibilidadHorariaPct": 80, "compatibilidadCreditosPct": 80,
  "umbralPct": 80, "estado": "Homologable", "homologable": true,
  "justificacion": "Los tres criterios alcanzan individualmente el umbral de 80 %.",
  "contenidosSinCobertura": [], "resultadosAprendizajeSinCobertura": [], "informacionFaltante": []
}
```

`examples/evaluar.json` incluye una fixture sintética mayor: 8 contenidos y 4 resultados cubiertos,
frente a 10 y 5 requeridos. Devuelve los tres criterios al 80 %, deja C9/C10 y R5 sin cobertura y
sí recomienda con umbral 80. Con umbral 85 devuelve `NoHomologable`.

### Migración obligatoria de consumidores

| Anterior | Actual |
|---|---|
| `origen` singular | `origenes[]`, cada uno con `aprobada: true` y referencia al plan |
| `temas` | `contenidos` y `resultadosAprendizaje`, ambos inventarios explícitos |
| `correspondencias` / `temaOrigen` / `temaReceptor` | dos listas de correspondencias con asignatura de origen, elementos y evidencia |
| sin unidad o sistema explícito | `unidadHoras` y `sistemaCreditos` para acreditar comparabilidad |
| `asignaturaOrigen` en respuesta | `asignaturasOrigen[]` |
| `contenidoPct` / `profundidadPct` | `compatibilidadAcademicaPct`; profundidad eliminada |
| `cargaHorariaPct` / `creditosPct` | `compatibilidadHorariaPct` / `compatibilidadCreditosPct` |
| `temasSinCobertura` | dos listas: contenidos y resultados de aprendizaje sin cobertura acreditada |
| estado solo interno | `estado` e `informacionFaltante` explícitos en respuesta |

La ruta y JWT permanecen iguales. Se actualiza el contrato en la ruta existente por autorización
expresa del usuario; no se mantienen interpretaciones silenciosas del contrato antiguo ni se crea
una segunda API. Propiedades JSON desconocidas, incluidos campos antiguos o porcentajes/umbrales
aportados por el cliente, se rechazan con 400. Los consumidores y los futuros módulos de Edwin
que utilicen DTO antiguos deben migrar; no se cambia su código en esta tarea.

### Datos, estados y errores

- `estado`: `InformacionIncompleta`, `NoHomologable` o `Homologable`; `homologable` es true solo en el último.
- `null` en un inventario o lista de correspondencias significa desconocido. `[]` declara que la lista
  es conocida y vacía. Las dos categorías deben estar presentes; una puede estar vacía si la otra
  tiene elementos. Si el total receptor es cero, no se divide: información incompleta.
- Cada inventario tiene identificadores no vacíos y únicos. Orígenes con ID repetido se rechazan;
  contenidos con el mismo ID en asignaturas distintas se distinguen por el ID de origen.
- Evidencia ausente/vacía, referencia al plan ausente o constancia de aprobación desconocida/false:
  información incompleta. Las correspondencias sin evidencia/aprobación/planes no acreditan cobertura.
  El porcentaje académico se devuelve null cuando falta información académica obligatoria.
- Las listas de elementos sin cobertura incluyen todo lo que no tiene cobertura acreditada; no
  implican un juicio semántico negativo. Conservan el orden del inventario receptor.
- Una dimensión desconocida mantiene estado incompleto aunque otra sea insuficiente. Con los tres
  porcentajes válidos, cualquiera inferior al umbral produce NoHomologable; no hay compensación.
- `{}`: 200 incompleto. Cuerpo nulo/malformado, IDs referenciados inexistentes, inventarios inválidos,
  cantidades proporcionadas ≤0 o campos desconocidos: 400 `application/problem+json`.
  Un inventario ausente no prueba que un elemento sea inexistente: se informa incompletitud.
- Sin JWT o con firma/emisor/audiencia/algoritmo/expiración inválidos: 401; rol ajeno: 403.
  Roles permitidos: Administrador, Docente y Coordinador. API y Gateway validan el token.

## Configuración, Swagger y pruebas

Variables de API directa: `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (externa, mínimo 32 bytes
UTF-8), `Equivalencia__UmbralPct` (80), `ASPNETCORE_ENVIRONMENT=Development`,
`ASPNETCORE_URLS=http://127.0.0.1:5083`. Se conserva HS256 y validación compatible con Auth/YARP,
expiración obligatoria y cero tolerancia adicional de reloj. No hay secretos en Git.

```bash
dotnet run --project src/homologacion-service/Homologacion.Api
dotnet restore Homologacion.sln
dotnet build Homologacion.sln --configuration Release --no-restore
dotnet test Homologacion.sln --configuration Release --no-build
```

Swagger en Development: `/swagger` y `/swagger/v1/swagger.json`, esquema Bearer y modelos actuales.
Límite de cuerpo en Kestrel: 1 MiB. TestServer no aplica ese límite y emite advertencias al respecto.

Compose (sin base ni puerto público para el motor):

```bash
docker compose --env-file /ruta/privada/hu14.env -p hu14-local \
  -f src/auth-service/compose.yaml -f src/gateway/compose.t2.yaml \
  -f src/homologacion-service/compose.hu14.yaml up -d --build
python3 tests/homologacion-service/smoke.py
```

El archivo privado requiere las variables de Auth existentes: `AUTH_SQL_PASSWORD`, `AUTH_JWT_ISSUER`,
`AUTH_JWT_AUDIENCE`, `AUTH_JWT_KEY`, `AUTH_ADMIN_USERNAME`, `AUTH_ADMIN_PASSWORD`, además de
`CATALOGO_SERVICE_URL` y `HOMOLOGACION_SERVICE_URL`. El overlay sustituye el último destino por el
servicio interno, pero T2 interpola primero su variable. La URL de catálogo no se consulta en el smoke.
Opcionales: `GATEWAY_HTTP_PORT` (5080), `AUTH_HTTP_PORT` (5081), `EQUIVALENCIA_UMBRAL_PCT` (80).
El smoke necesita credenciales exportadas y `GATEWAY_URL` (por defecto localhost:5080); no imprime JWT.

## Documentación y pendientes

- [Cambio académico y coordinación](../../docs/arquitectura/cambio-hu-14-tres-criterios.md).
- [Evidencias actuales](../../docs/sprint-1/evidencia-hu-14-tres-criterios.md).
- [Estructura para Edwin y Leslie](../../docs/arquitectura/coordinacion-homologacion-service.md).

Pendientes académicos: cómo agregar horas/créditos de varios orígenes, comparabilidad/conversión de
unidades y sistemas distintos, y verificación de correspondencias con planes reales autorizados.
Profundidad independiente ya no es un pendiente: quedó sustituida por la fórmula combinada confirmada.
Pendientes de entrega: revisión cruzada, aceptación de Shirley y actualización coordinada del Issue remoto.
