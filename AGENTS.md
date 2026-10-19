# Instrucciones para agentes de programación

## Propósito
Desarrollar el Sistema de Homologación de Asignaturas FISEI-UTA conforme al documento de planificación de Scrum + XP.
Toda tarea debe estar vinculada a un Issue de GitHub y a sus criterios de aceptación.

## Alcance obligatorio del Sprint 1
- HU-01: inicio de sesión y protección de rutas.
- HU-04: CRUD de carreras.
- HU-05: registro y consulta de asignaturas.
- HU-06: unidades curriculares, contenidos y resultados de aprendizaje.
- HU-14: cálculo de equivalencia en cuatro dimensiones.
- HU-15: validación de aprobación y antigüedad.
- HU-16: mínimos de cambio interno y externo.

No adelantar agentes de IA, informes PDF, asignación de docentes ni carga automática de PDF del Sprint 2/3.

## Stack fijado por el proyecto
- React, TypeScript, Vite; ASP.NET Core 8 Web API.
- YARP, SQL Server, RabbitMQ según el papel especificado en la arquitectura.
- xUnit, Docker Compose y GitHub Actions.
- Separación por microservicios; cada servicio es dueño de sus datos.
- Motor determinista independiente de LLM.

## Distribución de trabajo vigente
- Pablo: HU-01 (auth-service, Gateway y login), HU-14, HU-15 y HU-16 (homologacion-service y pruebas).
- Leslie: HU-04, HU-05, HU-06 (catalogo-service, datos académicos y frontend del catálogo).
- Shirley: Product Owner, aceptación y priorización.
- Edwin: análisis funcional, datos de prueba, pruebas manuales, evidencias y documentación técnica.
- Consultar `docs/contexto-proyecto.md` para las reglas de negocio y el alcance, `docs/sprint-1/criterios-aceptacion.md` para cada historia y `docs/arquitectura/decisiones-y-contratos.md` cuando se alteren contratos.

## Reglas de implementación
1. Leer la historia, criterios y tareas del Issue antes de cambiar archivos.
2. Proponer y ejecutar autónomamente una solución simple, explicando decisiones relevantes y archivos afectados; detenerse a consultar solo si cambia el alcance o existe ambigüedad normativa/material.
3. Aplicar TDD a reglas y validaciones: primero prueba fallida, luego implementación, luego refactorización.
4. Mantener el cálculo de horas y créditos acotado a 100 %.
5. No inventar datos de sílabos ni normas. Si falta información, registrar 'información incompleta'.
6. El umbral de 80 % en cuatro dimensiones es una **propuesta del equipo** pendiente de contrastación normativa; hacerlo configurable.
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
