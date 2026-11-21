# HU-21 — Análisis de varias asignaturas de origen y receptoras (CE-3)

**Issue:** [#75](https://github.com/PabloToapanta/ProyectoHomologacion/issues/75)

**Estado:** alcance de los tres casos confirmado por Pablo; requiere cerrar las decisiones de distribución antes del plan de implementación.

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

La [respuesta del ingeniero del 20/11](decisiones-validacion-ingeniero.md#hu-21--agrupación-nm-ce-3) había acotado la implementación a N:1. Pablo confirmó un alcance más amplio que incluye 1:1 en conjunto, 1:N y N:M; **no se presenta la regla anterior como definición de N:M ni se codifica la ampliación sin resolver las decisiones académicas y obtener la confirmación funcional de Shirley**. La lectura del ingeniero tampoco sustituye un pronunciamiento institucional. Los resultados del sistema siguen siendo sugerencias.

## Criterios de aceptación en elaboración

### CA1 — Selección y trazabilidad

- **Given** una solicitud y un docente asignado, **when** elige asignaturas de origen registradas y receptoras de la carrera destino, **then** el sistema conserva los IDs seleccionados, sus vínculos explícitos y la evidencia que sustenta cada vínculo.
- **Given** una selección con IDs inexistentes, repetidos o ajenos a la solicitud/carrera, **when** se intenta analizar, **then** se rechaza con un mensaje concreto.
- **Given** varias parejas 1:1, **when** se analizan en el mismo caso, **then** se conservan los resultados de cada pareja por separado; presentarlas juntas no suma automáticamente horas ni créditos entre parejas.

### CA2 — Elegibilidad del origen

- **Given** las materias de origen seleccionadas, **when** se prepara el análisis, **then** se verifica individualmente aprobación, antigüedad de HU-15 y exclusión por tercera matrícula de HU-17 cuando corresponda. Una materia no elegible no aporta contenidos, horas ni créditos y se informa el motivo.
- **Given** información académica o documental insuficiente, **when** no se puede verificar la elegibilidad, **then** el análisis correspondiente queda en «información incompleta»; no se presume aprobación.

### CA3 — Contenidos y resultados

- **Given** correspondencias revisadas, vigentes y con evidencia, **when** se evalúa una receptora, **then** cada elemento receptor cubierto se cuenta una sola vez aunque tenga varios vínculos de origen. Se muestran los elementos cubiertos, no cubiertos y sus evidencias.
- **Given** un origen que aporta a varias receptoras, **when** se consultan sus vínculos, **then** el análisis debe mostrar a cuál receptora se atribuye cada aporte. La regla exacta para compartir o repartir un mismo elemento de origen está pendiente de decisión.

### CA4 — Horas, créditos y veredicto

- **Given** aportes asignados a cada receptora y unidades comparables, **when** se calcula su equivalencia, **then** se muestran horas y créditos atribuidos, totales de esa receptora y porcentajes acotados al 100 %. Los tres criterios existentes se comparan por separado con el umbral vigente de HU-03.
- **Given** unidades o sistemas de créditos distintos, cantidades faltantes o una distribución de aportes todavía no definida, **when** se solicita un veredicto, **then** se informa «información incompleta» en las receptoras afectadas; no se convierten unidades ni se asigna automáticamente el total de un origen a varias receptoras.
- **Pendiente:** decidir si horas y créditos se asignan enteros a una sola receptora, se reparten en cantidades explícitas o se usa otra regla académica aprobada. Sin esa decisión, CA4 no está listo para implementar 1:N/N:M.

### CA5 — Análisis e informe

- **Given** resultados de una agrupación, **when** los dos docentes revisan y se genera el informe, **then** se identifican los orígenes, receptoras, vínculos, aportes, porcentajes, evidencia, faltantes y decisiones docentes sin perder la trazabilidad de cada receptora.
- **Given** una agrupación, **when** se presenta, **then** muestra «Homologación por agrupación — regla de análisis pendiente de validación institucional» y deja claro que no es una decisión académica oficial.
- **Pendiente:** decidir si la revisión y decisión final son por receptora o por el conjunto completo. El modelo actual guarda análisis e informes por pareja 1:1.

### CA6 — Verificación

- **Given** el contrato académico y técnico cerrado, **when** se implementa, **then** las pruebas cubren: varias parejas 1:1 sin suma global, 1:N con temas distribuidos, N:M, cobertura sin doble conteo, aportes de horas/créditos, incompatibilidad de unidades, datos faltantes, elegibilidad por origen, integración con revisión docente e informe. OpenAPI documenta contratos y errores nuevos.

## Decisiones necesarias antes de implementar 1:N/N:M

| Decisión | Por qué hace falta | Quién la confirma |
| --- | --- | --- |
| Las tres situaciones de la tabla están incluidas. | Pablo lo confirmó; Shirley debe validar la ampliación frente a la respuesta previa del ingeniero y al compromiso del sprint. | Shirley |
| ¿Puede un origen aportar a varias receptoras? Si sí, ¿cómo se distribuyen contenidos, horas y créditos? | Evita atribuir el total de una materia varias veces. | Ingeniero/Coordinación con Shirley |
| ¿Puede una misma correspondencia o elemento de origen sustentar más de una receptora? | Define el control de doble conteo académico. | Ingeniero/Coordinación con Shirley |
| ¿La sugerencia y la decisión docente son por cada receptora o por el grupo completo? | Define persistencia, informe y doble revisión. | Shirley con Pablo y Leslie |
| ¿Un origen puede aparecer en dos grupos de la misma solicitud? | Define la restricción de uso y la concurrencia. | Ingeniero/Coordinación con Shirley |

Hasta cerrar estas decisiones, la spec describe el objetivo y los casos, pero **no está lista para implementar el algoritmo 1:N/N:M**. Las parejas 1:1 existentes conservan su comportamiento.

## Tareas del Sprint Backlog, pendientes de ajuste

| Tarea | Descripción del backlog | Horas originales | Ajuste requerido |
| --- | --- | ---: | --- |
| T62 | Motor de agrupación con TDD | 6 | Definir algoritmo N:M y pruebas tras acordar distribución y doble conteo. |
| T63 | Solicitud, análisis e informe por agrupación | 5 | Definir identidad del grupo y si el resultado es por receptora o por conjunto. |
| T64 | Interfaz de agrupación | 3 | Diseñar selección y visualización según el contrato acordado. |
| **Total** | **8 PH de historia** | **14** | **Reestimar tras cerrar el alcance N:M.** |

## Siguiente paso de coordinación

1. Shirley confirma la ampliación a las tres situaciones frente al acuerdo previo N:1 y el compromiso de 8 PH. El caso 1:1 ya existe como flujo individual; definir la vista conjunta que requiere HU-21.
2. Shirley lleva al ingeniero/Coordinación las preguntas sobre reparto de contenidos, horas y créditos y uso de un origen en varias receptoras. Registrar la respuesta en el Issue #75 y en la spec.
3. Con las reglas cerradas, ajustar CA3–CA5, reestimar T62–T64 y definir el contrato de API/persistencia. Después elaborar el plan de implementación y dividir archivos entre Pablo y Leslie.
4. Trabajar en la rama `feature/hu-21-agrupacion-nm`, con Draft PR hacia `develop` creada por Pablo. Edwin revisa; Shirley acepta funcionalmente tras pruebas, OpenAPI y ejecución integrada con Docker Compose.

## Fuera de alcance confirmado

- Inventar conversiones de horas/créditos, correspondencias académicas o reglas normativas no validadas.
- Modificar resultados históricos de parejas 1:1 o convertirlos en agrupaciones sin evidencia.
- Registro nuevo de programas analíticos externos (D2 de HU-19) y decisión académica oficial automática.
