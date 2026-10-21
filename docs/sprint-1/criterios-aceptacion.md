# Criterios de aceptación — Sprint 1

Transcripción organizada de los criterios detallados para HU-01, HU-04, HU-05, HU-06, HU-14, HU-15 y HU-16 en las páginas 19–38 del documento de planificación. Los responsables de aceptación registrarán pruebas y evidencias sin considerar terminada una historia por su sola implementación.

## HU-01 — Inicio de sesión
- [ ] Credenciales correctas: administrador recibe JWT y entra al panel.
- [ ] Credenciales incorrectas: error y ningún token.
- [ ] Ruta protegida sin token válido: Gateway responde HTTP 401.

## HU-04 — Gestión de carreras
- Aclaración SC-01 (20/10/2026): Leslie confirmó el acuerdo con Shirley. El catálogo receptor corresponde a FISEI; es una decisión de alcance del equipo, no una norma institucional.
- [ ] Crear carrera con nombre, facultad y vigencia; debe aparecer en el listado.
- [ ] La UI muestra FISEI fija al crear/editar y envía `facultad: "FISEI"`; POST/PUT rechazan otra facultad con 400 sin persistir cambios. Se admiten diferencias de caso y espacios externos, conservando FISEI canónica.
- [ ] Los registros anteriores conservan UUID, facultad y asignaturas; los ajenos a FISEI quedan señalados para revisión, sin conversión automática ni edición silenciosa.
- [ ] Nombre duplicado en la misma facultad: rechazar con mensaje claro.
- [ ] Eliminar carrera sin asignaturas: permitido; con asignaturas: prohibido.

## HU-05 — Gestión de asignaturas
- [ ] Registrar asignatura con nivel, créditos y horas mayores que cero, relacionada con su carrera.
- [ ] Créditos u horas menores o iguales a cero: rechazar.
- [ ] Consultar malla de carrera y ordenar asignaturas por nivel.

## HU-06 — Unidades curriculares y contenidos
- [ ] Aclaración docente 21/10/2026: unidad curricular → unidades temáticas → contenidos mínimos; horas y resultado de aprendizaje compartidos en la curricular.
- [ ] Registrar, consultar y editar varias temáticas por unidad, conservando su asociación con los contenidos.
- [ ] Contenidos anteriores sin clasificación se conservan y requieren asignación humana, sin inferencias automáticas.
- [ ] Registrar unidad con contenido y resultados de aprendizaje asociados a una asignatura.
- [ ] Unidad sin campos obligatorios: error.
- [ ] Consulta de asignatura incluye unidades, contenidos y resultados.
- [ ] Registrar y consultar el programa analítico usado como dato de prueba.

## HU-14 — Equivalencia de asignaturas (aclaración académica del 19/10/2026)

Cambio confirmado por el docente y transmitido por el equipo: **tres criterios**, sustituyendo las
cuatro dimensiones anteriores. No implica atribución a una norma oficial no proporcionada.
El estado de las casillas corresponde a aceptación de la PO, no solo a pruebas técnicas.

- [ ] Compatibilidad académica: `(contenidos cubiertos + resultados cubiertos) / (contenidos receptores + resultados receptores) × 100`; igual peso por elemento, sin promedio 50/50.
- [ ] 8 contenidos y 4 resultados de 10 contenidos y 5 resultados receptores: 80 %.
- [ ] 7 contenidos y 4 resultados de esos totales: menos del 80 %, no homologable con umbral 80 %.
- [ ] Varias asignaturas de origen aprobadas cubren elementos receptores sin doble conteo por categoría.
- [ ] Correspondencias explícitas con evidencia en ambos planes; identificadores inexistentes se rechazan.
- [ ] Un único origen y unidades comparables: horas/créditos de origen iguales o mayores producen 100 %, nunca más.
- [ ] Tres criterios exactamente al 80 %: homologable, también mediante API con evidencia válida.
- [ ] Cualquiera de los tres criterios inferior al umbral: no homologable, sin compensación.
- [ ] Umbral externo de 85 % y un criterio de 82 %: no homologable.
- [ ] Información obligatoria ausente, aprobación no acreditada, inventarios desconocidos o sin elementos receptores: información incompleta y sin recomendación.
- [ ] Varias fuentes sin regla de agregación horaria/de créditos, o unidades/sistemas no comparables: información incompleta; no sumar ni convertir por suposición.
- [ ] Respuesta distingue estados y explica los elementos receptores sin cobertura acreditada.

Ver el [contrato y cambio incompatible](../arquitectura/decisiones-y-contratos.md) y
las [evidencias vigentes](evidencia-hu-14-tres-criterios.md). El Issue #5 remoto consultado conserva
la descripción histórica; su actualización externa queda pendiente de coordinación/autorización.

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
El diseño inicial de cuatro dimensiones fue sustituido para HU-14 por la aclaración académica del equipo: tres criterios con umbral configurable inicialmente del 80 %. La confirmación comunicada por el usuario no debe presentarse como una norma oficial no aportada. Agregación entre varias asignaturas y conversiones de unidades/sistemas siguen pendientes.
