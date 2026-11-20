# Planificación del Sprint 3

Relacionado con [Issue #8 — planificación general](https://github.com/PabloToapanta/ProyectoHomologacion/issues/8). Versión de coordinación del 13/11/2026, preparada para el Planning con la Product Owner; **actualizada el 20/11/2026** con las respuestas del ingeniero ([decisiones de validación](decisiones-validacion-ingeniero.md)) y el estado verificable de las ramas. Distribuye trabajo planificado; no acredita avance ni disponibilidad de días ya transcurridos.

Sistema de Homologación de Materias (FISEI-UTA) · 5 al 25 de noviembre de 2026 · Desarrollo principal: 5–24 de noviembre · Integración, aceptación y Review: miércoles 25 de noviembre

**Cambio de calendario:** el cierre original era el miércoles 18 de noviembre. El equipo informa una semana adicional; la entrega pasa al **miércoles 25 de noviembre**. Fuente de las historias: Product Backlog del documento «Planificación Scrum + XP» (Sprint 0) y [cambios al Product Backlog del Sprint 2](../sprint-2/planificacion-detallada.md#7-cambios-al-product-backlog).

## 1. Punto de partida

| Sprint | Historias | PH | Estado |
| --- | --- | --- | --- |
| Sprint 1 | HU-01, 04, 05, 06, 14, 15, 16 | 26 | Entregado |
| Sprint 2 | HU-08, 10, 11, 18, 19, 20, 29, 30, 34, 35, 36 | 45 | En `develop`; aceptación de la PO pendiente |
| Sprint 3 adelantadas | HU-31 (revisión docente) y HU-23 (informe) | 10 | En `develop`; HU-23 aprobada por el ingeniero el 04/11 |

**Estado verificable al 13/11/2026:**

- `develop` no tiene commits posteriores al 04/11 (último: `4b1162c`, HU-23). No existen Issues de GitHub para las historias pendientes del Sprint 3.
- Ramas del cierre del Sprint 2 sin integrar: `feature/hu-08-revision-carga-masiva` y `feature/hu-01-sesion-recarga` (publicadas, sin PR registrado aquí) y `feature/hu-18-espera-agente` (solo local).
- El avance real del 5 al 12 de noviembre debe confirmarlo el equipo. Se mantienen las 14 historias más HU-37; la capacidad se revisa con ese dato (sección 3).

**Estado verificable al 20/11/2026** (repositorio remoto; el trabajo local no publicado no se ve):

- `develop` sigue en `4b1162c` (04/11); no se ha integrado nada del Sprint 3.
- Issues creados: #63–#77. PR abiertas a `develop`, todas sin merge:

  | PR | Contenido |
  | --- | --- |
  | [#78](https://github.com/PabloToapanta/ProyectoHomologacion/pull/78) | HU-08 |
  | [#79](https://github.com/PabloToapanta/ProyectoHomologacion/pull/79) | HU-01, sesión |
  | [#80](https://github.com/PabloToapanta/ProyectoHomologacion/pull/80) | HU-18, espera del agente |
  | [#81](https://github.com/PabloToapanta/ProyectoHomologacion/pull/81) | Esta planificación |
  | [#82](https://github.com/PabloToapanta/ProyectoHomologacion/pull/82) | HU-12, adjuntos |
  | [#83](https://github.com/PabloToapanta/ProyectoHomologacion/pull/83) | HU-24, PDF |

- Sin ramas publicadas para HU-02, 03, 07, 09, 17, 21, 22, 25, 27, 28, 32, 33 ni 37.
- Las respuestas del ingeniero (CE-2, CE-3, RN-07, HU-03, HU-32/33, HU-37) se registran en [decisiones de validación](decisiones-validacion-ingeniero.md). El equipo decide **mantener todas las historias** e implementar con esos criterios, como reglas de análisis del proyecto pendientes de confirmación institucional.

## 2. Objetivo del sprint

Cerrar el flujo completo de una solicitud —el informe se remite, el coordinador lo revisa y finaliza la solicitud—, completar la administración del sistema y los casos especiales que la normativa permite, y entregar el software con pruebas E2E, documentación y guion de demostración.

## 3. Capacidad

Se planifican 15 jornadas: jueves 5 y viernes 6; del lunes 9 al viernes 13; del lunes 16 al viernes 20; lunes 23, martes 24 y miércoles 25 de noviembre. Sin fines de semana ni feriados en el período. Se mantienen 3 h por integrante y jornada; la disponibilidad real debe confirmarla el equipo.

| Cálculo | Resultado | Referencia |
| --- | --- | --- |
| 4 integrantes × 3 h × 15 jornadas × 0,8 | **144 h** | Capacidad efectiva del sprint; 180 h brutas |
| Desde el 13/11: 4 × 3 h × 9 jornadas × 0,8 | **86,4 h** | Si el 5–12 no tuvo avance |
| 5,2 PH/día (Sprint 1) × 15 jornadas | 78 PH | Proyección orientativa |
| Pendiente del Product Backlog | **59 PH · 101 h** | 15 historias, incluida HU-37 (sección 4) |
| Cierre del Sprint 2 | 6 h | PR, revisión cruzada y aceptación de HU-08, HU-01 y HU-18 |
| **Total planificado** | **107 h** | Margen de 37 h sobre el sprint completo |

**Decisión del 13/11/2026: se mantienen las 14 historias.** **14/11/2026:** se añade HU-37 (reutilizar el análisis del agente, 3 PH), pedida por el ingeniero y aceptada por la PO. Las 107 h caben en el sprint completo, pero no en las 86,4 h restantes si el 5–12 no tuvo avance (faltarían 20,6 h). Si al revisar el avance falta capacidad, la PO decide qué sale. Las candidatas son **HU-21 (14 h)** y **HU-22 (8 h)**: sin ellas el compromiso sería 46 PH y 85 h, y además dependen de reglas aún no validadas (sección 6).

| Pareja | Horas asignadas | Efectivas (sprint completo) | Efectivas (desde el 13/11) | Si la PO retira HU-21/22 |
| --- | --- | --- | --- | --- |
| Pablo–Leslie | 54 h | 72 h | 43,2 h | 40 h |
| Edwin–Shirley | 53 h | 72 h | 43,2 h | 45 h |

**Revisión del 20/11/2026.** Quedan tres jornadas de desarrollo (viernes 20, lunes 23 y martes 24): 4 × 3 h × 3 × 0,8 = **28,8 h efectivas**. Restando HU-12 y HU-24 (en PR) y el cierre del Sprint 2 ya abierto en PR, el trabajo sin código publicado ronda las **95 h**. El equipo decide mantener todas las historias. Para que el 25/11 haya un flujo demostrable, se trabaja en este orden de prioridad (sección 7):

1. Integrar las PR abiertas.
2. HU-32/33.
3. HU-17, HU-03 y HU-37.
4. HU-02, HU-07 y HU-09.
5. HU-22 y HU-21.

Lo que no esté integrado y aceptado el 24/11 no se declara hecho: la PO lo devuelve al Product Backlog en la Review. Si los integrantes amplían sus horas, se registra la disponibilidad real.

El 25 de noviembre se reserva para integración, aceptación de la PO, Review y retrospectiva; no se inician historias nuevas. Shirley conserva el rol de Product Owner y la aceptación: el trabajo que desarrolla lo revisa otro integrante.

## 4. Historias

| HU | Historia | Prioridad | PH | Responsables | Condición |
| --- | --- | --- | --- | --- | --- |
| HU-32 | Remitir el informe al coordinador de carrera | Alta | 3 | Edwin y Shirley | — |
| HU-33 | El coordinador revisa el informe remitido y finaliza la solicitud | Alta | 3 | Edwin y Shirley | Depende de HU-32 |
| HU-27 | Plan de pruebas con pruebas de integración y E2E | Alta | 8 | Todo el equipo | E2E completo tras HU-33 |
| HU-02 | Gestionar usuarios y roles (docentes y coordinadores) | Media | 5 | Pablo y Leslie | — |
| HU-03 | Configurar umbral, años máximos y mínimos | Media | 3 | Pablo y Leslie | Coordinar con reglas de Edwin (HU-15/16) |
| HU-07 | Versionar mallas (vigente / no vigente) — CE-2 | Media | 3 | Pablo y Leslie | — |
| HU-09 | Consultar materias homologables entre carreras FISEI | Media | 5 | Pablo y Leslie | Usa correspondencias revisadas |
| HU-12 | Adjuntar historial y sílabos a la solicitud | Media | 2 | Edwin y Shirley | — |
| HU-17 | Regla de tercera matrícula (RN-07) | Media | 2 | Edwin y Shirley | 20/11: se excluye la asignatura, no la solicitud |
| HU-24 | Exportar el informe a PDF | Media | 3 | Edwin y Shirley | — |
| HU-28 | Documentación y guion de demo | Media | 3 | Shirley y Edwin | Al final |
| HU-25 | Historial y auditoría de informes | Baja | 3 | Edwin y Shirley | Tras HU-32/33 |
| HU-21 | Agrupación N:M — CE-3 | Media | 8 | Pablo y Leslie | 20/11: alcance N:1 acotado, pendiente de validación institucional |
| HU-22 | Carreras muy diferentes y ruta alternativa — CE-2 | Media | 5 | Edwin y Shirley | 20/11: solo alerta informativa, sin bloqueo; usa HU-07 |
| HU-37 | Reutilizar el análisis del agente para el mismo par y los mismos programas ([#77](https://github.com/PabloToapanta/ProyectoHomologacion/issues/77)) | Media | 3 | Edwin y Shirley | Pedida por el ingeniero; aceptada por la PO el 14/11. Toca código de HU-18: revisa Pablo |
| | **TOTAL: 15 historias comprometidas** | | **59** | | — |

No incluida: la historia para registrar el programa analítico de la universidad de origen (decisión D2 de HU-19). No figura en el Product Backlog; sin ella el análisis externo no llega a «Completo». Solo se planifica si la PO la incorpora.

### Reparto entre las parejas

Cada pareja trabaja sobre los servicios que ya conoce, para no editar en paralelo los mismos archivos:

- **Pablo y Leslie:** Auth, Gateway, catálogo y motor (HU-02, HU-03, HU-07, HU-09 y HU-21), más el E2E y la CI de HU-27.
- **Edwin y Shirley:** flujo de la solicitud y el informe, en Solicitudes y Homologación (HU-32, HU-33, HU-17, HU-25, HU-12, HU-24 y HU-22), más el plan de pruebas de HU-27 y HU-28.

Pablo y Leslie tienen 24 PH y Edwin y Shirley 27 PH con HU-37; HU-27 (8 PH) se comparte. Las horas quedan casi iguales (54 h y 53 h), aunque Shirley además dedica tiempo a su rol de Product Owner (aceptación de las historias y Review). HU-37 modifica la orquestación de HU-18 (`AsistirEvaluacionInterna`) y la persistencia de HU-20: se acuerda el cambio con Pablo antes de codificar y él lo revisa.

| Pareja | Historias | PH | Horas |
| --- | --- | --- | --- |
| Pablo–Leslie | HU-02, HU-03, HU-07, HU-09, HU-21 y HU-27 (T66 E2E, T67 CI) | 24 + HU-27 | 54 h |
| Edwin–Shirley | HU-32, HU-33, HU-12, HU-17, HU-24, HU-25, HU-22, HU-37, HU-28 y HU-27 (T65 plan de pruebas) | 27 + HU-27 | 53 h |

**Responsable principal por historia** (la pareja colabora; la revisión cruzada la hace un integrante de la otra pareja):

| Integrante | Lidera | Horas |
| --- | --- | --- |
| Pablo | HU-02 usuarios (8 h), HU-03 parámetros (6 h), HU-21 junto con Leslie (7 h), E2E junto con Leslie (3 h), CI T67 (3 h), cierre del Sprint 2 (1,5 h) | 28,5 h |
| Leslie | HU-07 mallas (6 h), HU-09 consulta FISEI (8 h), HU-21 junto con Pablo (7 h), E2E junto con Pablo (3 h), cierre del Sprint 2 (1,5 h) | 25,5 h |
| Edwin | HU-32 remitir (5 h), HU-33 finalizar (6 h), HU-17 RN-07 (4 h), HU-25 auditoría (5 h), HU-22 junto con Shirley (4 h), HU-37 junto con Shirley (2,5 h), cierre del Sprint 2 (1,5 h) | 28 h |
| Shirley | HU-12 adjuntos (4 h), HU-24 PDF (5 h), HU-22 junto con Edwin (4 h), HU-37 junto con Edwin (2,5 h), plan de pruebas T65 (3 h), HU-28 (5 h), cierre del Sprint 2 (1,5 h); además, la aceptación como PO | 25 h |

Cada integrante dispone de 36 h efectivas en el sprint completo (3 h × 15 × 0,8), o de 21,6 h desde el 13/11. Las coincidencias entre parejas son pocas y se coordinan: HU-22 usa las versiones de malla de HU-07, HU-03 toca las reglas de Edwin (HU-15/16) y el E2E de Pablo y Leslie necesita HU-32/33 terminadas.

## 5. Sprint Backlog

| Tarea | HU | Actividad | Horas |
| --- | --- | --- | --- |
| — | Sprint 2 | PR a `develop`, revisión cruzada y aceptación de HU-08, HU-01 (sesión) y HU-18 (espera del agente) | 6 |
| T44 | HU-32 | Homologación: remitir el informe una sola vez (fecha, docente asignado); la solicitud pasa a «Remitida» mediante contrato con Solicitudes | 3 |
| T45 | HU-32 | Interfaz: «Remitir al coordinador» y estado visible para docentes y coordinador | 2 |
| T46 | HU-33 | API del coordinador: informes remitidos; en las parejas «Sin acuerdo», decisión propia motivada (resultado, justificación, autor, fecha); finalización con la solicitud en solo lectura. La devolución y la reapertura quedan fuera | 4 |
| T47 | HU-33 | Interfaz de revisión y finalización del coordinador | 2 |
| T48 | HU-24 | Exportar el informe fijo a PDF (acordar: hoja de impresión del navegador o generación en servidor) y prueba | 5 |
| T49 | HU-12 | Adjuntos PDF de la solicitud (límite de tamaño, huella SHA-256, quién y cuándo) e interfaz | 4 |
| T50 | HU-17 | RN-07 con TDD: una asignatura aprobada en tercera matrícula, con evidencia adjunta (HU-12), queda excluida; las demás se evalúan; sin evidencia, «información incompleta». Mostrarlo en el análisis y el informe | 4 |
| T51 | HU-22 | CE-2 en el informe, como alerta sin bloqueo: sin el mínimo de HU-16, remitir al proceso de admisión y cambio de carrera de la UTA citando la fuente; si la malla de origen no está vigente, indicar que la carrera confirma la ruta de cambio de plan | 5 |
| T52 | HU-22 | Interfaz e informe de la ruta alternativa | 3 |
| T53 | HU-25 | Registro de eventos del informe (generado, remitido, finalizado) y consulta del historial | 5 |
| T54 | HU-02 | Auth: el administrador crea, activa/desactiva y cambia el rol de docentes y coordinadores; contraseña temporal con hash | 5 |
| T55 | HU-02 | Interfaz de usuarios y roles | 3 |
| T56 | HU-03 | Parámetros persistidos (umbral, años máximos —10 provisional—, mínimos interno/externo) con los valores vigentes como iniciales. Historial de cambios (actor, fecha, anterior/nuevo, motivo, respaldo) e instantánea de los parámetros en cada análisis. El motor los lee | 4 |
| T57 | HU-03 | Interfaz de parámetros | 2 |
| T58 | HU-07 | Catálogo: versión de malla vigente/no vigente y asignaturas por versión; migración sin pérdida de datos | 4 |
| T59 | HU-07 | Interfaz de versiones de malla | 2 |
| T60 | HU-09 | Consulta de homologables entre dos carreras FISEI con el motor determinista sobre correspondencias revisadas | 5 |
| T61 | HU-09 | Interfaz de la consulta | 3 |
| T62 | HU-21 | Motor de agrupación N:1 con TDD: cobertura de cada elemento receptor contado una vez; suma de horas y créditos de los orígenes aprobados (tope 100 %); unidades distintas → «información incompleta»; origen de uso único por solicitud | 6 |
| T63 | HU-21 | Solicitud, análisis e informe marcados «homologación por agrupación» | 5 |
| T64 | HU-21 | Interfaz de agrupación | 3 |
| T65 | HU-27 | Plan de pruebas: matriz HU → criterio → prueba automatizada o manual | 3 |
| T66 | HU-27 | E2E del flujo completo: login → solicitud → docentes → análisis → informe → remisión → finalización | 6 |
| T67 | HU-27 | Integración y E2E en GitHub Actions | 3 |
| T68 | HU-28 | Documentación: arranque con Docker Compose, manual por rol y configuración | 3 |
| T69 | HU-28 | Guion de demo con datos sintéticos | 2 |
| T70 | HU-37 | Homologación: clave de reutilización (par, firmas de ambos programas y de las correspondencias, parámetros, modelo y versión de la plantilla del prompt), sin caducidad por tiempo, guardando solo análisis completos | 3 |
| T71 | HU-37 | Reutilizar o forzar un análisis nuevo (docente asignado, motivo registrado, sin borrar el anterior); la interfaz indica «reutilizado», la fecha y el id del análisis original; pruebas | 2 |
| | | **TOTAL** | **107 h** |

HU-23 y HU-31 se adelantaron en el Sprint 2 y no consumen capacidad de este sprint; sus horas reales se registran en sus evidencias.

## 6. Decisiones previas a codificar

| Decisión | Afecta | Quién | Fecha límite |
| --- | --- | --- | --- |
| Validar CE-3 con el docente: unión de temas del origen y suma de horas. El documento base lo marca como «interpretación» que la normativa no regula. Sin validación no se codifica ninguna agregación. | HU-21 | PO con el ingeniero | Lunes 16/11 |
| Validar CE-2: qué significa «carreras muy diferentes», cuál es la vía de admisión y la ruta de cambio de plan. | HU-22 | PO con el ingeniero | Lunes 16/11 |
| RN-07: cómo se determina que la malla receptora «considera» la asignatura de tercera matrícula (mismo código, correspondencia revisada u otra regla). | HU-17 | PO | Antes de T50 |
| Contrato de estados de la solicitud: Remitida y Finalizada, quién los cambia y qué queda bloqueado. | HU-32/33 | Edwin con Pablo | Antes de T44 |
| HU-03: quién puede cambiar parámetros normativos y si se registra el cambio. Los valores iniciales siguen la aclaración del 19/10 (80 %, 10 años, mínimos de HU-16). | HU-03 | PO | Antes de T56 |
| PDF: generación en navegador o en servidor (librería y licencia). | HU-24 | Edwin y Shirley | Antes de T48 |

Si el lunes 16/11 no hay validación de CE-2 o CE-3, la PO decide si la historia afectada se mantiene o vuelve al Product Backlog; no se codifican reglas sin validar.

**Estado al 20/11/2026.** El ingeniero respondió las decisiones académicas con una lectura de fuentes públicas (RRA del CES e instructivo UTA RES CAU-P-005-2023); él mismo aclara que no sustituye el pronunciamiento institucional. El detalle está en [decisiones de validación](decisiones-validacion-ingeniero.md).

| Decisión | Resultado para implementar |
| --- | --- |
| CE-3 (HU-21) | Solo agrupación N:1, con cobertura sin doble conteo, suma de horas y créditos de los orígenes aprobados, y cada origen usado una vez. Es regla del proyecto, pendiente de validación. |
| CE-2 (HU-22) | No se define «muy diferentes». Alerta informativa sin bloqueo, que remite al proceso de la UTA. |
| RN-07 (HU-17) | Se excluye la asignatura aprobada en tercera matrícula, no toda la solicitud. Ya no hace falta definir qué significa «considera». |
| HU-03 | Historial de cambios e instantánea por análisis. La propuesta es que cambie los parámetros el rol Coordinador; **lo confirma la PO**. |
| Estados HU-32/33 | Se remite con todas las parejas completas. En «Sin acuerdo», decisión motivada del coordinador. Finalizar bloquea la edición. La devolución y la reapertura quedan fuera. El contrato técnico lo acuerdan Edwin y Pablo. |
| PDF (HU-24) | Lo resuelve la PR #83. Lleva la leyenda «Formato del sistema; no sustituye el formato oficial». |

Hallazgos sobre lo entregado:

- **Antigüedad.** El CES dice 10 años y el instructivo UTA de 2023, 5 años. Se mantienen 10 años como valor provisional y parametrizable.
- **Tres criterios del motor.** Se presentan como regla de análisis del proyecto; el CES nombra contenido, profundidad y carga horaria.

## 7. Calendario

| Fecha | Pablo y Leslie | Edwin y Shirley |
| --- | --- | --- |
| 5–6 nov | Planning. PR y revisión cruzada de HU-01 y HU-18. Contrato de HU-02/03. | Cierre del Sprint 2: PR de HU-08 y aceptación de la PO. Contrato de estados HU-32/33. Crear los Issues del sprint. |
| 9–10 nov | HU-02 (T54–T55). | HU-32 (T44–T45). Plan de pruebas (T65). |
| 11–12 nov | HU-03 (T56–T57). | HU-33 (T46–T47). |
| 13 nov | HU-07 (T58). Solicitar validación de CE-2 y CE-3. | HU-17 (T50). |
| 16–17 nov | HU-07 (T59) y HU-09 (T60–T61). **Decisión sobre HU-21/22.** | HU-24 (T48) y HU-12 (T49). |
| 18–19 nov | HU-21 (T62–T63) si se valida CE-3; si no, E2E (T66). | HU-25 (T53); HU-22 (T51) si se valida CE-2. |
| 20 nov | E2E (T66) y CI (T67). | HU-37 (T70–T71); HU-22 (T52) o documentación (T68). |
| 23–24 nov | HU-21 (T64) o estabilización. Congelar funcionalidad al cierre del 24. | Documentación (T68) y guion de demo (T69). |
| 25 nov | Integración final y verificación de seguridad. | Aceptación de la PO, evidencias, Review y retrospectiva. |

Las filas del 5 al 12 de noviembre son el plan base. Si esas tareas no se ejecutaron, se recorren al período restante y se revisa la capacidad (sección 3).

**Calendario ajustado del 20/11/2026.** Sigue el orden de prioridad de la sección 3 y mantiene todas las historias.

| Fecha | Pablo y Leslie | Edwin y Shirley |
| --- | --- | --- |
| Vie 20 nov | Revisar e integrar #78, #79, #80 y #82/#83 (revisión cruzada). Contrato de estados con Edwin. HU-03 (T56). | Contrato de estados HU-32/33 con Pablo. HU-32 (T44–T45). HU-17 (T50) con TDD. |
| Lun 23 nov | HU-03 (T57) y HU-02 (T54–T55). HU-07 (T58–T59). | HU-33 (T46–T47). HU-37 (T70–T71), revisado por Pablo. |
| Mar 24 nov | HU-09 (T60–T61) y HU-21 (T62–T64). E2E (T66) y CI (T67). Congelar a las 18:00. | HU-25 (T53) y HU-22 (T51–T52). Plan de pruebas (T65), documentación (T68) y guion (T69). |
| Mié 25 nov | Integración final con Docker Compose y verificación de seguridad. | Aceptación de la PO, evidencias, Review y retrospectiva. |

Con 28,8 h efectivas no cabe todo: lo pendiente el martes 24 no se declara hecho (sección 3).

## 8. Riesgos del sprint

| Riesgo | Mitigación |
| --- | --- |
| Avance del 5–12 sin confirmar; quedan 86,4 h efectivas frente a 107 h. | Confirmar en el Planning. Si falta capacidad, la PO decide; candidatas a salir: HU-21 y HU-22. (Revisado el 20/11: se mantienen; ver la fila de capacidad del 20/11). |
| CE-2 y CE-3 no validados a tiempo. | 20/11: respuesta del ingeniero sin pronunciamiento institucional. Se implementan los alcances acotados de las [decisiones de validación](decisiones-validacion-ingeniero.md), marcados como reglas de análisis del proyecto; no se inventan reglas fuera de ellos. |
| 20/11: unas 95 h sin código publicado frente a 28,8 h efectivas. | Seguir el orden de prioridad; integrar cada día; lo que no esté aceptado el 24/11 vuelve al Product Backlog. |
| Antigüedad de 10 años (CES) frente a 5 años (instructivo UTA 2023). | Valor provisional de 10 años, parametrizado en HU-03 y advertido en la documentación; lo confirma la UTA. |
| HU-03 cambia el umbral y las reglas que usan HU-14/15/16 y sus pruebas. | Valores iniciales idénticos a los actuales y pruebas existentes en verde; coordinar con Edwin. |
| HU-07 modifica el modelo del catálogo con datos reales ya cargados. | Migración que asigna la versión vigente a lo existente; respaldo previo de la base. |
| HU-32/33 cruzan Homologación y Solicitudes. | Contrato acordado antes de T44; clave interna (HU-36) y JWT en ambas llamadas. |
| E2E inestable en CI (Ollama no disponible en GitHub Actions). | E2E con agente simulado; la prueba con modelo real queda como evidencia local. |
| HU-37 cambia código de HU-18/HU-20 de otra pareja. | Acordar el cambio con Pablo antes de T70; revisión de Pablo en el PR. |
| Cambios de varias personas en `App.tsx`, `Program.cs` y Gateway. | Coordinar como en sprints anteriores; PR pequeños y revisados el mismo día. |

## Coordinación de implementación

- Crear un Issue por HU antes de su código y vincular cada PR a él.
- Ramas `feature/hu-XX-descripcion` desde `develop`, Conventional Commits en español y PR a `develop` con revisión cruzada.
- TDD en reglas: RN-07 (HU-17), CE-2 (HU-22), CE-3 (HU-21) y parámetros (HU-03).
- El motor determinista conserva el veredicto; ninguna historia de este sprint delega decisiones al agente de IA.
- Sin secretos en Git ni en el frontend; los adjuntos (HU-12) y los PDF (HU-24) no se versionan.

## Definición de hecho y estado

PR hacia `develop` revisado por otro integrante; criterios comprobados por Shirley; pruebas en verde; Swagger actualizado; ejecución integrada con Docker Compose y evidencia reproducible. El registro de ejecución, la Review y la retrospectiva se documentan cuando ocurran.
