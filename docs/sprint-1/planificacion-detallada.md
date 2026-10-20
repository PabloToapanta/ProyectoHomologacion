# Planificación detallada del Sprint 1

**Proyecto:** Sistema de Homologación de Materias (FISEI-UTA)  
**Fecha de planificación original:** 15–21 de octubre de 2026  
**Replanificación operativa:** 19–21 de octubre de 2026  
**Duración disponible en la replanificación:** lunes, martes y miércoles.  
**Product Owner:** Shirley Amaguaña  
**Scrum Master:** Pablo Toapanta  
**Desarrolladores de código:** Pablo Toapanta, Leslie Coello y Edwin Caraguay  
**Product Owner, documentación Scrum y QA de aceptación:** Shirley Amaguaña

> Basado en el documento académico «Sprint 0: Planificación Scrum + XP — Sistema de Homologación de Materias». Este archivo no certifica que el código esté terminado. Los responsables de ejecución se redistribuyeron por disponibilidad del equipo; las siete historias comprometidas permanecen en el Sprint 1.

## Sprint Goal

Que un administrador autenticado pueda mantener el catálogo de carreras y asignaturas con unidades y contenidos curriculares; además, que el sistema disponga de un núcleo determinista que calcule equivalencias y valide antigüedad y requisitos mínimos de cambio, probado con TDD.

## Compromiso: 7 historias y 26 PH

| Historia | Descripción | Prioridad | PH | Responsable de código |
|---|---|---|---:|---|
| HU-01 | Iniciar sesión con rol | Alta | 3 | Pablo |
| HU-04 | Gestionar carreras | Alta | 3 | Leslie |
| HU-05 | Gestionar asignaturas | Alta | 5 | Leslie |
| HU-06 | Gestionar unidades, contenidos y resultados | Alta | 5 | Edwin |
| HU-14 | Calcular equivalencia de asignaturas | Alta | 5 | Pablo |
| HU-15 | Validar aprobación y antigüedad | Alta | 2 | Edwin |
| HU-16 | Validar mínimos de cambio interno/externo | Alta | 3 | Edwin |
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
| T9 | HU-14 | Comparador: académico combinado, horas y créditos; múltiples orígenes y evidencia, TDD (aclaración 19/10) | 5 | Pablo |
| T10 | HU-14 | Umbral configurable y casos 79,9 %/80 % | 2 | Pablo |
| T11 | HU-15 | Aprobación y antigüedad máxima de diez años | 3 | Edwin |
| T12 | HU-16 | Mínimos para cambios internos y externos | 3 | Edwin |
| T13 | HU-06 | Entidades y API de unidades curriculares y contenidos | 4 | Edwin |
| T14 | HU-06 | UI: unidades, contenidos y resultados | 4 | Edwin |
| T15 | HU-06 | Carga de cuatro unidades de Álgebra Lineal | 2 | Edwin |
| **Total corregido** | | | **45** | **Pablo 15 h; Leslie 14 h; Edwin 16 h** |

**Corrección respecto al PDF:** en el cuadro resumen la tarea de carga aparece repetida como T10 y figura «43 h». Según el desglose individual, corresponde a **T15** y la suma correcta es **45 h**.

### Riesgo de capacidad

Las 45 h son estimaciones de trabajo de desarrollo del documento y no incluyen de forma explícita toda la instalación, integración, revisión y correcciones. Tres desarrolladores con 3 h/día durante tres días acumularían 27 h brutas, menos que las 45 h técnicas estimadas. T1 y T2 ya tienen código publicado, pero su aceptación completa sigue pendiente. El equipo deberá ampliar su disponibilidad real para aspirar a completar el compromiso y registrar las desviaciones honestamente. La asistencia de Codex no sustituye pruebas ni revisión.

## Dependencias

SC-01, 20/10/2026: Leslie confirma acuerdo con Shirley para fijar el catálogo
receptor a FISEI. Ajuste de T4/T5 con regresión HU-05: política de dominio,
facultad fija en UI y conservación de datos anteriores. No altera las estimaciones
originales ni adelanta el contrato de procedencia o las reglas de Edwin.

- Antes de desarrollo paralelo: definir contratos entre Gateway, Auth, Catálogo y Homologación.
- HU-01: T1 → T2 y T3 → integración con rutas protegidas.
- HU-04: T4 → T5.
- HU-05: T4/T6 → T7/T8.
- HU-06: T6/T13 → T14/T15.
- HU-14: reglas y pruebas T9 → configurable T10; contenidos estructurados HU-06 necesarios para pruebas integrales.
- HU-15 y HU-16: pruebas independientes de reglas deterministas (T11/T12).

## Calendario de ejecución replanificado

| Día | Pablo | Leslie | Edwin | Shirley |
|---|---|---|---|---|
| **Lun 19** | T1–T2 implementadas; continuar T3, verificar contrato React/Gateway | T4 y T6: modelos, carrera y asignatura en catalogo-service | T11 y T12: pruebas primero, reglas de antigüedad y cambio en módulo independiente | Actualizar tablero, criterios, prioridades y evidencia |
| **Mar 20** | T3 e integración HU-01; T9/T10 comparador y pruebas | T5, T7, T8: pantallas y carga Álgebra Lineal | Completar HU-15/HU-16; T13 después de conocer modelos base | Pruebas manuales, revisión de demostraciones y estado de Issues |
| **Mié 21** | Integración de homologación y Gateway; revisión de PR | Integrar catálogo y frontend; correcciones | T14/T15 e integración de unidades; correcciones | Aceptación por criterios, Sprint Review y retrospectiva |

Este calendario indica actividades planificadas; no certifica implementación ni aceptación.

**Coordinación de archivos:** Pablo mantiene inicialmente `frontend/src/App.tsx` y el arranque de React; Leslie desarrolla `frontend/src/features/catalogo`. Edwin inicia HU-15/HU-16 en módulos propios de `homologacion-service` y coordina la modificación de `Homologacion.sln` y el punto de composición; HU-06 depende de Carrera/Asignatura de Leslie. Antes de merge, revisar integraciones y las rutas del Gateway.

**Revisión cruzada:** Pablo revisa a Leslie; Leslie revisa a Edwin; Edwin revisa a Pablo. Shirley acepta funcionalmente.

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

### Aclaración de HU-14 del 19/10/2026

El docente confirmó tres criterios; la compatibilidad académica combina contenidos y resultados
con igual peso por elemento. Se permite cobertura académica desde varios orígenes aprobados,
sin duplicar receptores. Esto sustituye profundidad como dimensión independiente de T9.
No se cambian tareas, criterios ni estimaciones de HU-15/HU-16 de Edwin. Las estimaciones T9/T10
se conservan como planificación original; no se afirman horas reales ni aprobación normativa.
