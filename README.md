# Sistema de Homologación de Asignaturas — FISEI-UTA

Proyecto académico de la asignatura **Metodologías Ágiles**. Se desarrolla con Scrum + XP para analizar la posible homologación de asignaturas en cambios de carrera internos y externos.

> **Estado:** inicio del Sprint 1. El software aún no está implementado ni validado. Las decisiones académicas oficiales quedan fuera del sistema: la aplicación solo emite sugerencias.

## Equipo y roles

- **Shirley Amaguaña:** Product Owner, priorización y aceptación.
- **Pablo Toapanta:** Scrum Master, desarrollo técnico e integración.
- **Leslie Coello:** desarrollo del catálogo y frontend.
- **Edwin Caraguay:** análisis académico, documentación y pruebas manuales.

## Sprint 1 — 15 al 21 de octubre de 2026

Historias comprometidas (26 puntos):

- HU-01: inicio de sesión por roles.
- HU-04: gestión de carreras.
- HU-05: gestión de asignaturas.
- HU-06: gestión de unidades curriculares y contenidos.
- HU-14: equivalencia de asignaturas.
- HU-15: validación de aprobación y antigüedad.
- HU-16: validación de requisitos de cambio interno y externo.

**No se trasladan estas historias al siguiente sprint.** Solo se considerará terminada una historia cuando sus criterios de aceptación se hayan comprobado.

## Tecnologías previstas

- Frontend: React, TypeScript y Vite.
- Backend: ASP.NET Core 8 Web API y YARP como Gateway.
- Base de datos: SQL Server (propiedad de datos por servicio).
- Mensajería: RabbitMQ cuando existan flujos asíncronos.
- Pruebas: xUnit; integración continua: GitHub Actions.
- Despliegue local: Docker Compose.
- Agentes IA: planificados para Sprint 2, no forman parte del núcleo determinista del Sprint 1.

## Flujo de colaboración

1. Se trabaja desde `develop` creando `feature/hu-XX-descripcion`.
2. Cada cambio se relaciona con un Issue.
3. Se usa el estilo **Conventional Commits**, con descripción en español: `feat(catalogo): registrar carreras`.
4. Se abre un Pull Request de la rama `feature/*` hacia `develop`; otro integrante revisa y se ejecutan las pruebas.
5. Para la entrega se prepara `release/0.1.0` y, tras su validación, se publica en `main`.

## Contexto para Codex

Consultar `AGENTS.md` y `docs/contexto-proyecto.md` para alcance, reglas de negocio y responsabilidades. Para codificar en paralelo, seguir `docs/codex/arranque-pablo-leslie.md`.

## Documentación

La planificación del Sprint 1, las decisiones arquitectónicas, los criterios de aceptación y el registro de evidencias se agregarán en `docs/`. El repositorio se encuentra en preparación.
