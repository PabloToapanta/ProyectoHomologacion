# Evidencia HU-01 — T3: login React y rutas protegidas

Fecha: 19 de octubre de 2026 (America/Guayaquil). Relacionado con [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1). Ejecución técnica asistida por Codex para Pablo; no sustituye revisión cruzada o aceptación de Shirley.

## Estado inicial y alcance

Git estaba limpio en `feature/hu-01-autenticacion`, HEAD `2f5a860`. Se consultaron AGENTS.md, contexto, criterios de aceptación, decisiones/contratos y el Issue #1. T1/T2 estaban implementadas y no había frontend en esta rama.

Se añadieron exclusivamente el frontend, su CI y esta evidencia con capturas. No se modificaron Auth Service, Gateway, sus contratos, sus pruebas, sus Compose ni la solución .NET. No se implementaron catálogo, homologaciones, informes, solicitudes ni historias posteriores.

## Archivos y decisiones

- `frontend/package.json`, `package-lock.json`, `tsconfig.json`, `vite.config.ts`: React + TypeScript + Vite, tipado estricto, versiones fijadas y proxy `/api` hacia `VITE_GATEWAY_URL`.
- `frontend/src/app/App.tsx`, `main.tsx`: composición, BrowserRouter y rutas `/login` y `/panel`.
- `frontend/src/features/auth/`: formulario, cliente HTTP, lectura de claims para UI, sesión en memoria y guard por autenticación/rol.
- `frontend/src/features/admin/AdminPanel.tsx`: panel mínimo con usuario, perfil, estado de sesión y logout.
- `frontend/src/features/catalogo/README.md`: carpeta reservada para Leslie y propuesta de integración de archivos compartidos.
- `frontend/src/shared/`: estilos adaptables a escritorio/móvil e identidad visual tipográfica con icono SVG sencillo.
- `frontend/src/**/*.test.*`, `src/test/`: 43 casos de componentes, API y ciclo de sesión.
- `frontend/e2e/auth.spec.ts`, `playwright.config.ts`, `scripts/run-e2e.mjs`: dos escenarios Chromium contra el stack real, con credenciales privadas por entorno y sin trazas/vídeos de red.
- `frontend/Dockerfile`, `nginx.conf.template`, `compose.t3.yaml`, `.dockerignore`: build de producción y servidor de archivos estáticos/proxy que se añaden mediante overlay, sin editar T1/T2.
- `frontend/README.md`, `.env.example`, `.gitignore`: ejecución, configuración sin secretos y exclusión de artefactos temporales.
- `.github/workflows/frontend.yml`: npm ci, tipos, pruebas y build preparados para PR a develop; no ejecutados en GitHub en esta sesión.

Solo se usan React Router, Context y fetch para el flujo. No se agregaron librerías de estado, formularios, componentes visuales o decodificación JWT.

El JWT permanece únicamente en memoria. La vigencia se obtiene de `exp` y `expiresAtUtc`, tomando el menor valor. Caducidad, logout y HTTP 401 eliminan la sesión; recargar requiere autenticarse de nuevo. La UI interpreta claims sin verificar la firma; el Gateway conserva toda la validación criptográfica y autorización de la API. No se comparte ni incorpora al frontend la clave HS256.

El panel requiere Administrador. Docente/Coordinador reciben una pantalla de acceso restringido con logout; no se añadieron pantallas o funciones de sus historias futuras. No se cambiaron políticas del Gateway.

### Coordinación con Leslie

No había archivos de frontend de Leslie que intervenir. La base nueva se implementó para HU-01. El README de `features/catalogo` registra la propuesta y los puntos compartidos que deben acordarse antes de una futura modificación/integración: rutas, arranque, shared, dependencias/lockfile y configuraciones. **No se afirma haber contactado a Leslie ni contar con su aprobación.** Su módulo quedó vacío de funcionalidades de HU-04/05/06.

## TDD y correcciones

Primero se prepararon pruebas con login, sesión y App sin implementar. Resultado real: **23 fallidas, 12 aprobadas**, 35 casos; registro temporal `/tmp/hu01-t3-red.log`. Las aprobadas iniciales eran rechazos que también satisfacía una implementación que siempre fallaba; los casos válidos y de UI detectaron el trabajo pendiente.

Tras implementar se aprobaron los 35 casos. Se añadieron verificaciones de caducidad, 401/403, destino de JWT, logout y respuestas tardías, hasta **43 casos aprobados**. Se corrigió una opción de Testing Library no admitida por sus tipos; el build Docker que había capturado esa versión se reconstruyó después de la corrección.

En la revisión visual se corrigió un espacio del título móvil. Una primera expectativa E2E asumía que «Atrás» siempre permanece dentro de la aplicación; Chromium puede volver a la página anterior externa. La comprobación final exige que no aparezca el panel tras logout y que abrir de nuevo `/panel` redirija a login. La inspección de un cuerpo 401 mediante `page.waitForResponse().json()` agotó el tiempo de instrumentación; el cuerpo se contrastó por HTTP en Frontend/Gateway/Auth y la aserción se realizó con APIRequest de Playwright, manteniendo la comprobación del mensaje visible de React.

## Resultados ejecutados y verificados

