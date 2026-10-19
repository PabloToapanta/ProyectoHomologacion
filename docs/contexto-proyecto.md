# Contexto de negocio y proyecto para Codex

Documento de consulta operativa derivado de la planificación académica «Sprint 0: Planificación Scrum + XP — Sistema de Homologación de Materias (FISEI-UTA)» (41 páginas). **No sustituye al PDF original**: cuando se requiera una interpretación jurídica o de sílabos, verificar la fuente con el equipo y docente.

## Propósito y límites
Sistema web de apoyo al análisis de homologación cuando una persona solicita cambio de carrera. Compara asignaturas aprobadas de origen contra la carrera receptora, considerando contenido, profundidad, horas y créditos, y permite generar una **sugerencia** fundamentada para la autoridad competente. **No** toma decisiones oficiales, aprueba solicitudes, matricula, asigna cupos ni modifica sistemas institucionales.

El diseño completo distingue homologación **interna UTA** y **externa** desde otras instituciones (incluidas extranjeras). Solicitudes originalmente llegan por **Quipux**, fuera de esta aplicación.

## Usuarios y proceso completo previsto
- Administrador: gestiona usuarios, roles, carreras, asignaturas, programas y parámetros.
- Coordinador de Carrera: registra la solicitud que llega por Quipux, asigna dos docentes y finaliza trámite.
- Docentes: revisan por separado el análisis automatizado, registran observaciones y remiten informe.
- Estudiante: **no utiliza el sistema**.
- Estados futuros de solicitud: Registrada → Docentes asignados → En análisis → En revisión docente → Informe remitido → Finalizada.
- El Consejo Directivo conserva la decisión académica final.

Este flujo completo aporta contexto, pero **solo implementar las 7 HU del Sprint 1**. No adelantar UI o lógica del flujo de solicitudes, docentes, agentes ni informes.

## Sprint 1 comprometido (cierre 21 de octubre de 2026)
**Objetivo:** un administrador autenticado puede registrar carreras y sus asignaturas, unidades y contenidos; un núcleo determinista calcula y valida las reglas de equivalencia, antigüedad y requisitos mínimos.

| HU | Funcionalidad | Responsable del código | Issue |
|---|---|---|---|
| HU-01 | Login por roles, JWT, Gateway, rutas protegidas | Pablo | #1 |
| HU-04 | Gestión de carreras y restricciones por duplicado | Leslie | #2 |
| HU-05 | Asignaturas por carrera, créditos, horas y nivel | Leslie | #3 |
| HU-06 | Unidades, contenidos y resultados de aprendizaje | Leslie | #4 |
| HU-14 | Comparar cuatro dimensiones y verificar umbral | Pablo | #5 |
| HU-15 | Aprobación y antigüedad de diez años | Pablo | #6 |
| HU-16 | Requisitos de cambios internos/externos | Pablo | #7 |

Equipo: Shirley = Product Owner; Pablo = Scrum Master y desarrollador; Leslie = desarrolladora; Edwin = análisis funcional, datos académicos, documentación y pruebas de aceptación.

Consultas normativas y criterios exactos se encuentran en `docs/sprint-1/criterios-aceptacion.md`. Las estimaciones originales suman **45 h** (21 h Pablo y 24 h Leslie), no las 43 h del cuadro resumen del PDF.

## Reglas de negocio planteadas en el documento

Estas reglas provienen de la planificación. **Las fórmulas y las interpretaciones legales pendientes se deben distinguir de disposiciones oficiales y confirmar con el docente.**

- **RN-01:** análisis comparativo de contenido, profundidad y carga horaria, con umbral propuesto del 80 % respecto de la receptora.
- **RN-02:** en movilidad académica, correspondencia de resultados de aprendizaje bajo el umbral aplicable.
- **RN-03:** materia de origen aprobada y con un máximo de diez años desde su aprobación; después, corresponde validación de conocimientos.
- **RN-04:** cambio interno: un período cursado y al menos una materia homologable; condición adicional de gratuidad según normativa.
- **RN-05:** cambio desde otra IES: al menos dos períodos y dos materias homologables, sin cambio anterior de carrera.
- **RN-06:** IES extranjera: requisitos de RN-05 y documentos apostillados aplicables.
- **RN-07:** regla de tercera matrícula según la malla receptora (se programa en Sprint 3, HU-17).
- **RN-08:** la carrera receptora debe garantizar el perfil de egreso; evaluación académica humana.
- **RN-09:** flujo institucional y decisión de autoridades, fuera del software para este sprint.
- **RN-10:** equivalencia de calificaciones configurable, fuera del incremento actual.

