# Instrucciones para agentes de programación

## Propósito
Desarrollar el Sistema de Homologación de Asignaturas FISEI-UTA conforme al documento de planificación de Scrum + XP.
Toda tarea debe estar vinculada a un Issue de GitHub y a sus criterios de aceptación.

## Alcance obligatorio del Sprint 1
- HU-01: inicio de sesión y protección de rutas.
- HU-04: CRUD de carreras.
- HU-05: registro y consulta de asignaturas.
- HU-06: unidades curriculares, contenidos y resultados de aprendizaje.
- HU-14: equivalencia en tres criterios (académico combinado, horas y créditos), según aclaración del equipo del 19/10/2026.
- HU-15: validación de aprobación y antigüedad.
- HU-16: mínimos de cambio interno y externo.

El Sprint 1 fue entregado según confirmación de Edwin y el Sprint 2 está en `develop` (cierre en PR #78–#80). Conservar sus reglas y pruebas. El alcance activo es el Sprint 3 según la sección siguiente.

## Sprint 3 activo — 5 al 25 de noviembre de 2026
- Consultar `docs/sprint-3/planificacion-detallada.md` y `docs/sprint-3/decisiones-validacion-ingeniero.md`. Issue de planificación #8; Issues por HU #63–#77; vincular cada implementación a su Issue.
- Calendario: desarrollo hasta el martes 24 (congelar al cierre); miércoles 25 integración, aceptación y Review. Al 20/11 quedan 3 jornadas (28,8 h efectivas supuestas) frente a unas 95 h sin código publicado; el equipo mantiene todas las historias y sigue el orden de prioridad del plan. Lo no aceptado el 24/11 vuelve al Product Backlog; no declarar avance no verificado.
- Pablo/Leslie: HU-02 usuarios, HU-03 parámetros, HU-07 mallas, HU-09 consulta FISEI, HU-21 agrupación, E2E y CI (HU-27). Edwin/Shirley: HU-32 remitir, HU-33 finalizar, HU-17 RN-07, HU-12 adjuntos, HU-24 PDF, HU-25 auditoría, HU-22 ruta alternativa, HU-37 reutilización del análisis, plan de pruebas (HU-27) y HU-28.
- Shirley conserva Product Owner y aceptación; ayudar al desarrollo no autoriza aceptar cambios propios sin revisión cruzada. HU-37 toca código de HU-18/20: acordar con Pablo y que él revise.
- Reglas académicas (respuestas del ingeniero del 20/11, lectura de fuentes públicas, no pronunciamiento institucional): implementar solo los alcances acotados de `decisiones-validacion-ingeniero.md` y marcarlos como reglas de análisis del proyecto. HU-21 solo N:1; HU-22 solo alerta sin bloqueo; RN-07 excluye la asignatura, no la solicitud; antigüedad 10 años provisional (el instructivo UTA 2023 menciona 5) y parametrizable en HU-03.
- Fuera: devolución y reapertura de informes, programa analítico de la universidad de origen (D2 de HU-19). No implementar HU adicionales por inferencia.
- Motor determinista conserva las tres dimensiones y el veredicto; agente con JSON validado, sin inventar evidencia; ninguna historia del Sprint 3 delega decisiones al agente.
- Seguridad: contraseñas con hash; HTTPS; token firmado y cifrado; claves solo en servidores; clave interna entre servicios además de JWT y autorización. Adjuntos y PDF no se versionan.
- Conservar el flujo de rama feature/hu-XX-descripcion, Conventional Commits en español y PR a develop. No realizar merge ni release sin autorización.

## Sprint 2 — histórico
- Planificación en `docs/sprint-2/planificacion-detallada.md`: HU-08, 10, 11, 18, 19, 20, 29, 30, 34, 35 y 36, más HU-31 y HU-23 adelantadas. Conservar sus contratos y pruebas.

## Stack fijado por el proyecto
- React, TypeScript, Vite; ASP.NET Core 8 Web API.
- YARP, SQL Server, RabbitMQ según el papel especificado en la arquitectura.
- xUnit, Docker Compose y GitHub Actions.
- Separación por microservicios; cada servicio es dueño de sus datos.
- Motor determinista independiente de LLM.

## Distribución histórica del Sprint 1 — tres desarrolladores con Codex
- **Pablo:** Scrum Master y desarrollo HU-01 (T1–T3) y HU-14 (T9–T10). Estimación técnica original: 15 h. T1–T3 incorporadas en `develop` con pruebas; la aceptación funcional corresponde a Shirley.
- **Leslie:** desarrollo HU-04 (T4–T5) y HU-05 (T6–T8). Estimación: 14 h.
- **Edwin:** desarrollo HU-06 (T13–T15), HU-15 (T11) y HU-16 (T12). Estimación: 16 h. Comenzar por HU-15/HU-16 con reglas y TDD, sin editar archivos compartidos de Pablo; HU-06 depende de modelos del catálogo de Leslie.
- **Shirley:** única Product Owner, Product Backlog, criterios de aceptación, pruebas manuales, evidencias Scrum/XP, Sprint Review y retrospectiva.
- **Revisión cruzada:** Pablo revisa a Leslie; Leslie revisa a Edwin; Edwin revisa a Pablo. Nadie acepta sus propios cambios sin revisión.
- **Contratos y propiedad de archivos:** Pablo es responsable inicial de los archivos de arranque React, autenticación, Gateway y comparador; Leslie mantiene carreras/asignaturas; Edwin añade unidades y reglas de antigüedad/cambio. No editar simultáneamente `Program.cs`, `Homologacion.sln`, configuración del Gateway ni `frontend/src/App.tsx` sin coordinar.
- **Clean Architecture:** cada microservicio de negocio separado en Domain, Application, Infrastructure y Api; interfaces en Application, implementaciones técnicas en Infrastructure, reglas deterministas en Domain. Gateway YARP y frontend React no requieren cuatro capas .NET artificiales.
- Consultar `docs/contexto-proyecto.md`, `docs/sprint-1/criterios-aceptacion.md`, `docs/arquitectura/decisiones-y-contratos.md` y `docs/sprint-1/planificacion-detallada.md`.

## Reglas de implementación
1. Consultar los criterios locales y tareas del Issue cuando GitHub sea accesible; no bloquear el trabajo si la sesión no tiene integración de GitHub.
2. Proponer y ejecutar autónomamente una solución simple, explicando decisiones relevantes y archivos afectados; detenerse a consultar solo si cambia el alcance o existe ambigüedad normativa/material.
3. Aplicar TDD a reglas y validaciones: primero prueba fallida, luego implementación, luego refactorización.
4. Mantener el cálculo de horas y créditos acotado a 100 %.
5. No inventar datos de sílabos ni normas. Si falta información, registrar 'información incompleta'.
6. La aclaración académica transmitida por el equipo el 19/10/2026 establece tres criterios que deben alcanzar individualmente un umbral configurable, inicialmente 80 %. No presentarla como normativa oficial no aportada ni inventar agregaciones o conversiones pendientes.
7. No almacenar contraseñas en texto plano ni guardar credenciales/secretos en Git.
8. Documentar endpoints en OpenAPI/Swagger e incluir mensajes de error apropiados.
9. No declarar tests exitosos ni funcionalidad terminada sin haberlos ejecutado y verificado.
10. Antes de intervenir en archivos de otro desarrollador, acordar el contrato o abrir una propuesta de cambio.

## Git y revisión
- Crear ramas `feature/hu-XX-descripcion` desde `develop`.
- Commits en español con formato Conventional Commits: `feat(auth): implementar inicio de sesión`.
- No hacer push, merge, force-push, reset destructivo ni releases por cuenta del agente sin permiso humano.
- Pull Request hacia `develop` y revisión cruzada entre desarrolladores.
- No modificar `main` fuera del proceso de publicación de una release aprobada.

## Entrega y calidad
- Cada historia tiene una prueba o evidencia que comprueba todos sus criterios de aceptación.
- Definición de Hecho: PR revisado, pruebas en verde, Swagger, aceptación del Product Owner y ejecución integrada con Docker Compose.
- La documentación debe distinguir lo planificado de lo efectivamente verificado.