| Comprobación | Resultado real |
|---|---|
| Consulta del registro oficial npm e instalación con lockfile | Correcta; Node local 24.21.0, versiones compatibles con Node 24 |
| `npm run typecheck` desde frontend | Correcto, sin errores TypeScript |
| `npm test` desde frontend | **43 aprobadas**, 0 fallidas, cuatro archivos de pruebas |
| `npm run build` desde frontend | Build de producción correcto con Vite; genera dist |
| `npm audit --omit=dev --audit-level=low` | 0 vulnerabilidades publicadas reportadas para dependencias de producción en esta fecha |
| Docker Compose T1 + T2 + overlay T3 | Construcción y arranque correctos de frontend, Gateway, Auth y SQL Server |
| `node scripts/run-e2e.mjs /tmp/hu01-t3.env` desde frontend | **2 aprobadas**, 0 fallidas/omitidas, Chromium con stack real |
| `dotnet build Homologacion.sln --configuration Release --no-restore -m:1 -p:UseSharedCompilation=false` | Correcto, **0 advertencias, 0 errores** |
| `dotnet test Homologacion.sln --configuration Release --no-build -m:1 --logger trx --results-directory /tmp/hu01-t3-backend-results` | **115 aprobadas**, 0 fallidas/omitidas: Auth 43, Application 13, Gateway 59 |
| `git diff --name-only -- src tests Homologacion.sln docs/arquitectura/decisiones-y-contratos.md` | Vacío: backend, pruebas existentes, solución y contratos permanecen intactos |

Los runners y Docker utilizaron los permisos de sesión necesarios para sockets y descargas. El stack `hu01-t3-verification` usó credenciales aleatorias en un archivo privado de `/tmp`, no en frontend ni Git. Sus contenedores, volumen y archivo de credenciales se retiran al finalizar. Las capturas guardadas muestran exclusivamente una cuenta sintética (`admin-t3`) y nunca contraseña/JWT.

Playwright emitió un aviso de variables de color de la terminal (`NO_COLOR`/`FORCE_COLOR`); las dos pruebas terminaron correctamente y no detectaron excepciones JavaScript de la aplicación.

### Cobertura de frontend

- JSON y ruta de login compatibles con T1/T2; ninguna cabecera Bearer en el login.
- Estados de carga y bloqueo de envíos duplicados.
- Errores 400, 401, 500/502, conexión y respuestas 200 inválidas; contraseña limpiada y reintento habilitado.
- Interpretación de claims y fechas: tres roles reconocidos, rechazo de rol desconocido/tipo incorrecto, claims incompletos, token malformado, vencido o todavía no vigente.
- Acceso directo a panel sin sesión redirigido a login; sesión válida de administrador abre el panel.
- Roles Docente y Coordinador no abren el panel de administrador.
- Logout, remonte/recarga, timer y regreso a pestaña caducada eliminan el acceso.
- La petición autenticada adjunta Bearer únicamente a `/api/`; un 401 elimina la sesión y un 403 la conserva.
- Después de logout no se envían peticiones autenticadas; una respuesta de login tardía no restablece la sesión.
- No se almacenan JWT en localStorage/sessionStorage.

### Flujo real en Chromium

Escritorio: navegación directa a panel, redirección a login, contraseña incorrecta con error visible y 401 sin accessToken, login correcto contra SQL Server y acceso al panel, almacenes de navegador vacíos, recarga que exige login, nuevo login/logout, navegación posterior sin acceso al panel y consulta de `/api/carreras` que devuelve 401 sin token.

Móvil (390 × 844): login real, acceso al panel, logout y ausencia de desbordamiento horizontal. Escritorio verificado a 1365 × 900. Las capturas de las dos versiones se inspeccionaron visualmente.

Capturas conservadas:

- [Login de escritorio](evidencias/hu-01-t3/login-escritorio.png).
- [Panel de escritorio](evidencias/hu-01-t3/panel-escritorio.png).
- [Login móvil](evidencias/hu-01-t3/login-movil.png).
- [Panel móvil](evidencias/hu-01-t3/panel-movil.png).

## Criterios de aceptación HU-01

| Criterio | Estado técnico y evidencia |
|---|---|
| Credenciales correctas: administrador recibe JWT y entra al panel | **Verificado** mediante React → proxy de frontend → Gateway → Auth → SQL Server, en escritorio y móvil |
| Credenciales incorrectas: error y ningún token | **Verificado**: mensaje visible, contraseña limpiada, 401 sin accessToken; las rutas siguen protegidas |
| Ruta protegida sin token válido: Gateway responde 401 | **Verificado** en las pruebas existentes de T2 y mediante `/api/carreras` a través del frontend en el E2E real |

Los tres criterios funcionales tienen evidencia técnica. **HU-01 no se declara formalmente aceptada ni cerrada:** faltan revisión cruzada/PR, ejecución remota de CI y aceptación de Shirley con sus evidencias manuales. La integración de Catálogo/Homologación reales corresponde a sus historias e integración posterior y no se certifica con este panel mínimo. No se modificó el Issue. Durante la implementación y verificación no se realizaron commits, pushes o merges. Posteriormente, Pablo autorizó commits atómicos y el push de la rama; esta autorización no implica aceptación de HU-01 ni autoriza merges.
