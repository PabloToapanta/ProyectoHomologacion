# Planificación del Sprint 3

Relacionado con [Issue #8 — planificación general](https://github.com/PabloToapanta/ProyectoHomologacion/issues/8). Versión de coordinación del 13/11/2026, preparada para el Planning con la Product Owner. Distribuye trabajo planificado; no acredita avance ni disponibilidad de días ya transcurridos.

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
- El avance real del 5 al 12 de noviembre debe confirmarlo el equipo. Se mantienen las 14 historias; la capacidad se revisa con ese dato (sección 3).

## 2. Objetivo del sprint

Cerrar el flujo completo de una solicitud —el informe se remite, el coordinador lo revisa y finaliza la solicitud—, completar la administración del sistema y los casos especiales que la normativa permite, y entregar el software con pruebas E2E, documentación y guion de demostración.

## 3. Capacidad

Se planifican 15 jornadas: jueves 5 y viernes 6; del lunes 9 al viernes 13; del lunes 16 al viernes 20; lunes 23, martes 24 y miércoles 25 de noviembre. Sin fines de semana ni feriados en el período. Se mantienen 3 h por integrante y jornada; la disponibilidad real debe confirmarla el equipo.

| Cálculo | Resultado | Referencia |
| --- | --- | --- |
| 4 integrantes × 3 h × 15 jornadas × 0,8 | **144 h** | Capacidad efectiva del sprint; 180 h brutas |
| Desde el 13/11: 4 × 3 h × 9 jornadas × 0,8 | **86,4 h** | Si el 5–12 no tuvo avance |
| 5,2 PH/día (Sprint 1) × 15 jornadas | 78 PH | Proyección orientativa |
| Pendiente del Product Backlog | **56 PH · 96 h** | 14 historias (sección 4) |
| Cierre del Sprint 2 | 6 h | PR, revisión cruzada y aceptación de HU-08, HU-01 y HU-18 |
| **Total planificado** | **102 h** | Margen de 42 h sobre el sprint completo |

**Decisión del 13/11/2026: se mantienen las 14 historias.** Las 102 h caben en el sprint completo, pero no en las 86,4 h restantes si el 5–12 no tuvo avance (faltarían 15,6 h). Si al revisar el avance falta capacidad, la PO decide qué sale. Las candidatas son **HU-21 (14 h)** y **HU-22 (8 h)**: sin ellas el compromiso sería 43 PH y 80 h, y además dependen de reglas aún no validadas (sección 6).

| Pareja | Horas asignadas | Efectivas (sprint completo) | Efectivas (desde el 13/11) | Si la PO retira HU-21/22 |
| --- | --- | --- | --- | --- |
| Pablo–Leslie | 54 h | 72 h | 43,2 h | 40 h |
| Edwin–Shirley | 48 h | 72 h | 43,2 h | 40 h |

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
| HU-17 | Regla de tercera matrícula (RN-07) | Media | 2 | Edwin y Shirley | Precisar «la malla receptora considera la asignatura» |
| HU-24 | Exportar el informe a PDF | Media | 3 | Edwin y Shirley | — |
| HU-28 | Documentación y guion de demo | Media | 3 | Shirley y Edwin | Al final |
| HU-25 | Historial y auditoría de informes | Baja | 3 | Edwin y Shirley | Tras HU-32/33 |
| HU-21 | Agrupación N:M — CE-3 | Media | 8 | Pablo y Leslie | **Validar CE-3 antes de codificar** |
| HU-22 | Carreras muy diferentes y ruta alternativa — CE-2 | Media | 5 | Edwin y Shirley | **Validar CE-2 antes de codificar**; usa HU-07 |
| | **TOTAL: 14 historias comprometidas** | | **56** | | — |

No incluida: la historia para registrar el programa analítico de la universidad de origen (decisión D2 de HU-19). No figura en el Product Backlog; sin ella el análisis externo no llega a «Completo». Solo se planifica si la PO la incorpora.

### Reparto entre las parejas

Cada pareja trabaja sobre los servicios que ya conoce, para no editar en paralelo los mismos archivos:

- **Pablo y Leslie:** Auth, Gateway, catálogo y motor (HU-02, HU-03, HU-07, HU-09 y HU-21), más el E2E y la CI de HU-27.
- **Edwin y Shirley:** flujo de la solicitud y el informe, en Solicitudes y Homologación (HU-32, HU-33, HU-17, HU-25, HU-12, HU-24 y HU-22), más el plan de pruebas de HU-27 y HU-28.

Las dos parejas tienen 24 PH cada una; HU-27 (8 PH) se comparte. Edwin y Shirley reciben 6 h menos porque Shirley también dedica tiempo a su rol de Product Owner (aceptación de las 14 historias y Review).

| Pareja | Historias | PH | Horas |
| --- | --- | --- | --- |
| Pablo–Leslie | HU-02, HU-03, HU-07, HU-09, HU-21 y HU-27 (T66 E2E, T67 CI) | 24 + HU-27 | 54 h |
| Edwin–Shirley | HU-32, HU-33, HU-12, HU-17, HU-24, HU-25, HU-22, HU-28 y HU-27 (T65 plan de pruebas) | 24 + HU-27 | 48 h |

**Responsable principal por historia** (la pareja colabora; la revisión cruzada la hace un integrante de la otra pareja):

| Integrante | Lidera | Horas |
| --- | --- | --- |
| Pablo | HU-02 usuarios (8 h), HU-03 parámetros (6 h), HU-21 junto con Leslie (7 h), E2E junto con Leslie (3 h), CI T67 (3 h), cierre del Sprint 2 (1,5 h) | 28,5 h |
| Leslie | HU-07 mallas (6 h), HU-09 consulta FISEI (8 h), HU-21 junto con Pablo (7 h), E2E junto con Pablo (3 h), cierre del Sprint 2 (1,5 h) | 25,5 h |
| Edwin | HU-32 remitir (5 h), HU-33 finalizar (6 h), HU-17 RN-07 (4 h), HU-25 auditoría (5 h), HU-22 junto con Shirley (4 h), cierre del Sprint 2 (1,5 h) | 25,5 h |
| Shirley | HU-12 adjuntos (4 h), HU-24 PDF (5 h), HU-22 junto con Edwin (4 h), plan de pruebas T65 (3 h), HU-28 (5 h), cierre del Sprint 2 (1,5 h); además, la aceptación como PO | 22,5 h |

Cada integrante dispone de 36 h efectivas en el sprint completo (3 h × 15 × 0,8), o de 21,6 h desde el 13/11. Las coincidencias entre parejas son pocas y se coordinan: HU-22 usa las versiones de malla de HU-07, HU-03 toca las reglas de Edwin (HU-15/16) y el E2E de Pablo y Leslie necesita HU-32/33 terminadas.

## 5. Sprint Backlog

| Tarea | HU | Actividad | Horas |
| --- | --- | --- | --- |
| — | Sprint 2 | PR a `develop`, revisión cruzada y aceptación de HU-08, HU-01 (sesión) y HU-18 (espera del agente) | 6 |
| T44 | HU-32 | Homologación: remitir el informe una sola vez (fecha, docente asignado); la solicitud pasa a «Remitida» mediante contrato con Solicitudes | 3 |
| T45 | HU-32 | Interfaz: «Remitir al coordinador» y estado visible para docentes y coordinador | 2 |
| T46 | HU-33 | API del coordinador: informes remitidos, decisión en parejas «Sin acuerdo» y finalización; la solicitud queda en solo lectura | 4 |
| T47 | HU-33 | Interfaz de revisión y finalización del coordinador | 2 |
| T48 | HU-24 | Exportar el informe fijo a PDF (acordar: hoja de impresión del navegador o generación en servidor) y prueba | 5 |
| T49 | HU-12 | Adjuntos PDF de la solicitud (límite de tamaño, huella SHA-256, quién y cuándo) e interfaz | 4 |
| T50 | HU-17 | RN-07 con TDD: aprobada con tercera matrícula y malla receptora que considera la asignatura → cambio no procedente; mostrarlo en análisis e informe | 4 |
| T51 | HU-22 | CE-2 en el informe: sin el mínimo, sugerir «no convalidar» y la vía de admisión; malla de origen no vigente, indicar la ruta de cambio de plan | 5 |
| T52 | HU-22 | Interfaz e informe de la ruta alternativa | 3 |
| T53 | HU-25 | Registro de eventos del informe (generado, remitido, finalizado) y consulta del historial | 5 |
| T54 | HU-02 | Auth: el administrador crea, activa/desactiva y cambia el rol de docentes y coordinadores; contraseña temporal con hash | 5 |
| T55 | HU-02 | Interfaz de usuarios y roles | 3 |
| T56 | HU-03 | Parámetros persistidos (umbral, años máximos, mínimos interno/externo) con los valores vigentes como iniciales; el motor los lee | 4 |
| T57 | HU-03 | Interfaz de parámetros | 2 |
| T58 | HU-07 | Catálogo: versión de malla vigente/no vigente y asignaturas por versión; migración sin pérdida de datos | 4 |
| T59 | HU-07 | Interfaz de versiones de malla | 2 |
| T60 | HU-09 | Consulta de homologables entre dos carreras FISEI con el motor determinista sobre correspondencias revisadas | 5 |
| T61 | HU-09 | Interfaz de la consulta | 3 |
| T62 | HU-21 | Motor N:M según CE-3 (unión de temas, suma de horas) con TDD | 6 |
| T63 | HU-21 | Solicitud, análisis e informe marcados «homologación por agrupación» | 5 |
| T64 | HU-21 | Interfaz de agrupación | 3 |
| T65 | HU-27 | Plan de pruebas: matriz HU → criterio → prueba automatizada o manual | 3 |
| T66 | HU-27 | E2E del flujo completo: login → solicitud → docentes → análisis → informe → remisión → finalización | 6 |
| T67 | HU-27 | Integración y E2E en GitHub Actions | 3 |
| T68 | HU-28 | Documentación: arranque con Docker Compose, manual por rol y configuración | 3 |
| T69 | HU-28 | Guion de demo con datos sintéticos | 2 |
| | | **TOTAL** | **102 h** |

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

## 7. Calendario

| Fecha | Pablo y Leslie | Edwin y Shirley |
| --- | --- | --- |
| 5–6 nov | Planning. PR y revisión cruzada de HU-01 y HU-18. Contrato de HU-02/03. | Cierre del Sprint 2: PR de HU-08 y aceptación de la PO. Contrato de estados HU-32/33. Crear los Issues del sprint. |
| 9–10 nov | HU-02 (T54–T55). | HU-32 (T44–T45). Plan de pruebas (T65). |
| 11–12 nov | HU-03 (T56–T57). | HU-33 (T46–T47). |
| 13 nov | HU-07 (T58). Solicitar validación de CE-2 y CE-3. | HU-17 (T50). |
| 16–17 nov | HU-07 (T59) y HU-09 (T60–T61). **Decisión sobre HU-21/22.** | HU-24 (T48) y HU-12 (T49). |
| 18–19 nov | HU-21 (T62–T63) si se valida CE-3; si no, E2E (T66). | HU-25 (T53); HU-22 (T51) si se valida CE-2. |
| 20 nov | E2E (T66) y CI (T67). | HU-22 (T52) o documentación (T68). |
| 23–24 nov | HU-21 (T64) o estabilización. Congelar funcionalidad al cierre del 24. | Documentación (T68) y guion de demo (T69). |
| 25 nov | Integración final y verificación de seguridad. | Aceptación de la PO, evidencias, Review y retrospectiva. |

Las filas del 5 al 12 de noviembre son el plan base. Si esas tareas no se ejecutaron, se recorren al período restante y se revisa la capacidad (sección 3).

## 8. Riesgos del sprint

| Riesgo | Mitigación |
| --- | --- |
| Avance del 5–12 sin confirmar; quedan 86,4 h efectivas frente a 102 h. | Confirmar en el Planning. Si falta capacidad, la PO decide; candidatas a salir: HU-21 y HU-22. |
| CE-2 y CE-3 no validados a tiempo. | Fecha límite el 16/11; sin validación, la historia vuelve al Product Backlog. No inventar reglas. |
| HU-03 cambia el umbral y las reglas que usan HU-14/15/16 y sus pruebas. | Valores iniciales idénticos a los actuales y pruebas existentes en verde; coordinar con Edwin. |
| HU-07 modifica el modelo del catálogo con datos reales ya cargados. | Migración que asigna la versión vigente a lo existente; respaldo previo de la base. |
| HU-32/33 cruzan Homologación y Solicitudes. | Contrato acordado antes de T44; clave interna (HU-36) y JWT en ambas llamadas. |
| E2E inestable en CI (Ollama no disponible en GitHub Actions). | E2E con agente simulado; la prueba con modelo real queda como evidencia local. |
| Cambios de varias personas en `App.tsx`, `Program.cs` y Gateway. | Coordinar como en sprints anteriores; PR pequeños y revisados el mismo día. |

## Coordinación de implementación

- Crear un Issue por HU antes de su código y vincular cada PR a él.
- Ramas `feature/hu-XX-descripcion` desde `develop`, Conventional Commits en español y PR a `develop` con revisión cruzada.
- TDD en reglas: RN-07 (HU-17), CE-2 (HU-22), CE-3 (HU-21) y parámetros (HU-03).
- El motor determinista conserva el veredicto; ninguna historia de este sprint delega decisiones al agente de IA.
- Sin secretos en Git ni en el frontend; los adjuntos (HU-12) y los PDF (HU-24) no se versionan.

## Definición de hecho y estado

PR hacia `develop` revisado por otro integrante; criterios comprobados por Shirley; pruebas en verde; Swagger actualizado; ejecución integrada con Docker Compose y evidencia reproducible. El registro de ejecución, la Review y la retrospectiva se documentan cuando ocurran.
