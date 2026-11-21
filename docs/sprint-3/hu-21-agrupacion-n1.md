# HU-21 — Homologación por agrupación N:1 (CE-3)

**Issue:** [#75](https://github.com/PabloToapanta/ProyectoHomologacion/issues/75)  
**Estado:** especificación para revisión de Pablo, Leslie y Shirley; sin implementación ni aceptación funcional.  
**Sprint:** 3 (5–25 de noviembre de 2026). **Estimación:** 8 PH, 14 h de tareas.  
**Desarrollo:** Pablo y Leslie. **Revisión cruzada:** Edwin. **Aceptación funcional:** Shirley (PO).

## Historia de usuario

| Dato | Definición |
| --- | --- |
| Número de historia | HU-21 |
| Usuario | Docente asignado a la solicitud |
| Prioridad | Media |
| Riesgo | Alto: CE-3 aún carece de validación institucional y el modelo actual registra parejas individuales |
| Nombre | Homologación por agrupación N:1 |
| Puntos de historia | 8 PH |
| Descripción | Como docente asignado, quiero evaluar varias asignaturas aprobadas de origen frente a una asignatura receptora en una sola agrupación, para obtener una sugerencia trazable sin contar dos veces el mismo elemento receptor. |

El título histórico del backlog dice «N:M». La [respuesta del ingeniero del 20/11](decisiones-validacion-ingeniero.md#hu-21--agrupación-nm-ce-3) acotó el alcance implementable a **N:1**. Su lectura de fuentes públicas no sustituye un pronunciamiento de la Coordinación o la UTA: esta es una **regla de análisis del proyecto** y el resultado es una sugerencia, no una decisión académica oficial. La fecha límite original de consulta, lunes 16/11, ya pasó; la respuesta del 20/11 queda registrada y la validación institucional sigue pendiente.

## Criterios de aceptación

### CA1 — Crear una agrupación identificable

- **Given** una solicitud con dos o más asignaturas de origen registradas y una receptora de la carrera destino, y un docente asignado.
- **When** el docente selecciona los orígenes y la receptora para analizarlos juntos.
- **Then** el sistema registra una sola agrupación de esa solicitud, con identificador estable, IDs de todos los orígenes y exactamente una receptora. La consulta, los análisis de ambos docentes y el informe conservan la misma composición e identificador.
- **Given** una selección con un origen, ninguna receptora, varias receptoras o IDs repetidos.
- **When** se intenta crear una agrupación.
- **Then** se rechaza la operación con un mensaje explícito. El análisis individual 1:1 existente sigue disponible por su flujo actual. No se impone un máximo académico de orígenes.

### CA2 — Elegibilidad y uso único del origen

- **Given** los orígenes seleccionados.
- **When** se prepara el análisis.
- **Then** se verifica para cada uno su pertenencia a la solicitud, aprobación, antigüedad vigente de HU-15 y exclusión por tercera matrícula de HU-17 cuando corresponda. Cada exclusión muestra el origen y el motivo; no aporta cobertura, horas ni créditos.
- **Given** un origen ya usado en otra agrupación o pareja individual de la misma solicitud.
- **When** se intenta asignarlo de nuevo.
- **Then** se rechaza esa asignación. La regla se comprueba en el servidor y persiste ante peticiones concurrentes; el frontend solo ayuda a prevenirla.
- **Given** las exclusiones dejan menos de dos orígenes elegibles.
- **When** se solicita el análisis agrupado.
- **Then** no se convierte silenciosamente en una pareja 1:1: se informa que la agrupación no puede evaluarse y cuáles orígenes faltan o fueron excluidos.

### CA3 — Cobertura académica sin doble conteo

- **Given** inventarios académicos y correspondencias revisadas y vigentes entre cada origen elegible y la receptora.
- **When** el motor calcula contenido y resultados de aprendizaje.
- **Then** cada elemento receptor queda cubierto si al menos un origen tiene una correspondencia válida con evidencia hacia él; se cuenta una sola vez aunque haya varias correspondencias. Se muestran los elementos sin cobertura y la evidencia de los orígenes que sí aportaron cobertura.
- **Given** faltan programa, inventario, revisión vigente o evidencia necesaria.
- **When** no se puede establecer la cobertura.
- **Then** el criterio queda en «información incompleta» y se enumeran los datos faltantes; no se presume correspondencia.

### CA4 — Horas, créditos y decisión

- **Given** dos o más orígenes elegibles con cantidades positivas, misma unidad de horas y mismo sistema de créditos entre sí y con la receptora.
- **When** se evalúa la agrupación.
- **Then** se suman por separado horas y créditos de los orígenes elegibles, se comparan con los respectivos totales de la receptora, y cada porcentaje se limita a 100 %. Se conservan los aportes por origen y los totales utilizados.
- **Given** falta una cantidad o unidad, las unidades o sistemas difieren, o el total receptor no permite la comparación.
- **When** el motor evalúa.
- **Then** devuelve «información incompleta» con la causa, sin convertir unidades ni emitir un veredicto inferido.
- **Given** los tres criterios están completos.
- **When** se decide la sugerencia.
- **Then** cobertura académica, horas y créditos alcanzan **cada uno** el umbral vigente de HU-03 (inicialmente 80 %) para sugerir «Homologable»; de lo contrario se sugiere «No homologable». El resultado conserva la versión de parámetros usada.

### CA5 — Análisis docente e informe trazables

- **Given** una agrupación evaluada.
- **When** los docentes consultan o registran su análisis y se genera el informe de la solicitud.
- **Then** aparece como una sola unidad de revisión con todos sus orígenes, receptora, aportes, porcentajes, evidencia, datos faltantes y decisiones docentes. No se generan parejas independientes para los mismos orígenes como efecto secundario.
- **Given** un análisis o informe agrupado.
- **When** se presenta al usuario.
- **Then** muestra la leyenda **«Homologación por agrupación — regla de análisis pendiente de validación institucional»**. El informe mantiene su carácter de sugerencia y el flujo existente de doble revisión, acuerdo o desacuerdo.

### CA6 — Verificación y errores

- **Given** la implementación terminada.
- **When** se ejecutan pruebas de dominio, aplicación, API, interfaz e integración.
- **Then** quedan cubiertos: doble correspondencia hacia un elemento receptor, sumas y tope del 100 %, umbral en cada criterio, unidades incompatibles, datos faltantes, exclusión de un origen, uso único entre grupo y pareja, concurrencia, rechazo de 1:N/N:M y representación de la agrupación en análisis e informe. OpenAPI describe los contratos y errores nuevos.

## Alcance y reglas acordadas

1. Solo **N:1**, con N ≥ 2 orígenes elegibles de una solicitud y una receptora. No se implementan 1:N ni N:M general. Una petición de esos tipos se rechaza de forma explícita.
2. El análisis toma correspondencias revisadas y vigentes; no une textos de sílabos por similitud ni inventa evidencia. La cobertura se calcula sobre los elementos de la receptora, sin doble conteo.
3. Solo las asignaturas elegibles aportan horas y créditos. No hay factor de conversión ni máximo académico de asignaturas. Son válidos límites técnicos razonables de tamaño de petición si se documentan como tales, sin presentarlos como norma.
4. Si la información académica externa aún no está registrada (dependencia D2 de HU-19), el caso externo sigue en «información incompleta»; HU-21 no crea ese registro ni presupone sus datos.
5. Se conserva el motor determinista y el umbral versionado de HU-03. El agente puede explicar la sugerencia cuando haya datos suficientes, pero no cambia porcentajes ni veredicto.
6. Las solicitudes e informes existentes de parejas 1:1 siguen siendo legibles; una migración no los convierte en grupos ni inventa un historial de agrupación.

## Tareas propuestas

Las tareas conservan los identificadores y las horas del [Sprint Backlog](planificacion-detallada.md#5-sprint-backlog). **6 + 5 + 3 = 14 h; 8 PH para la historia completa.** La asignación concreta y los archivos se fijarán en el plan de implementación tras revisar esta spec.

| Tarea | Historia | Nombre | Entregable verificable | Horas |
| --- | --- | --- | --- | ---: |
| T62 | HU-21 | Motor N:1 con TDD | Elegibilidad por origen, cobertura sin doble conteo, sumas comparables, tope 100 %, decisión por tres criterios, faltantes y pruebas. | 6 |
| T63 | HU-21 | Solicitud, análisis e informe agrupados | Contrato y persistencia de grupo, uso único por solicitud, análisis de ambos docentes e informe con trazabilidad y leyenda; pruebas de API e integración. | 5 |
| T64 | HU-21 | Interfaz de agrupación | Selección N:1, prevención visible de reutilización, estados/errores, revisión e informe legibles; pruebas de interfaz. | 3 |
| **Total** | **HU-21** | | | **14** |

## Contrato que debe cerrar el plan de implementación

- **Identidad del grupo:** identificador estable en la solicitud, lista ordenada y sin duplicados de `asignaturaAprobadaId` y un `asignaturaReceptoraId`. La agrupación debe distinguirse de la pareja individual en API, persistencia, análisis y reporte.
- **Propiedad de datos:** Solicitudes valida pertenencia, elegibilidad y uso único; Homologación calcula y conserva la evaluación; el informe referencia la misma agrupación. No se duplican reglas de autoridad en el frontend.
- **Compatibilidad:** definir cómo leer y escribir grupos junto a registros históricos 1:1 y cómo se identifican las revisiones de los dos docentes sin alterar el significado de `asignaturaAprobadaId` en parejas existentes.
- **Errores:** diferenciar selección inválida, origen ya utilizado, origen no elegible, ausencia de evidencia y fuente no disponible; «información incompleta» pertenece al resultado académico, no sustituye errores de una petición mal formada.
- **Concurrencia:** asegurar que dos docentes o peticiones no puedan reservar simultáneamente un mismo origen en distintas unidades de revisión.

## Secuencia y definición de hecho

1. Pablo y Leslie revisan esta spec y cierran el contrato compartido de T62/T63 antes de editar servicios en paralelo. Shirley confirma los criterios funcionales; Edwin revisa el contrato que afecta análisis e informe.
2. Preparar el plan de implementación con archivos, orden de integración y propiedad por tarea. Trabajar en `feature/hu-21-agrupacion-nm` desde `develop`, con una **Draft PR** hacia `develop` que Pablo creará; ambos harán commits en esa rama para sus partes, coordinando archivos compartidos.
3. Aplicar TDD al motor, integrar solicitud/análisis/informe, luego interfaz; ejecutar regresión, integración con Docker Compose y actualizar OpenAPI/evidencia.
4. La historia se considera hecha solo con PR revisada por Edwin, pruebas en verde, ejecución integrada y aceptación funcional de Shirley. La aprobación de la spec o del contrato técnico no equivale a esa aceptación.

## Fuera de alcance

- Algoritmos 1:N o N:M general, conversiones de unidades o reglas de distribución de un origen entre varias receptoras.
- Pronunciamiento institucional, decisión académica automática o aprobación oficial de la homologación.
- Registro nuevo de programas externos, cambios a HU-15/HU-17/HU-03 o rediseño general del informe.
