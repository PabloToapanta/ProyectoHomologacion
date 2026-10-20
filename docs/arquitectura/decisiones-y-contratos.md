# Arquitectura y contratos propuestos — Sprint 1

**Estado:** arquitectura de referencia. La sección HU-14 se actualiza por aclaración académica del equipo del 19/10/2026 y cambia su contrato. El estado de implementación de otras historias se consulta en sus evidencias; las rutas propuestas no prueban por sí solas su ejecución.

## Arquitectura de microservicios

```text
React + TypeScript
       |
       v
API Gateway (YARP) -- valida JWT
       |
       +-- auth-service          -> base de datos Auth (SQL Server)
       +-- catalogo-service      -> base de datos Catalogo (SQL Server)
       +-- homologacion-service  -> motor determinista sin dependencia del LLM

Sprints siguientes:
       +-- solicitudes-service   -> base de datos Solicitudes
       +-- informes-service      -> base de datos Informes
       RabbitMQ: eventos de negocio cuando se implementen los flujos asíncronos.
```

No se deben compartir entidades EF Core ni acoplar servicios mediante consultas directas a las bases de datos ajenas. Una instancia física de SQL Server puede alojar bases separadas. El motor no necesita persistencia de resultados hasta la historia HU-20 del Sprint 2.

## Rutas propuestas en el Gateway

| Método | Ruta | Servicio | HU |
|---|---|---|---|
| POST | `/api/auth/login` | auth-service | HU-01 |
| GET | `/api/carreras` | catalogo-service | HU-04 |
| POST | `/api/carreras` | catalogo-service | HU-04 |
| PUT | `/api/carreras/{id}` | catalogo-service | HU-04 |
| DELETE | `/api/carreras/{id}` | catalogo-service | HU-04 |
| GET | `/api/carreras/{id}/asignaturas` | catalogo-service | HU-05 |
| POST | `/api/asignaturas` | catalogo-service | HU-05 |
| GET | `/api/asignaturas/{id}` | catalogo-service | HU-05/HU-06 |
| POST | `/api/asignaturas/{id}/unidades` | catalogo-service | HU-06 |
| POST | `/api/homologaciones/evaluar` | homologacion-service | HU-14 |
| POST | `/api/homologaciones/validar-antiguedad` | homologacion-service | HU-15 |
| POST | `/api/homologaciones/validar-requisitos` | homologacion-service | HU-16 |

El equipo debe revisar y acordar estas rutas antes de implementar frontend y Gateway. CRUD adicional para editar unidades o asignaturas puede necesitarse según los criterios y la interpretación de 'gestión' del docente.

## Entidades esenciales

**Catálogo:** Carrera, Malla, Asignatura, UnidadCurricular, Contenido. Campos académicos del documento de planificación: carrera y facultad; nivel; créditos; horas; unidad; tema; resultado de aprendizaje; versión de malla si aplica.

**Autenticación:** Usuario y Rol (Administrador, Docente, Coordinador).

**Motor:** EvaluaciónEquivalencia (tres porcentajes, estado, umbral, homologable, justificación y contenidos/resultados sin cobertura), ValidaciónAntigüedad y ValidaciónRequisitosCambio.

## Contrato de comparación HU-14 — tres criterios (cambio incompatible)

Fuente del cambio: aclaración del docente transmitida por el usuario el 19/10/2026. Sustituye el
contrato anterior en `POST /api/homologaciones/evaluar`; no cambia las rutas ni la autenticación.
La confirmación para el equipo no se presenta como una disposición normativa no aportada.

**Fórmula:** `(contenidos receptores cubiertos + resultados receptores cubiertos) / (total contenidos receptores + total resultados receptores) × 100`.
Cada elemento pesa uno. La unión de correspondencias de varias asignaturas de origen aprobadas
se deduplica por identificador receptor dentro de cada categoría. Sin promedio de porcentajes ni
profundidad independiente. No se usa IA para determinar correspondencias.

**Solicitud:** `origenes[]`, `receptora`, `correspondenciasContenidos[]`,
`correspondenciasResultadosAprendizaje[]`. Cada asignatura incluye identificador, referencia de plan,
inventarios de contenidos/resultados, horas/unidad y créditos/sistema. Cada origen incluye `aprobada`.
Cada correspondencia identifica asignatura origen, elementos origen/receptor y ubicaciones de evidencia
en ambos planes. Los inventarios son instantáneas; no se consulta la base del catálogo.

**Respuesta:**

```json
{
  "asignaturasOrigen": ["origen-sintetico"],
  "asignaturaReceptora": "receptora-sintetica",
  "compatibilidadAcademicaPct": 80,
  "compatibilidadHorariaPct": 80,
  "compatibilidadCreditosPct": 80,
  "umbralPct": 80,
  "estado": "Homologable",
  "homologable": true,
  "justificacion": "Los tres criterios alcanzan individualmente el umbral de 80 %.",
  "contenidosSinCobertura": ["C9", "C10"],
  "resultadosAprendizajeSinCobertura": ["R5"],
  "informacionFaltante": []
}
```

`InformacionIncompleta` significa ausencia de evidencia/datos requeridos o comparabilidad sin regla;
`NoHomologable` significa tres porcentajes disponibles y alguno insuficiente; `Homologable` significa
los tres suficientes. El umbral es externo, inicialmente 80 %, y cada criterio debe cumplirlo por separado.
Un porcentaje desconocido es null. Las listas pendientes indican cobertura no acreditada, no una
inferencia semántica negativa. La asignatura receptora debe tener algún elemento válido entre las dos categorías.

Horas y créditos preservan `min(100, origen / receptora × 100)` para un solo origen, cantidades
positivas y unidad/sistema explícitamente iguales. Con varios orígenes no se suman cantidades;
horas/créditos son null, con explicación e información incompleta. Tampoco se convierten sistemas
externos por suposición. La validación estructural de evidencia no certifica la corrección académica
ni la autenticidad del plan: corresponde a revisión humana con datos autorizados.

**Rupturas:** desaparecen `origen`, `temas`, `correspondencias`, `contenidoPct`, `profundidadPct`,
`cargaHorariaPct`, `creditosPct`, `asignaturaOrigen` y `temasSinCobertura` del contrato anterior.
Se sustituyen por las estructuras/criterios anteriores. Los campos JSON desconocidos se rechazan
con HTTP 400, sin interpretar silenciosamente una solicitud antigua. No se aceptan porcentajes,
reglas de agregación ni umbral por cuerpo HTTP. JWT, roles y Problem Details se conservan.

Guía completa y ejemplo de migración: [README de homologación](../../src/homologacion-service/README.md).
Coordinación para Edwin/Leslie: [propuesta concreta](cambio-hu-14-tres-criterios.md). No se cambian
los contratos ni el código de HU-15/HU-16; cualquier consumidor futuro debe usar el DTO nuevo.

## Acuerdos antes de la primera integración

- Tipo e identificador de carrera y asignatura.
- Nombres JSON en camelCase y errores HTTP estandarizados.
- JWT: emisor, audiencia, clave compartida vía variable de entorno, roles y expiración.
- Puertos y rutas internas de cada servicio para YARP.
- Estructura de la respuesta de comparación; qué procede con datos faltantes.
- Las fechas se guardan y validan como fechas, no cadenas con formatos locales.

## Seguridad y alcance
El repositorio es público: no subir datos personales de estudiantes, credenciales reales ni programas analíticos restringidos. La homologación oficial corresponde al Consejo Directivo; el sistema solo produce una sugerencia.
