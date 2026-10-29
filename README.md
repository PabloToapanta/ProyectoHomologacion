# Sistema de Homologación de Asignaturas — FISEI-UTA

Proyecto académico de la asignatura **Metodologías Ágiles**. Se desarrolla con Scrum + XP para analizar la posible homologación de asignaturas en cambios de carrera internos y externos.

> **Estado:** las siete historias del Sprint 1 tienen implementación integrada: HU-01, HU-04, HU-05, HU-06, HU-14, HU-15 y HU-16. La aceptación funcional, las revisiones pendientes y el cierre del Sprint deben registrarse por el equipo; no se deducen del código. La aplicación solo emite sugerencias, no decisiones académicas oficiales.

## Sprint 2 — planificación activa

El equipo confirma la entrega del Sprint 1. El trabajo principal del Sprint 2 se
planifica del **22 al 30 de octubre**, con integración y Review el **4 de noviembre**.
Compromiso: **40 PH y 69 h**, capacidad efectiva supuesta **76,8 h** en ocho jornadas.
Se priorizan HTTPS, JWT cifrado, clave interna y flujo base de solicitudes con análisis
asistido. Ver [planificación y coordinación](docs/sprint-2/planificacion-detallada.md)
y [registro de ejecución](docs/sprint-2/registro-evidencias.md). Son elementos
planificados; no se declaran implementados por esta actualización documental.

## Ejecución local integrada

Consultar la [guía de ejecución](docs/sprint-1/ejecucion-integrada.md): copiar `.env.example`
a `.env.sprint1`, completar las credenciales privadas y ejecutar desde la raíz:

```bash
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml up -d --build --wait --wait-timeout 180
python3 tests/sprint-1/smoke.py --env-file .env.sprint1
```

Frontend: <http://127.0.0.1:5173>. Una instancia SQL aloja bases separadas de Auth
y Catálogo. Conservar proyecto y volumen al actualizar; la guía explica cómo
reutilizar la demostración existente sin borrar sus datos.

El catálogo permite registrar, consultar y editar unidades, contenidos y resultados
de aprendizaje. Las pantallas de equivalencia, antigüedad y requisitos están en
`/validaciones`. HU-14 conserva múltiples orígenes por API; su formulario presenta
un par de asignaturas. Los tres casos de uso siguen siendo independientes.

## Equipo y roles

- **Shirley Amaguaña:** Product Owner, priorización, QA manual y aceptación.
- **Pablo Toapanta:** Scrum Master, autenticación y equivalencia (HU-01/HU-14), integración.
- **Leslie Coello:** desarrollo del catálogo de carreras y asignaturas, frontend (HU-04/HU-05).
- **Edwin Caraguay:** desarrollo de unidades/contenidos y validaciones de cambio (HU-06/HU-15/HU-16).

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

Consultar `AGENTS.md` y `docs/contexto-proyecto.md` para alcance, reglas de negocio y responsabilidades. Para codificar en paralelo, seguir `docs/codex/arranque-tres-desarrolladores.md`.

## Documentación

La planificación, contratos, criterios y evidencias están en `docs/`. Consultar
los resultados ejecutados de cada historia; código integrado no equivale a
aceptación ni a finalización del Sprint.
