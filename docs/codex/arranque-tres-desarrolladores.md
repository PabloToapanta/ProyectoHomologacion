# Inicio de desarrollo paralelo — Pablo, Leslie y Edwin

## Requisitos previos
1. Cada desarrollador debe aceptar la invitación a GitHub y disponer de Git, SDK .NET 8, Node.js y Codex con su cuenta.
2. Leer `AGENTS.md`, `docs/contexto-proyecto.md`, `docs/sprint-1/criterios-aceptacion.md` y `docs/arquitectura/decisiones-y-contratos.md`.
3. Revisar y fusionar el PR que modifica la redistribución hacia `develop`. Cada persona debe crear su rama desde un `develop` actualizado.
4. No subir claves, datos reales ni documentación restringida al repositorio público. Conventional Commits en español.
5. Los tres aplican **Clean Architecture** dentro de los microservicios de negocio (Domain, Application, Infrastructure, Api); frontend React y Gateway YARP no requieren cuatro proyectos .NET.

## Responsabilidades y orden de ramas

| Persona | Rama inicial | Issue | Siguiente rama | Alcance |
|---|---|---|---|---|
| Pablo | `feature/hu-01-autenticacion` (existente) | #1 | `feature/hu-14-equivalencia` | Completar T3 y después comparador T9/T10 |
| Leslie | `feature/hu-04-carreras` | #2 | `feature/hu-05-asignaturas` | Catálogo base y frontend T4–T8 |
| Edwin | `feature/hu-15-antiguedad` | #6 | `feature/hu-16-requisitos`, `feature/hu-06-unidades` | Validaciones puras T11/T12 y unidades T13–T15 |

## Prompts iniciales

### Pablo: completar HU-01
> Lee AGENTS.md, el contexto y los criterios de HU-01. T1 y T2 están codificadas en la rama actual: NO rehacerlas. Implementa T3: login React + TypeScript + Vite consumiendo `POST /api/auth/login` por YARP, gestión de errores, rutas protegidas, cierre de sesión y pruebas. Crea el arranque del frontend compartido y avisa qué archivos compartidos modificaste. Los módulos de catálogo de Leslie deben ir en `frontend/src/features/catalogo`, mientras mis pantallas de autenticación van en `frontend/src/features/auth`. Ejecuta build y pruebas; no commits ni push sin autorización.

### Leslie: comenzar HU-04
> Lee AGENTS.md, contexto del proyecto, contratos API e Issue #2. Implementa HU-04 con Clean Architecture dentro de `catalogo-service`, cuatro proyectos Domain/Application/Infrastructure/Api, SQL Server, CRUD de Carrera y restricciones por duplicados y eliminación. Escribe pruebas, prepara Swagger y documenta contratos. Coordina las entidades de Asignatura que Edwin necesitará para HU-06. No edites Auth, Gateway ni módulos de homologación. Evita crear otro frontend independiente: una vez integrado el arranque de React de Pablo, usa `frontend/src/features/catalogo`. No commits ni push sin autorización.

### Edwin: comenzar HU-15
> Lee AGENTS.md, contexto y aceptación de HU-15 / Issue #6. Desarrolla las reglas deterministas de aprobación y antigüedad máxima de 10 años, empezando por pruebas xUnit para menos de 10, exactamente 10, más de 10, reprobado y no cursado. Aplica Clean Architecture y evita dependencias EF Core, HTTP y LLM en Domain. Coordina con Pablo el nombre y contrato del servicio `homologacion-service`: no sobrescribas su `Program.cs`, `Homologacion.sln` ni motor comparador. Prepara módulo autocontenido para integración por PR. No hagas commits ni push sin autorización.

## Dependencias y estrategia de integración

- **Catálogo:** Leslie define Carrera/Asignatura. Edwin solo implementa HU-06 cuando estén integradas; hasta entonces puede desarrollar HU-15/HU-16.
- **Homologación:** Pablo define el contrato de evaluación HU-14; Edwin añade módulos independientes para antigüedad/requisitos. Acuerdan estructura de Domain y punto de composición antes de integrar.
- **Frontend:** Pablo publica bootstrap compartido; Leslie implementa componentes de catálogo en directorios propios.
- **Solución y composición:** evitar editar simultáneamente `Homologacion.sln`, `frontend/src/App.tsx`, `Program.cs` común y configuración de YARP. Integrar con PR secuenciales si coinciden archivos.
- **CI:** cada PR hacia `develop` ejecuta pruebas; ningún test se declara aprobado por la mera presencia del archivo.
- **Revisiones:** Pablo revisa a Leslie, Leslie revisa a Edwin, Edwin revisa a Pablo; Shirley valida criterios de aceptación.

## Comandos de arranque (Leslie y Edwin)

```bash
git clone https://github.com/PabloToapanta/ProyectoHomologacion.git
cd ProyectoHomologacion
git switch develop
git pull --ff-only origin develop
git switch -c feature/hu-04-carreras   # Leslie
# o git switch -c feature/hu-15-antiguedad   # Edwin
codex
```

Verificar `git status` antes de invocar Codex, y usar un modelo disponible en su plan y /status para comprobar límites.

## Calidad y evidencia
- Registrar commits convencionales en español y PR enlazando Issue, sin cerrarlo hasta aceptación.
- Mantener pruebas TDD, integración con Docker, Swagger y evidencias del Sprint Review.
- Fecha objetivo 21/10/2026: no eliminar historias comprometidas; documentar riesgos y criterios pendientes en lugar de afirmar entregas inexistentes.
