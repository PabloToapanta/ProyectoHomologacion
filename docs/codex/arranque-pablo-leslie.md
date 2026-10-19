# Arranque para Pablo y Leslie con Codex CLI

Este documento describe el trabajo humano con asistencia de Codex. Codex actúa en el repositorio local; las decisiones sobre normas ambiguas, entregas y merges requieren confirmación humana.

## Preparación: revisión y merge del PR documental
Primero revisar el PR #16 (rama `feature/configuracion-sprint-1` → `develop`) y fusionarlo tras la revisión para que ambos reciban `AGENTS.md`, el contexto y el plan. No comenzar ramas nuevas basadas en una rama `develop` que todavía no contenga estas guías.

## Instalación y autenticación (cada usuario)
```bash
node --version
npm --version
npm install -g @openai/codex@latest
codex --version
codex login
codex login status
```
Elegir **Iniciar sesión con ChatGPT** (cada persona usa su propia cuenta Plus). La autenticación con API key puede ocasionar facturación API separada.

## Clonar y crear rama

Pablo:
```bash
git clone https://github.com/PabloToapanta/ProyectoHomologacion.git
cd ProyectoHomologacion
git switch develop
git pull --ff-only origin develop
git switch -c feature/hu-01-autenticacion
codex
```

Leslie, en su propio equipo:
```bash
git clone https://github.com/PabloToapanta/ProyectoHomologacion.git
cd ProyectoHomologacion
git switch develop
git pull --ff-only origin develop
git switch -c feature/hu-04-carreras
codex
```

Ambos deben tener permisos de escritura en el repositorio y un `git config user.name`/email propio si lo necesitan.

## Modelos y cuota
- Principal: **GPT-6.1 Sol** con razonamiento `medium`, si aparece.
- Alternativas: GPT-6 Sol o GPT-5.6 Sol.
- GPT-6 Luna para pequeños cambios repetitivos.
- GPT-6 Astra para bloqueos de arquitectura o depuración difícil.
- Usar `/model` para seleccionar, `/status` para revisar cuota y el modelo activo. Alternativa en `~/.codex/config.toml`: `model_reasoning_effort = "medium"`.
- No enviar una solicitud del tipo 'implementa todo el sprint': una HU o tarea autocontenida por ejecución.

## Primera instrucción de Pablo
> Lee AGENTS.md y docs/contexto-proyecto.md. Para trabajar HU-01 consulta docs/sprint-1/criterios-aceptacion.md, docs/arquitectura/decisiones-y-contratos.md y el Issue #1. Implementa T1 en esta rama: auth-service .NET 8 con usuarios y roles, contraseña hasheada, inicio de sesión JWT y un administrador de prueba seguro. Propón y aplica decisiones técnicas simples compatibles con el proyecto; documenta supuestos. Ejecuta el build y pruebas disponibles. No hagas push, merge ni commits sin que los revise. Al final resume los archivos cambiados, comandos ejecutados, criterios comprobados y el siguiente paso.

Después: T2/T3; crear nuevas ramas desde `develop` para HU-14, HU-15 y HU-16. No acumular siete historias en una rama.

## Primera instrucción de Leslie
> Lee AGENTS.md, docs/contexto-proyecto.md y docs/arquitectura/decisiones-y-contratos.md. Consulta los criterios del Issue #2 y docs/sprint-1/criterios-aceptacion.md. Implementa T4 en la rama actual: catálogo .NET 8 con entidad Carrera, persistencia SQL Server, CRUD, validación de duplicado por facultad y prohibición de eliminar carreras con asignaturas. Incluye tests y Swagger. Decide los detalles técnicos menores por tu cuenta respetando los contratos y registra supuestos. No cambies auth-service ni homologacion-service. Ejecuta build y pruebas, informa archivos modificados y errores; no hagas push ni merge por tu cuenta.

Después: HU-05 y HU-06 en ramas independientes, frontend del catálogo y datos académicos provistos por Edwin.

## Aceptación y entrega de cada tarea
1. Revisar `git diff`, `git status` y archivos nuevos. Ejecutar tests localmente.
2. Commit convencional en español: `feat(auth): agregar validación de credenciales`.
3. Push de `feature/*`; PR hacia `develop` enlazando Issue.
4. El otro desarrollador revisa el PR; Shirley valida criterios; Edwin recopila evidencia.
5. Después del merge, crear siguiente rama desde `develop` actualizado.

## Resolver límites de uso
Una sesión por tarea, preferir Sol/Medium y Luna para tareas pequeñas. Evitar ejecutar 20 subagentes o leer 41 páginas del PDF en cada turno. `AGENTS.md` es guía persistente; proporcionar al agente el contexto relevante para esa HU. Consultar `/status` y la sección Uso de ChatGPT para cuotas y restablecimientos.

## Si faltan archivos
El PDF original no se agrega por defecto al repositorio público. El agente no conoce automáticamente el PDF de este chat. Si hace falta una página, proporcionarla de forma explícita en el equipo/local y revisar que no incluya información no publicable.
