# Arquitectura y contratos propuestos — Sprint 1

**Estado:** diseño inicial para coordinar a dos desarrolladores. Este archivo describe contratos propuestos; los endpoints no están implementados ni probados todavía.

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

## Clean Architecture obligatoria dentro de cada microservicio

El documento original exige estilo **Onion/Clean** por microservicio, con cuatro responsabilidades bien separadas. Crear proyectos independientes en cada servicio de negocio, sin introducir capas vacías en el Gateway.

Ejemplo para `homologacion-service` (repetir el patrón en `auth-service` y `catalogo-service`):

```text
services/
  homologacion-service/
    Homologacion.sln
    src/
      Homologacion.Domain/
      Homologacion.Application/
      Homologacion.Infrastructure/
      Homologacion.Api/
    tests/
      Homologacion.Domain.Tests/
      Homologacion.Application.Tests/
```

### Reglas de referencias y responsabilidades

- **Domain**: entidades, objetos de valor, políticas de negocio y comparaciones puras. No depende de otras capas ni de EF Core, HTTP, JWT o LLM.
- **Application**: casos de uso, DTO, validaciones de aplicación e interfaces (puertos) para repositorios y servicios externos; **solo referencia Domain**.
- **Infrastructure**: EF Core, `DbContext`, migraciones, persistencia SQL Server, JWT como infraestructura de autenticación y adaptadores; referencia Application y Domain según se necesite.
- **Api/Presentation**: controladores/endpoints, OpenAPI/Swagger, manejo HTTP y registro de dependencias. Referencia Application y, exclusivamente en su punto de composición, Infrastructure.
- La regla de dependencias es hacia el interior: Domain <- Application <- Infrastructure y Api como capa de entrada/composición.
- Cada microservicio posee datos, contratos y capas propias; nunca referenciar directamente el `Domain` de otro microservicio.
- Integración entre servicios mediante contratos HTTP/eventos y DTO, sin compartir `DbContext`.
- **Gateway YARP**: aplicación de infraestructura para enrutamiento/autorización, no un microservicio con reglas de negocio. **React**: frontend separado.

### Adaptación de código existente

Si Codex ya generó archivos, primero inventariarlos y analizar dependencias reales. Moverlos progresivamente a proyectos Clean, ajustar namespaces e inyección de dependencias, compilar y ejecutar pruebas tras cada movimiento. No reescribir lógica correcta ni perder cambios sin commit. El objetivo es preservar todos los criterios de aceptación del Sprint 1.

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

**Motor:** EvaluaciónEquivalencia (cuatro porcentajes, umbral, homologable, justificación, temas sin cobertura), ValidaciónAntigüedad y ValidaciónRequisitosCambio.

## Contrato de resultado de comparación (propuesta)

```json
{
  "asignaturaOrigen": "ejemplo-origen",
  "asignaturaReceptora": "ejemplo-receptora",
  "contenidoPct": 85,
  "profundidadPct": 80,
  "cargaHorariaPct": 100,
  "creditosPct": 100,
  "umbralPct": 80,
  "homologable": true,
  "justificacion": "Ejemplo de resultado de prueba, no veredicto académico oficial",
  "temasSinCobertura": []
}
```

**Cálculo de horas:** `min(100, horasOrigen / horasReceptora × 100)`; las horas de la receptora deben ser positivas.

**Cálculo de créditos:** `min(100, créditosOrigen / créditosReceptora × 100)`; los créditos de la receptora deben ser positivos.

**Contenido y profundidad:** se calculan con datos estructurados y un criterio explícito y verificable, a acordar con el equipo; sin evidencia suficiente devolver 'información incompleta'. La propuesta del documento tiene que validarse académicamente.

**Umbral configurable:** comprobar que *cada dimensión* alcance el valor, sin promediar las dimensiones para compensar una deficiencia.

## Acuerdos antes de la primera integración

- Tipo e identificador de carrera y asignatura.
- Nombres JSON en camelCase y errores HTTP estandarizados.
- JWT: emisor, audiencia, clave compartida vía variable de entorno, roles y expiración.
- Puertos y rutas internas de cada servicio para YARP.
- Estructura de la respuesta de comparación; qué procede con datos faltantes.
- Las fechas se guardan y validan como fechas, no cadenas con formatos locales.

## Seguridad y alcance
El repositorio es público: no subir datos personales de estudiantes, credenciales reales ni programas analíticos restringidos. La homologación oficial corresponde al Consejo Directivo; el sistema solo produce una sugerencia.
