# Criterios de aceptación — Sprint 1

Transcripción organizada de los criterios detallados para HU-01, HU-04, HU-05, HU-06, HU-14, HU-15 y HU-16 en las páginas 19–38 del documento de planificación. Los responsables de aceptación registrarán pruebas y evidencias sin considerar terminada una historia por su sola implementación.

## HU-01 — Inicio de sesión
- [ ] Credenciales correctas: administrador recibe JWT y entra al panel.
- [ ] Credenciales incorrectas: error y ningún token.
- [ ] Ruta protegida sin token válido: Gateway responde HTTP 401.

## HU-04 — Gestión de carreras
- [ ] Crear carrera con nombre, facultad y vigencia; debe aparecer en el listado.
- [ ] Nombre duplicado en la misma facultad: rechazar con mensaje claro.
- [ ] Eliminar carrera sin asignaturas: permitido; con asignaturas: prohibido.

## HU-05 — Gestión de asignaturas
- [ ] Registrar asignatura con nivel, créditos y horas mayores que cero, relacionada con su carrera.
- [ ] Créditos u horas menores o iguales a cero: rechazar.
- [ ] Consultar malla de carrera y ordenar asignaturas por nivel.

## HU-06 — Unidades curriculares y contenidos
- [ ] Registrar unidad con contenido y resultados de aprendizaje asociados a una asignatura.
- [ ] Unidad sin campos obligatorios: error.
- [ ] Consulta de asignatura incluye unidades, contenidos y resultados.
- [ ] Registrar y consultar el programa analítico usado como dato de prueba.

## HU-14 — Equivalencia de asignaturas
- [ ] Si horas y créditos de origen son iguales o mayores que las receptoras, su porcentaje es 100 %.
- [ ] Una dimensión de 79,9 % con umbral de 80 %: no homologable.
- [ ] Las cuatro dimensiones con exactamente 80 %: homologable.
- [ ] Con umbral de 85 %, una dimensión de 82 %: no homologable.

## HU-15 — Aprobación y antigüedad
- [ ] Asignatura aprobada hace menos de diez años: elegible.
- [ ] Asignatura aprobada hace exactamente diez años: elegible.
- [ ] Asignatura reprobada o no cursada: no elegible.
- [ ] Aprobada hace más de diez años: indicar validación de conocimientos.

## HU-16 — Requisitos para cambio
- [ ] Cambio interno: al menos un período y una asignatura homologable: cumple.
- [ ] Cambio interno sin asignaturas homologables: no cumple.
- [ ] Cambio externo: dos períodos, dos asignaturas homologables y ningún cambio previo: cumple.
- [ ] Cambio externo con una asignatura homologable o con cambio previo: no cumple.

## Evidencia por criterio
Para cada prueba, registrar entrada o pasos, resultado esperado, resultado real, captura o salida de test, fecha y responsable. La Product Owner acepta solo después de comprobar todos los criterios.

## Advertencia sobre normativa
El documento del equipo plantea cuatro dimensiones con umbral configurable del 80 %. El propio documento señala que la fórmula y sus interpretaciones deben contrastarse con la Coordinación de Carrera. No convertir esta propuesta en afirmación de normativa oficial sin validación.
