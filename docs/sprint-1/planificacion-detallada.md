# Planificación detallada del Sprint 1

**Proyecto:** Sistema de Homologación de Materias (FISEI-UTA)  
**Fecha de planificación original:** 15–21 de octubre de 2026  
**Replanificación operativa:** 19–21 de octubre de 2026  
**Duración disponible en la replanificación:** lunes, martes y miércoles.  
**Product Owner:** Shirley Amaguaña  
**Scrum Master:** Pablo Toapanta  
**Desarrolladores de código:** Pablo Toapanta y Leslie Coello  
**Documentación, apoyo de producto y QA:** Shirley Amaguaña y Edwin Caraguay

> Basado en el documento académico «Sprint 0: Planificación Scrum + XP — Sistema de Homologación de Materias». Este archivo no certifica que el código esté terminado. Los responsables de ejecución se redistribuyeron por disponibilidad del equipo; las siete historias comprometidas permanecen en el Sprint 1.

## Sprint Goal

Que un administrador autenticado pueda mantener el catálogo de carreras y asignaturas con unidades y contenidos curriculares; además, que el sistema disponga de un núcleo determinista que calcule equivalencias y valide antigüedad y requisitos mínimos de cambio, probado con TDD.

## Compromiso: 7 historias y 26 PH

| Historia | Descripción | Prioridad | PH | Responsable de código |
|---|---|---|---:|---|
| HU-01 | Iniciar sesión con rol | Alta | 3 | Pablo |
| HU-04 | Gestionar carreras | Alta | 3 | Leslie |
| HU-05 | Gestionar asignaturas | Alta | 5 | Leslie |
| HU-06 | Gestionar unidades, contenidos y resultados | Alta | 5 | Leslie |
| HU-14 | Calcular equivalencia de asignaturas | Alta | 5 | Pablo |
| HU-15 | Validar aprobación y antigüedad | Alta | 2 | Pablo |
| HU-16 | Validar mínimos de cambio interno/externo | Alta | 3 | Pablo |
| **Total** | | | **26** | |

## Tareas técnicas del documento original

| Tarea | HU | Actividad | h | Responsable |
|---|---|---|---:|---|
| T1 | HU-01 | auth-service: Usuario/Rol, JWT, administrador semilla | 3 | Pablo |
| T2 | HU-01 | Gateway YARP: ruteo y validación JWT | 2 | Pablo |
| T3 | HU-01 | Pantalla de inicio de sesión y rutas protegidas | 3 | Pablo |
| T4 | HU-04 | catalogo-service: carrera, CRUD y pruebas | 2 | Leslie |
| T5 | HU-04 | UI: gestión de carreras | 3 | Leslie |
| T6 | HU-05 | Asignatura, validaciones y API | 3 | Leslie |
| T7 | HU-05 | UI: asignaturas por carrera | 4 | Leslie |
| T8 | HU-05 | Datos de prueba: Álgebra Lineal | 2 | Leslie |
| T9 | HU-14 | Comparador: contenido, profundidad, horas, créditos, TDD | 5 | Pablo |
| T10 | HU-14 | Umbral configurable y casos 79,9 %/80 % | 2 | Pablo |
| T11 | HU-15 | Aprobación y antigüedad máxima de diez años | 3 | Pablo |
| T12 | HU-16 | Mínimos para cambios internos y externos | 3 | Pablo |
| T13 | HU-06 | Entidades y API de unidades curriculares y contenidos | 4 | Leslie |
| T14 | HU-06 | UI: unidades, contenidos y resultados | 4 | Leslie |
| T15 | HU-06 | Carga de cuatro unidades de Álgebra Lineal | 2 | Leslie |
| **Total corregido** | | | **45** | **Pablo 21 h; Leslie 24 h** |

**Corrección respecto al PDF:** en el cuadro resumen la tarea de carga aparece repetida como T10 y figura «43 h». Según el desglose individual, corresponde a **T15** y la suma correcta es **45 h**.

### Riesgo de capacidad

Las 45 h son estimaciones de trabajo de desarrollo del documento y no incluyen de forma explícita toda la instalación, integración, revisión y correcciones. Dos desarrolladores con 3 h/día durante tres días acumularían solo 18 h brutas. El equipo deberá ampliar su disponibilidad real para aspirar a completar el compromiso y registrar las desviaciones honestamente. La asistencia de Codex no sustituye pruebas ni revisión.

## Dependencias

- Antes de desarrollo paralelo: definir contratos entre Gateway, Auth, Catálogo y Homologación.
- HU-01: T1 → T2 y T3 → integración con rutas protegidas.
- HU-04: T4 → T5.
- HU-05: T4/T6 → T7/T8.
- HU-06: T6/T13 → T14/T15.
- HU-14: reglas y pruebas T9 → configurable T10; contenidos estructurados HU-06 necesarios para pruebas integrales.
- HU-15 y HU-16: pruebas independientes de reglas deterministas (T11/T12).

## Calendario de ejecución

| Día | Pablo (desarrollo) | Leslie (desarrollo) | Shirley y Edwin (documentación/aceptación) |
|---|---|---|---|
| **Lun 19** | Estructura común, T1–T2, pruebas iniciales de T9 | T4, T6 y T13; modelos y persistencia | Corregir planificación, crear Issues, definir aceptación y registrar riesgos |
| **Mar 20** | T3, T9, T10 y pruebas | T5, T7, T8 y T14 | Validar criterios, actualizar tablero, preparar evidencias |
| **Mié 21** | T11, T12; integración y pruebas | T15; correcciones e integración | QA final, revisión del Sprint e informe de resultados |

Este calendario identifica objetivos por día, no significa que el desarrollo esté realizado.

## Definition of Done (DoD)

- [ ] Código incorporado mediante PR hacia `develop`, revisado por otro integrante.
- [ ] Pruebas unitarias en verde y sin regresiones.
- [ ] Todos los criterios de aceptación comprobados por la Product Owner.
- [ ] Endpoint documentado en Swagger cuando aplique.
- [ ] Servicios funcionales con `docker compose up`.
- [ ] Evidencia de la demostración y resultado de la revisión registrados.

## Prácticas XP

Planning Game, TDD en reglas, revisión en parejas, integración continua con GitHub Actions, pequeños lanzamientos, refactorización, propiedad colectiva y ritmo sostenible. La evidencia de cada práctica debe corresponder a actividad efectivamente realizada.

## Alcance fuera del Sprint 1

Los agentes de IA de homologación interna/externa, registro de solicitudes Quipux, revisión de dos docentes, generación de informes y el PDF corresponden a historias de posteriores sprints. No declarar esas funcionalidades implementadas en este incremento.