### Equivalencia — propuesta a validar
Cuatro porcentajes comparados respecto a la **asignatura receptora**:
1. Contenido: temas receptores cubiertos por origen / temas receptores.
2. Profundidad: alineación de nivel y resultados de aprendizaje; el método exacto es propuesta del equipo, no una fórmula normada verificada.
3. Horas: `min(100, horasOrigen / horasReceptora × 100)`.
4. Créditos: `min(100, creditosOrigen / creditosReceptora × 100)`.

La propuesta del equipo exige **cada dimensión >= umbral** (inicial 80 %), nunca un promedio compensatorio. Probar 79,9 %, 80 % y el cambio a 85 %.
Cuando faltan temas, resultados u horas/creditos válidos, devolver **información incompleta**, no simular un cálculo ni inventar datos.

### Antigüedad
Escribir casos TDD con fecha de aprobación válida, exactamente diez años, menos de diez, más de diez, estado aprobado/reprobado/no cursado, zona temporal consistente. Evitar aproximar diez años como 3650 días; comparar fechas de calendario.

### Cambio de carrera
- Interno: al menos un período cursado y al menos una asignatura homologable.
- Externo: al menos dos períodos y dos asignaturas homologables; sin cambios previos.
- Reglas sobre instituciones extranjeras y apostilla pueden requerir datos que este sprint aún no captura; explicitar pendiente si una API no los incorpora. No inventar estado de documentos.

## Datos académicos relevantes
Entidades de catálogo: Carrera (facultad, modalidad, estado), Malla (versión, estado), Asignatura (código, carrera, nivel, créditos y horas), UnidadCurricular (unidades y resultados de aprendizaje), Contenido (temas y orden).

Ejemplo de prueba mencionado en el documento: **Álgebra Lineal**, carrera Software, primer nivel, 3 créditos, 144 horas, cuatro unidades de 36 horas. **Los nombres exactos de los temas y resultados deben provenir de un programa analítico que el equipo proporcione**, no inventarse. Usar datos sintéticos etiquetados para las pruebas de reglas si no hay más información.

## Arquitectura del producto
- React + TypeScript + Vite.
- Gateway YARP; servicios ASP.NET Core 8: autenticación, catálogo, homologación, solicitudes, informes.
- SQL Server con propiedad lógica de datos por servicio.
- RabbitMQ reservado para eventos de negocio del flujo que se implementará después.
- Ollama o API alternativa con agentes en Sprint 2; el motor del Sprint 1 debe funcionar **sin un LLM**.
- xUnit, Docker Compose, GitHub Actions, Swagger; GitFlow y Conventional Commits en español.
- Clean/Onion propuesto internamente; mantener el diseño proporcional al plazo.

## Sprints siguientes (solo para contexto)
Sprint 2: crear solicitudes y asignar dos docentes, analizar cambios internos/externos mediante agentes, guardar resultados y visualizar análisis; parte de la carga de programas analíticos.
Sprint 3: revisión humana final, emisión y exportación PDF, administración, auditoría, casos especiales y pruebas finales.
Los entregables del Sprint 1 no deben presentarse como si incluyeran esas capacidades.

## Dónde buscar información específica
- `AGENTS.md`: normas de trabajo del agente.
- `docs/sprint-1/criterios-aceptacion.md`: comportamiento verificable de cada HU.
- `docs/sprint-1/planificacion-detallada.md`: tareas y estimaciones.
- `docs/arquitectura/decisiones-y-contratos.md`: rutas y contratos propuestos que ambos deben acordar.
- `docs/codex/arranque-pablo-leslie.md`: instrucciones de Codex y Git.
- PDF de 41 páginas: fuente original disponible con el equipo; **no se ha subido al repositorio público**. Para detalles no recogidos aquí, solicitar al humano la página correspondiente.
