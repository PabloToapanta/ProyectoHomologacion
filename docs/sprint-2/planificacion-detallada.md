# Planificación del Sprint 2

Relacionado con [Issue #8 — planificación general](https://github.com/PabloToapanta/ProyectoHomologacion/issues/8). Versión de coordinación del 27/10/2026. El usuario confirma la entrega del Sprint 1; las evidencias y decisiones históricas se conservan. No acredita implementación del Sprint 2.

Fuente editable: [documento Word ajustado](Planificacion_Sprint2_Ajustada.docx). Esta versión Markdown mantiene el mismo calendario y estimaciones.

Sistema de Homologación de Materias (FISEI-UTA) · 22 de octubre al 4 de noviembre de 2026 · Desarrollo principal: 22–30 de octubre · Cierre y Review: miércoles 4 de noviembre

Ajuste de calendario preparado el 27 de octubre. Se conserva el compromiso de 40 PH y 69 h; fechas, capacidad y riesgos se sincronizan con las ocho jornadas solicitadas. Las propuestas de coordinación quedan sujetas al Planning del equipo.

## 1. Punto de partida (Sprint 1)

Se entregaron las 7 historias comprometidas (26 PH) del Sprint 1 en 5 días hábiles. La referencia observada es 26 ÷ 5 = 5,2 PH por día del equipo. Se utiliza como proyección inicial, no como garantía de productividad ni conversión entre puntos y horas.

Ya existe: contraseñas con hash (ASP.NET Identity), token JWT firmado (HS256) con clave obligatoria de al menos 32 bytes tomada del entorno, validación de emisor, audiencia y caducidad en el Gateway, y token guardado solo en memoria en el frontend.

No existe aún: HTTPS (todo corre por HTTP en local), cifrado del contenido del token, ni una clave de aplicación que impida llamar a los servicios saltándose el Gateway.

## 2. Objetivo del sprint

Cerrar la brecha de seguridad pedida por el ingeniero (HTTPS, token cifrado y clave secreta de la aplicación) y habilitar el flujo base de una solicitud: el coordinador la registra, asigna a dos docentes y estos ven el análisis del agente de IA.

## 3. Capacidad

Se planifican 8 jornadas: jueves 22, viernes 23, lunes 26, martes 27, miércoles 28, jueves 29 y viernes 30 de octubre, más miércoles 4 de noviembre. No se asignan horas al fin de semana ni al 2 y 3 de noviembre. Se mantienen 3 h por integrante en cada jornada, incluida la del 4 de noviembre; la disponibilidad real debe confirmarla el equipo.

| Cálculo | Resultado | Referencia |
| --- | --- | --- |
| 4 integrantes × 3 h × 8 jornadas × 0,8 | 76,8 h | Capacidad efectiva; 96 h brutas |
| 26 PH ÷ 5 días × 8 jornadas | 41,6 PH | Proyección orientativa de una sola iteración |
| Compromiso del sprint | 40 PH · 69 h | Margen: 7,8 h efectivas |

Distribución por parejas: Pablo–Leslie tienen 40 h estimadas frente a 38,4 h efectivas; Edwin–Shirley, 29 h frente a 38,4 h. Se propone que Edwin–Shirley apoyen 2 h de T37 (pruebas de integración del agente), conservando el responsable de HU-18. Con esa coordinación, la carga queda en 38 h y 31 h. Es una propuesta para Planning, no una reasignación ya acordada. Shirley conserva la aceptación como PO y su tiempo no se contabiliza dos veces.

El trabajo principal se concentra hasta el viernes 30 de octubre. El miércoles 4 de noviembre se reserva para integración final, correcciones, comprobación de criterios y cierre; no se programa el inicio de historias nuevas. Los eventos y contingencias se consideran dentro del factor de enfoque del 0,8, no como horas adicionales. No se retiran HU-19/HU-20 automáticamente por el 2 y 3 de noviembre, porque esas fechas ya están excluidas de la capacidad.

Calendario de ejecución previsto

| Fecha | Pablo y Leslie | Edwin y Shirley |
| --- | --- | --- |
| 22–23 oct | Diseño de seguridad, HTTPS y contrato del token cifrado. | Modelo de solicitudes y contrato con Auth; inicio HU-10. |
| 26–27 oct | Emisión/validación del token y clave interna; adaptar frontend. | Registro de solicitudes y aprobadas; asignación de docentes. |
| 28–29 oct | Adaptador IA, análisis interno y orquestación determinista. | Vista docente, persistencia y análisis externo sobre el agente común. |
| 30 oct | Completar integración y entregar funcionalidades listas para aceptación. | Prueba del flujo integrado, resultados y correcciones prioritarias. |
| 4 nov | Estabilización, verificación de seguridad y apoyo a la demostración. | Aceptación de la PO, evidencias, Review y retrospectiva. |

## 4. Historias comprometidas

| HU | Historia | Prioridad | PH | Responsables |
| --- | --- | --- | --- | --- |
| HU-36 | Clave secreta de la aplicación (acceso solo por el Gateway) | Alta | 5 | Pablo Toapanta y Leslie Coello |
| HU-35 | Token de sesión firmado y cifrado | Alta | 5 | Pablo Toapanta y Leslie Coello |
| HU-34 | Acceso seguro por HTTPS | Alta | 3 | Pablo Toapanta y Leslie Coello |
| HU-18 | Agente de IA: análisis de homologación interna | Alta | 8 | Pablo Toapanta y Leslie Coello |
| HU-10 | Registrar la solicitud que llega por Quipux | Alta | 5 | Edwin Caraguay y Shirley Amaguaña |
| HU-11 | Registrar asignaturas aprobadas del estudiante | Alta | 3 | Edwin Caraguay y Shirley Amaguaña |
| HU-29 | Asignar dos docentes a la solicitud | Alta | 3 | Edwin Caraguay y Shirley Amaguaña |
| HU-30 | Docente: ver solicitudes asignadas y resultado del agente | Alta | 3 | Edwin Caraguay y Shirley Amaguaña |
| HU-19 | Agente de IA: análisis de homologación externa | Alta | 3 | Edwin Caraguay y Shirley Amaguaña |
| HU-20 | Guardar el resultado del análisis en la base de datos | Alta | 2 | Edwin Caraguay y Shirley Amaguaña |
|  | TOTAL: 10 historias |  | 40 |  |

HU-31 (revisión propia del docente, 5 PH) queda fuera del compromiso. Solo podrá incorporarse si las historias comprometidas cumplen sus criterios y existe capacidad real confirmada por la Product Owner.

## 5. Sprint Backlog

| Tarea | HU | Actividad | Horas |
| --- | --- | --- | --- |
| T16 | HU-34 | Certificado de desarrollo y HTTPS en el Gateway, con HSTS y variables en compose | 3 |
| T17 | HU-34 | El frontend usa el Gateway por HTTPS; redirección desde HTTP y pruebas | 2 |
| T18 | HU-35 | Auth: emitir token firmado y cifrado; la clave de cifrado se valida al arrancar | 4 |
| T19 | HU-35 | Gateway y servicios: descifrar y validar el token | 3 |
| T20 | HU-35 | El login devuelve nombre, rol y expiración; el frontend deja de leer el token y se ajustan sus pruebas | 3 |
| T21 | HU-35 | Pruebas: token alterado, sin cifrar o con otra clave es rechazado | 2 |
| T22 | HU-36 | Verificación de la clave de aplicación en Auth, Catálogo, Homologación y Solicitudes; comparación segura y fallo de arranque si falta. Coordinar su incorporación con T26. | 3 |
| T23 | HU-36 | El Gateway agrega la clave a cada petición hacia los servicios (transformación YARP) | 2 |
| T24 | HU-36 | Compose: no publicar puertos de servicios internos; APP_SECRET_KEY en .env y .env.example | 2 |
| T25 | HU-36 | Pruebas: la llamada directa sin clave se rechaza; por el Gateway funciona | 2 |
| T26 | HU-10 | solicitudes-service: entidad Solicitud, migración y API | 4 |
| T27 | HU-10 | Interfaz para registrar la solicitud (n.º de trámite Quipux, estudiante, tipo y carreras) | 4 |
| T28 | HU-11 | Entidad Asignatura aprobada (nota, fecha, matrícula) y API | 3 |
| T29 | HU-11 | Interfaz para registrar asignaturas aprobadas | 2 |
| T30 | HU-29 | API para asignar dos docentes distintos con rol Docente | 3 |
| T31 | HU-29 | Interfaz para asignar docentes | 2 |
| T32 | HU-30 | API de solicitudes asignadas a un docente y su resultado del agente | 2 |
| T33 | HU-30 | Interfaz del docente | 3 |
| T34 | HU-18 | Puerto y adaptador del agente (Ollama): modelo configurable y salida JSON validada | 4 |
| T35 | HU-18 | Prompt y reglas del análisis interno | 3 |
| T36 | HU-18 | Orquestación: el motor determinista da el veredicto y el agente lo justifica | 4 |
| T37 | HU-18 | Pruebas con agente simulado y una prueba con modelo real | 3 |
| T38 | HU-19 | Reglas del análisis externo (RN-05 y RN-06) sobre el mismo agente | 3 |
| T39 | HU-20 | Guardar el resultado en la base de homologación (migración) | 3 |
|  |  | TOTAL | 69 h |

Los diagramas, los modelos de pantalla y las ceremonias siguen fuera del Sprint Backlog, como se acordó con el ingeniero.

## 6. Historias nuevas de seguridad

La planificación ya incluye HU-34 para HTTPS, HU-35 para JWT firmado y cifrado y HU-36 para la clave secreta entre Gateway y servicios. Suman 13 PH y 26 h estimadas. Se conservan separadas para verificar cada necesidad. El requisito comunicado por el ingeniero se contrasta mediante criterios concretos: cifrado de transporte, protección del token y restricción del acceso interno. Las contraseñas siguen almacenadas con hash; la clave de servidor no se incluye en React ni en variables VITE_.

## HU-34: Acceso seguro por HTTPS

Número: 34

Usuario: Usuario registrado (Administrador, Docente o Coordinador)

Nombre historia: Acceso seguro por HTTPS

Prioridad en negocio: Alta

Riesgo en desarrollo: Medio

Puntos estimados: 3

Iteraciones asignadas: 2

Programador responsable: Pablo Toapanta y Leslie Coello

Descripción:
Como usuario registrado, quiero que mis credenciales y mi sesión viajen cifradas entre el navegador y el sistema, para que nadie pueda leerlas en la red.

Validación (criterios de aceptación):
Al abrir el sistema por HTTP, el Gateway redirige a HTTPS.
La contraseña del inicio de sesión solo se envía por una conexión HTTPS.
Las respuestas del Gateway incluyen la cabecera de transporte seguro (HSTS).

## HU-35: Token de sesión cifrado

Número: 35

Usuario: Usuario registrado (Administrador, Docente o Coordinador)

Nombre historia: Token de sesión cifrado

Prioridad en negocio: Alta

Riesgo en desarrollo: Alto

Puntos estimados: 5

Iteraciones asignadas: 2

Programador responsable: Pablo Toapanta y Leslie Coello

Descripción:
Como usuario registrado, quiero que mi token de sesión esté firmado y cifrado, para que su contenido (nombre y rol) no pueda leerse ni modificarse fuera del sistema.

Validación (criterios de aceptación):
El token emitido al iniciar sesión no puede leerse con una herramienta de decodificación de JWT sin la clave de cifrado.
Un token alterado, sin cifrar o cifrado con otra clave es rechazado con HTTP 401.
Si falta la clave de cifrado o es más corta de lo requerido, el servicio no inicia y no muestra su valor.
El inicio de sesión devuelve nombre, rol y expiración para la interfaz, y las rutas protegidas siguen funcionando para los tres roles.

## HU-36: Clave secreta de la aplicación

Número: 36

Usuario: Administrador

Nombre historia: Clave secreta de la aplicación

Prioridad en negocio: Alta

Riesgo en desarrollo: Medio

Puntos estimados: 5

Iteraciones asignadas: 2

Programador responsable: Pablo Toapanta y Leslie Coello

Descripción:
Como administrador, quiero que los servicios solo respondan a peticiones que traigan la clave secreta de la aplicación, para que nadie pueda acceder saltándose el Gateway.

Validación (criterios de aceptación):
Una petición directa a Auth, Catálogo, Homologación o Solicitudes sin la clave es rechazada.
Una petición hecha por el Gateway con la clave correcta funciona con normalidad.
Si la clave no está configurada o es demasiado corta, el servicio no inicia y no muestra su valor.
La clave no aparece en el repositorio, en los registros ni en las respuestas.

## 7. Cambios al Product Backlog

Pasan al Sprint 3: HU-08 (carga masiva de programas analíticos), HU-31, HU-23, HU-32 y HU-33 (revisión docente, informe y finalización).

La planificación base estima el Sprint 3 en 68 PH frente a una capacidad orientativa de 52–56 PH. Debe reconciliarse el total con el Product Backlog vigente antes de comprometerlo. HU-07, HU-09, HU-12, HU-17 y HU-24 suman 15 PH y son candidatas a revisión de prioridad, no recortes aprobados. El Sprint 3 mantiene inicio el jueves 5 y cierre el miércoles 18 de noviembre: terminar el desarrollo principal del Sprint 2 el 30 deja margen de estabilización hasta el 4, pero no adelanta formalmente el Sprint 3.

## 8. Riesgos del sprint

| Riesgo | Mitigación |
| --- | --- |
| El token cifrado cambia el login del frontend y sus pruebas. | Hacer HU-35 primero y de forma que el login devuelva nombre, rol y expiración, para que la interfaz no necesite leer el token. |
| Cerrar los puertos de los servicios puede romper pruebas y CI. | Hacer HU-36 en una rama propia y actualizar el compose y los workflows en el mismo pull request. |
| El agente local (Ollama) es lento o impreciso. | El motor determinista decide el veredicto; el agente solo justifica. Pruebas con agente simulado. |
| HU-30 depende del resultado de HU-18. | Empezar HU-30 con resultados simulados y conectarla al agente al final. |
| Disponibilidad y cierre del 4 de noviembre. | No contar el 2 y 3 de noviembre. Confirmar las 3 h del día 4; si se pierden, la capacidad baja a 67,2 h y las 69 h planificadas exceden el límite por 1,8 h. La PO debe ajustar alcance o disponibilidad. |
| Pull requests sin revisar a tiempo (en el Sprint 1 quedaron #27 y #29). | Revisar los pull requests el mismo día y dejar la evidencia en el registro. |
| HU-29 necesita docentes y coordinador con cuentas y roles disponibles. | Preparar cuentas de prueba mediante una carga controlada antes de la integración. La administración completa de usuarios HU-02 sigue fuera de este compromiso. |

## 9. Para confirmar con el ingeniero

Confirmar el criterio técnico de HU-35: token firmado y cifrado, algoritmos y claves adecuados a la librería elegida, sin exponer secretos. HTTPS protege el canal y no sustituye el hash de contraseñas. HU-36 debe proteger también solicitudes-service; la clave de aplicación complementa JWT y permisos por rol, no los reemplaza.

Confirmar las tres historias de seguridad separadas, sus criterios de aceptación y la responsabilidad de las pruebas de integración al incorporar solicitudes-service.

Confirmar 3 h por integrante el miércoles 4 de noviembre, el horario de Review y el avance real del 22 al 26 de octubre. Este documento distribuye trabajo planificado; no acredita tareas ya ejecutadas ni disponibilidad retrospectiva.

## Coordinación de implementación

- Crear un Issue específico por HU antes de comenzar su código; la existencia de la planificación no implica que los Issues ya estén creados.
- Pablo y Leslie: HU-34/35/36 y HU-18 (40 h antes de redistribución propuesta). Edwin y Shirley: HU-10/11/29/30/19/20 (29 h). El apoyo de 2 h en T37 es propuesta, no asignación aceptada.
- Antes de T26 acordar contrato de Solicitud, estados, identificadores y rutas; preservar Quipux como entrada externa, sin integrar su API.
- HU-29 requiere dos docentes distintos y validación de rol con el servicio Auth mediante un contrato acordado; no consultar directamente su base.
- HU-30 debe controlar en servidor que el docente solo vea solicitudes asignadas. Simular resultados durante desarrollo no equivale a aceptación integrada.
- HU-20 prepara base propia del servicio de homologación, sin reutilizar DbContext del catálogo o solicitudes.
- El LLM aporta análisis/justificación estructurada; el motor determinista conserva el veredicto. Sin evidencia o JSON válido, informar revisión manual/información incompleta y no inventar resultados.
- HTTPS, token cifrado y clave interna deben probarse juntos, incluyendo login y los tres roles. No colocar secretos en frontend ni logs.
- Sin carga de PDF HU-08, revisión docente HU-31, informes ni finalización en este compromiso.

## Definición de hecho y estado

PR hacia develop revisado por otro integrante; criterios comprobados por Shirley; pruebas unitarias/integración en verde; Swagger actualizado; Compose integrado y evidencia reproducible. No cerrar historias por crear un commit o por usar un doble de IA.

El registro de ejecución se completa en [registro-evidencias.md](registro-evidencias.md). La Review y retrospectiva se registran cuando ocurran.
