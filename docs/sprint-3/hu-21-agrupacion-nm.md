# HU-21 — Análisis de varias asignaturas de origen y receptoras (CE-3)

**Issue:** [#75](https://github.com/PabloToapanta/ProyectoHomologacion/issues/75)

**Estado:** Pablo comunicó el 21/11/2026 que Shirley y el ingeniero revisaron y aceptaron este alcance funcional N:M. Pendientes: contrato técnico, reestimación, implementación y aceptación de la historia tras las pruebas.

**Sprint:** 3 · **Estimación vigente del backlog:** 8 PH, T62–T64 (14 h). El alcance N:M debe reestimarse antes de comprometer esas horas.
**Desarrollo:** Pablo y Leslie. **Revisión técnica:** Edwin. **Aceptación funcional:** Shirley (PO).

## Historia de usuario

| Dato | Definición |
| --- | --- |
| Número de historia | HU-21 |
| Usuario | Docente asignado a la solicitud |
| Prioridad | Media |
| Riesgo | Alto: distribución del aporte académico entre receptoras y compatibilidad con el modelo actual de parejas |
| Nombre | Análisis de agrupación N:M |
| Puntos de historia | 8 PH en el backlog, sujetos a revisión de alcance |
| Descripción | Como docente asignado, quiero analizar en una solicitud varias asignaturas cursadas frente a varias asignaturas receptoras, para conocer qué contenidos, horas y créditos respaldan cada posible homologación y obtener una sugerencia trazable. |

### Aclaración del alcance

Pablo aclaró que **N:M** significa analizar varias materias de origen y varias de destino en un mismo caso. Confirmó que HU-21 incluye las tres situaciones siguientes:

| Situación | Ejemplo | Qué se analiza |
| --- | --- | --- |
| Varias parejas 1:1 | Álgebra lineal → Álgebra lineal; Física I → Física; Cálculo I → Cálculo integral | Cada pareja tiene sus propios criterios; se presentan juntas en la solicitud. |
| 1:N | Una materia de origen → dos receptoras cuyos temas se distribuyen entre ellas | Hay que decidir qué parte del origen respalda a cada receptora. |
| N:M agrupado | Varias materias de origen → varias receptoras como conjunto | Hay que relacionar aportes y evitar contarlos dos veces. |

La [respuesta documentada del ingeniero del 20/11](decisiones-validacion-ingeniero.md#hu-21--agrupación-nm-ce-3) había acotado la implementación a N:1. Pablo comunicó el 21/11 que Shirley y el ingeniero revisaron y aceptaron el alcance ampliado descrito aquí: 1:1 en conjunto, 1:N y N:M. Esta actualización debe reflejarse también en el Issue #75 para que la decisión más reciente sea trazable. La lectura del ingeniero no sustituye un pronunciamiento institucional; los resultados del sistema siguen siendo sugerencias.

## Criterios de aceptación en elaboración

### CA1 — Selección y trazabilidad

- **Given** una solicitud y un docente asignado, **when** elige asignaturas de origen registradas y receptoras de la carrera destino, **then** el sistema conserva los IDs seleccionados, sus vínculos explícitos y la evidencia que sustenta cada vínculo.
- **Given** una selección con IDs inexistentes, repetidos o ajenos a la solicitud/carrera, **when** se intenta analizar, **then** se rechaza con un mensaje concreto.
- **Given** varias parejas 1:1, **when** se analizan en el mismo caso, **then** se conservan los resultados de cada pareja por separado; presentarlas juntas no suma automáticamente horas ni créditos entre parejas.
- **Given** el docente prepara un conjunto, **when** abre la pantalla de agrupación, **then** ve orígenes en filas y receptoras en columnas. Activa únicamente celdas con correspondencias revisadas y vigentes, declara horas y créditos por vínculo y ve, por cada origen, cuánto asignó y cuánto queda disponible. La misma tabla permite varias parejas 1:1, 1:N y N:M.

### CA2 — Elegibilidad del origen

- **Given** las materias de origen seleccionadas, **when** se prepara el análisis, **then** se verifica individualmente aprobación, antigüedad de HU-15 y exclusión por tercera matrícula de HU-17 cuando corresponda. Una materia no elegible no aporta contenidos, horas ni créditos y se informa el motivo.
- **Given** información académica o documental insuficiente, **when** no se puede verificar la elegibilidad, **then** el análisis correspondiente queda en «información incompleta»; no se presume aprobación.
- **Given** un origen ya pertenece a otra pareja o agrupación de la misma solicitud, **when** se intenta agregarlo a una unidad de análisis nueva, **then** se rechaza la operación, incluso ante peticiones concurrentes. Dentro de su agrupación sí puede vincularse a varias receptoras.
- **Given** una agrupación todavía sin análisis guardado, **when** el docente corrige sus materias, vínculos o aportes, o elimina el borrador, **then** se actualizan o liberan sus reservas de origen de forma atómica. Desde el primer análisis guardado, incluso si su estado es incompleto, la composición y las reservas ya no se editan ni eliminan.

### CA3 — Contenidos y resultados

- **Given** correspondencias revisadas, vigentes y con evidencia, **when** se evalúa una receptora, **then** cada elemento receptor cubierto se cuenta una sola vez aunque tenga varios vínculos de origen. Se muestran los elementos cubiertos, no cubiertos y sus evidencias.
- **Given** un origen que aporta a varias receptoras, **when** se consultan sus vínculos, **then** el análisis muestra a cuál receptora se atribuye cada contenido o resultado. Un mismo elemento de origen puede respaldar más de una receptora solo con correspondencias explícitas, revisadas, vigentes y trazables; dentro de cada receptora, su elemento de destino se cuenta una sola vez.
- **Given** un par origen→receptora sin correspondencias revisadas y vigentes en el catálogo, **when** se analiza la agrupación, **then** sus elementos no acreditan cobertura y se informa la falta de revisión. HU-21 no permite crear o aprobar correspondencias desde la pantalla de agrupación.

### CA4 — Horas, créditos y veredicto

- **Given** un origen que aporta a varias receptoras, **when** se registran sus aportes, **then** se asignan cantidades explícitas de horas y créditos a cada receptora; la suma asignada para cada magnitud no supera el total del origen. El servidor valida esos límites y conserva el aporte por origen y receptora.
- **Given** un aporte numérico, **when** se registra, **then** admite cero o un valor positivo con hasta dos decimales; no admite negativos ni precisión mayor. Las horas aprobadas del origen siguen siendo enteras, pero su reparto puede tener fracciones de hora. Un aporte ausente (`null`) significa «pendiente de asignación» y deja incompleto el análisis afectado; cero significa «no aporta esa magnitud» y produce 0 % para ese criterio cuando el total receptor es positivo.
- **Given** aportes explícitos y unidades comparables, **when** se calcula la equivalencia, **then** se muestran aportes, totales receptores y porcentajes acotados al 100 %. Los tres criterios existentes se comparan por separado con el umbral vigente de HU-03.
- **Given** unidades o sistemas de créditos distintos, cantidades faltantes o una distribución de aportes no declarada, **when** se solicita un veredicto, **then** se informa «información incompleta»; no se convierten unidades ni se asigna automáticamente el total de un origen a varias receptoras.
- **Given** todas las receptoras tienen datos completos, **when** se decide la sugerencia única, **then** el grupo es «Homologable» solo si **cada receptora** alcanza por separado el umbral en contenido, horas y créditos. Si al menos una receptora completa no alcanza algún criterio, el grupo es «No homologable» y se identifica cuál y por qué. No se promedian porcentajes entre receptoras.
- **Given** alguna receptora tiene información incompleta, **when** se decide la sugerencia única, **then** el grupo queda en «Información incompleta» y muestra los datos faltantes por receptora; no se infiere una aprobación global.

**Ejemplo de reparto:** si un origen aprobado tiene 100 horas y 4 créditos y se usa hacia dos receptoras, pueden declararse 60 horas/2,5 créditos para una y 40 horas/1,5 créditos para la otra. No se permite declarar 100 horas/4 créditos para ambas. Cada receptora se compara con su propio programa; si una no alcanza cualquiera de los tres criterios, el grupo completo no se sugiere homologable.

### CA5 — Análisis e informe

- **Given** resultados de una agrupación, **when** los dos docentes revisan y se genera el informe, **then** se muestra una sola sugerencia y una decisión docente por grupo, con detalle de orígenes, receptoras, vínculos, aportes, porcentajes, evidencia y faltantes de cada receptora.
- **Given** una agrupación, **when** se presenta, **then** muestra «Homologación por agrupación — regla de análisis pendiente de validación institucional» y deja claro que no es una decisión académica oficial.
- El grupo tiene un `grupoId` propio. Análisis, revisiones docentes e informe referencian ese ID. Las parejas 1:1 existentes conservan sus IDs y registros históricos; no se convierten en grupos ficticios.

### CA6 — Verificación

- **Given** el contrato académico y técnico cerrado, **when** se implementa, **then** las pruebas cubren: varias parejas 1:1 sin suma global, 1:N con temas distribuidos, N:M, cobertura sin doble conteo, aportes de horas/créditos, incompatibilidad de unidades, datos faltantes, elegibilidad por origen, integración con revisión docente e informe. OpenAPI documenta contratos y errores nuevos.

## Decisiones funcionales y contratos técnicos

### Contrato técnico resuelto 1 — aportes numéricos

- Cada vínculo origen→receptora registra `horasAportadas` y `creditosAportados` como decimales no negativos con **máximo dos cifras decimales**. Pablo confirmó el 21/11/2026 que se permiten fracciones de hora.
- `null` expresa que el reparto todavía no se declaró; `0` es un aporte conocido de cero. No se equiparan. Si un vínculo tiene un aporte pendiente, el análisis del grupo queda en «Información incompleta» con el origen, la receptora y la magnitud faltante.
- Un intento de analizar con aportes `null` devuelve los faltantes **sin guardar un análisis** ni congelar el borrador; el docente puede completar el reparto y volver a intentar. Otros análisis incompletos que sí se guarden congelan la composición desde ese primer guardado.
- Para cada origen, las sumas de horas y créditos de todos sus vínculos dentro del grupo deben ser menores o iguales a las cantidades aprobadas respectivas. Puede quedar una parte sin asignar. El servidor rechaza una suma excesiva, un negativo o más de dos decimales como entrada inválida; valida la suma con aritmética decimal, sin redondearla para hacerla pasar.
- Precisión definida para persistencia: horas `decimal(12,2)` y créditos `decimal(6,2)`, consistente con los créditos `decimal(6,2)` de Solicitudes. El algoritmo de agrupación trata un aporte conocido de cero como **0 %** frente a un total receptor positivo, sin cambiar el comportamiento histórico de las parejas 1:1. Los porcentajes se limitan al 100 % y se comparan con el umbral sin redondeo previo.

### Contrato técnico resuelto 2 — vínculos y evidencia

- La agrupación guarda vínculos entre IDs de asignaturas de origen aprobadas de la solicitud y IDs de receptoras. Para cada par se consulta la revisión existente del Catálogo; HU-21 no crea correspondencias académicas nuevas. Pablo confirmó el 21/11/2026 que se usarán únicamente las ya revisadas.
- Solo una revisión `Registrada` y `Vigente` acredita cobertura. Cada correspondencia usada conserva tipo (`Contenido` o `ResultadoAprendizaje`), ID de elemento de origen, ID de elemento receptor y ubicaciones de evidencia en ambos programas. Una misma fuente puede figurar en dos receptoras con vínculos separados; cada elemento receptor cuenta una vez en su propia evaluación.
- Al guardar un análisis, Homologación conserva una **fotografía de los vínculos y evidencias efectivamente usados**, junto con IDs de origen/receptora y resultados. Cambios posteriores del Catálogo no reescriben el análisis o informe ya guardados; un análisis nuevo consulta la revisión vigente de nuevo.

### Contrato técnico resuelto 3 — identidad, análisis, doble revisión e informe

- Pablo confirmó el 21/11/2026 una entidad de agrupación separada. Homologación crea un `grupoId` estable para una solicitud y registra por separado sus orígenes, receptoras y aportes origen→receptora. El ID de grupo no reemplaza `asignaturaAprobadaId` ni `asignaturaReceptoraId` en tablas de parejas existentes.
- El último análisis calculado de un grupo se guarda por `grupoId`, con desglose y fotografía de evidencia por receptora, versión de HU-03 y sugerencia única. Cada docente asignado guarda **una revisión por grupo**; la clave lógica es `(grupoId, docenteId)` y puede corregirla hasta la generación del informe, igual que en el flujo de parejas.
- El informe de la solicitud lista unidades individuales y grupos. Cada grupo aparece una vez, con sus dos revisiones docentes y el detalle por receptora. La generación sigue exigiendo las dos revisiones de cada unidad analizada y conserva el cierre que impide cambios docentes posteriores. Los informes y análisis 1:1 anteriores siguen siendo legibles sin migración semántica.

### Contrato técnico resuelto 4 — uso único, concurrencia y borradores

- Homologación mantiene una reserva por `(solicitudId, asignaturaAprobadaId)` con el tipo e ID de la unidad propietaria (pareja o grupo). La clave única de base de datos impide asignar el mismo origen a dos unidades. Al guardar por primera vez un análisis de pareja 1:1 se registra su reserva; al crear o modificar un grupo borrador se registran o actualizan todas las suyas en una transacción.
- Las operaciones que asignan, modifican o liberan orígenes de una solicitud se serializan en la base de Homologación y comprueban también las parejas históricas antes de aceptar un grupo. Un conflicto concurrente devuelve 409 sin grupo ni reservas parciales. No se eliminan ni reinterpretan análisis 1:1 anteriores, incluso si el legado ya contiene usos repetidos.
- Pablo confirmó el 21/11/2026 que el grupo puede editarse o eliminarse hasta antes del **primer análisis guardado**. El borrado libera sus reservas en la misma transacción. Después, composición y pertenencia son inmutables; puede ejecutarse otro análisis de la misma composición, y los docentes pueden corregir su revisión hasta generar el informe. Un intento con reparto `null` no guarda análisis para no congelar accidentalmente el borrador.

### Contrato técnico resuelto 5 — selección en la interfaz

- Pablo confirmó el 21/11/2026 una tabla de orígenes × receptoras para los tres casos. Una celda seleccionada representa un vínculo origen→receptora; el docente escribe `horasAportadas` y `creditosAportados` allí. Las celdas sin revisión vigente del Catálogo no son seleccionables y explican la razón.
- Por fila se muestran horas y créditos aprobados, suma asignada y saldo. Por columna se muestran cantidades receptoras, aportes recibidos y cobertura/veredicto cuando exista análisis. Los errores de servidor se asocian al vínculo o fila afectada, sin confiar solo en validación del navegador.
- La vista del análisis muestra el desglose por receptora y una sugerencia única del grupo. El borrador ofrece corregir/eliminar; tras el primer análisis guardado, la composición aparece de solo lectura.

### Contrato técnico resuelto 6 — rutas y mensajes de agrupación

Pablo confirmó el 21/11/2026 rutas propias para grupos. Las rutas actuales de parejas 1:1 conservan su contrato. Base nueva: `/api/homologaciones/solicitudes/{solicitudId}/agrupaciones` (docente asignado, con JWT; el Gateway aplica la misma protección académica que al análisis existente).

| Método y sufijo | Uso |
| --- | --- |
| `POST /` | Crear borrador; devuelve `201`, `grupoId` y composición. |
| `GET /` · `GET /{grupoId}` | Listar y consultar grupos de la solicitud con estado y último análisis, si existe. |
| `PUT /{grupoId}` · `DELETE /{grupoId}` | Reemplazar o eliminar un borrador **solo antes** del primer análisis guardado. La eliminación libera reservas. |
| `POST /{grupoId}/analisis` | Evaluar el grupo; devuelve sugerencia única y desglose por receptora. Si faltan aportes `null`, devuelve «Información incompleta» con `guardado: false`. |
| `GET /{grupoId}/revisiones` · `PUT /{grupoId}/revisiones` | Consultar ambas revisiones y guardar/corregir la del docente autenticado hasta generar el informe. |

El cuerpo de creación/reemplazo incluye `origenes[]` (IDs de asignaturas aprobadas), `receptoras[]` (IDs de catálogo) y `aportes[]` con `asignaturaAprobadaId`, `asignaturaReceptoraId`, `horasAportadas` y `creditosAportados`. Cada par aparece a lo sumo una vez y cada asignatura seleccionada participa en al menos un par. La respuesta incluye `grupoId`, estado de borrador/análisis, esos vínculos y, si existe, el último análisis con `configuracionVersionId`, estado/sugerencia del grupo, porcentajes y faltantes por receptora, y evidencias fotografiadas. El cliente no declara aprobación, revisión vigente ni veredicto: el servidor los obtiene y calcula.

Un grupo con exactamente un origen, una receptora y un vínculo usa el flujo 1:1 existente; para crear una agrupación se exige más de un vínculo o más de una asignatura en cualquiera de los lados. No se crean vínculos sin un origen y una receptora seleccionados.

Errores: `400` para IDs, pares o aportes inválidos; `401/403` para autenticación/asignación; `404` para grupo ajeno o inexistente; `409` para origen reservado, borrador congelado o conflicto concurrente; `428` si falta la versión exigida; `503` si la fuente autorizada no está disponible. Los faltantes académicos se devuelven en el resultado «Información incompleta», no como error de forma. OpenAPI documenta cada operación.

### Contrato técnico resuelto 7 — versión del borrador

- Pablo confirmó el 21/11/2026 control de versión para editar/eliminar borradores. `POST` de creación y `GET` de detalle devuelven un ETag fuerte y opaco de la versión del grupo. `PUT` y `DELETE` exigen `If-Match`; `POST /{grupoId}/analisis` también lo exige para evaluar exactamente la composición que vio el docente. Un ETag obsoleto responde `409` sin modificar reservas ni guardar análisis; uno ausente responde `428`.
- La escritura de nueva composición, reservas y versión es atómica. Una lectura de lista incluye la versión de cada grupo para que la interfaz pueda abrir el detalle vigente. Tras un `409`, la interfaz recarga el grupo y muestra la composición nueva antes de permitir reintentar; no mezcla automáticamente aportes de dos ediciones.

| Decisión | Estado comunicado el 21/11/2026 | Trabajo pendiente |
| --- | --- | --- |
| Las tres situaciones de la tabla están incluidas. | Pablo informa acuerdo de Shirley y el ingeniero. | Reflejar en #75 y reestimar el sprint. |
| Un origen puede aportar a varias receptoras; horas y créditos se asignan explícitamente por receptora y la suma no supera el total del origen. | Pablo informa acuerdo de Shirley y el ingeniero. | Validación y precisión resueltas en el contrato técnico 1; implementar y probar. |
| Un mismo elemento de origen puede sustentar más de una receptora con vínculos explícitos y trazables; cada elemento receptor se cuenta una vez. | Pablo informa acuerdo de Shirley y el ingeniero. | Representación y origen de evidencia resueltos en el contrato técnico 2; implementar y probar. |
| La sugerencia y decisión docente son únicas para el grupo; cada receptora debe cumplir por separado los tres criterios. | Pablo informa acuerdo de Shirley y el ingeniero. | Identidad y persistencia resueltas en el contrato técnico 3; implementar y probar. |
| Un origen pertenece a una sola unidad de análisis de la solicitud, individual o grupal; dentro de su grupo puede vincularse a varias receptoras. | Regla del 20/11 conservada en este alcance. | Reserva, concurrencia y corrección de borradores resueltas en el contrato técnico 4; implementar y probar. |

Las reglas funcionales ya permiten preparar el contrato técnico. La reestimación sigue pendiente: las 14 h del backlog se fijaron para el alcance anterior. Las parejas 1:1 existentes conservan su comportamiento. El acuerdo comunicado no equivale a aceptación de la implementación ni a pronunciamiento institucional.

## Tareas del Sprint Backlog, pendientes de reestimación

| Tarea | Descripción del backlog | Horas originales | Ajuste requerido |
| --- | --- | ---: | --- |
| T62 | Motor de agrupación con TDD | 6 | Definir algoritmo N:M y pruebas tras acordar distribución y doble conteo. |
| T63 | Solicitud, análisis e informe por agrupación | 5 | Definir identidad del grupo y si el resultado es por receptora o por conjunto. |
| T64 | Interfaz de agrupación | 3 | Diseñar selección y visualización según el contrato acordado. |
| **Total** | **8 PH de historia** | **14** | **Reestimar tras cerrar el alcance N:M.** |

## Siguiente paso de coordinación

1. Registrar en el Issue #75 el acuerdo N:M comunicado por Pablo y la fecha de revisión de Shirley y el ingeniero. La vista conjunta queda definida en el contrato técnico 5; el caso 1:1 individual existente conserva su flujo.
2. Pablo y Leslie revisan el contrato de API/persistencia del [plan técnico](plan-implementacion-hu-21.md), ajustan T62–T64 y registran la reestimación con Shirley.
3. Implementar por partes en la Draft PR con TDD, revisión cruzada y evidencia de cada criterio.
4. Trabajar en la rama `feature/hu-21-agrupacion-nm`, con Draft PR hacia `develop` creada por Pablo. Edwin revisa; Shirley acepta funcionalmente tras pruebas, OpenAPI y ejecución integrada con Docker Compose.

## Fuera de alcance confirmado

- Inventar conversiones de horas/créditos, correspondencias académicas o reglas normativas no validadas.
- Modificar resultados históricos de parejas 1:1 o convertirlos en agrupaciones sin evidencia.
- Registro nuevo de programas analíticos externos (D2 de HU-19) y decisión académica oficial automática.
