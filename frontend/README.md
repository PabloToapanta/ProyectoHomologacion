# Frontend — HU-01 / T3

Relacionado con [Issue #1](https://github.com/PabloToapanta/ProyectoHomologacion/issues/1). React, TypeScript y Vite, con organización por funcionalidades. T1 y T2 se consumen mediante su contrato existente y permanecen sin cambios.

## Estructura y colaboración

```text
frontend/
├── src/
│   ├── app/                    # Composición de providers y rutas
│   ├── features/
│   │   ├── auth/               # Login, sesión en memoria y guard de rutas
│   │   ├── admin/              # Panel mínimo de demostración
│   │   └── catalogo/           # Carpeta preparada para Leslie, sin funcionalidad
│   ├── shared/                 # Identidad visual, estilos base
│   ├── test/                   # Setup y datos sintéticos
│   └── main.tsx
├── e2e/                        # Chromium contra el stack real
├── scripts/run-e2e.mjs         # Lee credenciales privadas para E2E
├── vite.config.ts
├── Dockerfile
└── compose.t3.yaml
```

No había frontend ni archivos de Leslie en esta rama al comenzar T3. Se creó una base nueva para HU-01. La carpeta `src/features/catalogo` incluye una propuesta de integración, **pendiente de acuerdo con Leslie**; no se afirma haberla contactado ni aprobado cambios en su nombre. Antes de modificar puntos compartidos (`src/app/App.tsx`, `src/main.tsx`, `src/shared/`, dependencias/lockfile y configuraciones), acordar esos cambios con ella. Sus componentes, estilos y contratos de catálogo se desarrollan dentro de su funcionalidad.

La API pública de autenticación está en `features/auth/index.ts`: `AuthProvider`, `ProtectedRoute`, `useAuth` y tipos de sesión/rol. `useAuth().request('/api/...', init)` adjunta el JWT al Gateway y elimina la sesión ante 401. No acepta URLs externas. Cada módulo conserva el manejo de su propio contrato y errores.

## Comportamiento implementado

- `/login`: usuario, contraseña, alternancia para mostrar/ocultar, validación de campos obligatorios, carga y bloqueo de envíos duplicados.
- `POST /api/auth/login`: JSON `{ nombreUsuario, password }`, con el contrato de respuesta `{ accessToken, tokenType, expiresAtUtc }`. La contraseña se envía tal como se ingresó; el usuario se recorta como también hace Auth.
- Errores 400, 401, 5xx, respuesta inválida, conexión y timeout de 15 segundos: mensajes propios en español, sin mostrar cuerpos internos del servidor. Después de un fallo se limpia la contraseña y se permite reintentar.
- `/panel`: requiere sesión vigente y rol `Administrador`; muestra usuario, perfil y estado de sesión. Incluye navegación hacia catálogo y validaciones existentes. La adaptación visual del login y del panel se describe en `../docs/sprint-1/plan-refactor-frontend-mockups.md`.
- Docente/Coordinador son roles reconocidos, pero no pueden abrir este panel de administrador. Se muestra acceso restringido con opción de cerrar sesión.
- Rutas desconocidas se redirigen al flujo protegido. Sin sesión, el panel redirige a login.
- Logout, caducidad y una respuesta autenticada 401 eliminan la sesión. Un 403 conserva la sesión y corresponde al consumidor tratar el permiso de la operación.

React Router resuelve navegación/guards. Context administra la sesión y `fetch` se encarga del HTTP; no se añadieron bibliotecas de estado, de formularios, de componentes visuales o de decodificación JWT.

## Sesión y seguridad

El JWT se mantiene únicamente en memoria de React. No se escribe en localStorage, sessionStorage, cookies ni URL; tampoco se registra en consola. Recargar, cerrar la pestaña o abandonar la aplicación requiere volver a iniciar sesión. Esta es una decisión deliberada de T3, sin mecanismo de «recordarme» o refresh tokens.

Se interpretan los claims existentes para mostrar nombre/rol y se usa la menor vigencia entre `exp` y `expiresAtUtc`. El timer y los eventos de foco/visibilidad controlan la caducidad de una pestaña suspendida. Se cancelan logins pendientes y una respuesta tardía no puede restablecer una sesión cerrada.

La decodificación del token en el navegador **no comprueba su firma**. Es una ayuda de UI; el Gateway sigue siendo la autoridad para firma, algoritmo, emisor, audiencia, expiración y acceso a la API. El guard del navegador no reemplaza la protección 401/403 del backend. Cerrar sesión elimina la sesión local; T1 no ofrece revocación del JWT ya emitido antes de su expiración.

Las variables `VITE_*` no pueden contener secretos. Solo se utiliza la dirección del Gateway; jamás copiar la clave de firma, contraseñas SQL o credenciales de usuarios a un archivo Vite. Los secretos del stack/E2E se suministran por archivos privados fuera del repositorio o variables de entorno.

## Desarrollo

Requiere Node 24.15 o posterior de la rama 24; las versiones están fijadas en package.json y package-lock.json.

Desde `frontend/`:

```bash
npm ci
cp .env.example .env.local
npm run dev
```

Configurar `VITE_GATEWAY_URL`, por defecto `http://127.0.0.1:5080`. Debe ser un origen HTTP(S), sin credenciales, query, fragmento o subruta. Reiniciar Vite después de cambiarlo. `.env.local` está excluido de Git.

El navegador solicita `/api` en el mismo origen. Vite utiliza `VITE_GATEWAY_URL` como destino del [proxy de desarrollo](https://vite.dev/config/server-options#server-proxy), tanto en desarrollo como en `vite preview`, conservando `/api/auth/login`. En Docker lo hace Nginx. Este diseño permite el flujo existente sin añadir CORS ni reescribir Gateway. La URL del Gateway se configura en el servidor de frontend; no se hace una petición cross-origin directa desde React.

El servidor escucha en loopback, puerto 5173 estricto. El preview local usa 4173. El hosting del bundle debe resolver rutas SPA hacia index.html y reenviar `/api` al Gateway, como la configuración Nginx incluida.

## Tipos, pruebas y compilación

```bash
npm run typecheck
npm test
npm run build
```

Vitest + Testing Library cubren UI, API, roles, guards, sesión y caducidad. Los datos y JWT del fixture son sintéticos y no válidos como credenciales de producción. Las pruebas no modifican Auth ni Gateway. El workflow `frontend.yml` prepara estas mismas verificaciones para PR a develop; no se ejecutó remotamente en esta sesión.

## Stack real con Docker y Chromium

Desde la raíz del repositorio, copiar `src/gateway/.env.example` a un archivo **privado fuera de Git**, por ejemplo `/tmp/hu01-t3.env`, rellenar variables AUTH y aplicar `chmod 600`. Puede añadirse `FRONTEND_HTTP_PORT=5173`. El archivo de ejemplo del Gateway enumera los valores que ya usan T1/T2. Los servicios académicos no son necesarios para este login; para esta demostración pueden configurarse destinos deliberadamente inaccesibles, sin crear servicios nuevos.

```bash
docker compose --env-file /tmp/hu01-t3.env \
  -f src/auth-service/compose.yaml -f src/gateway/compose.t2.yaml \
  -f frontend/compose.t3.yaml -p hu01-t3-local up -d --build --wait
```

El overlay añade frontend con Nginx y deja los archivos base T1/T2 sin modificaciones. El contenedor recibe `VITE_GATEWAY_URL=http://gateway:8080` y publica solo loopback. El build contiene archivos estáticos; Nginx utiliza esa variable al arrancar para reenviar `/api`, soporta deep links y añade cabeceras de seguridad. No usar `vite preview` como servidor institucional. La publicación institucional requiere HTTPS.

Desde `frontend/`, con el stack listo:

```bash
npx playwright install chromium
node scripts/run-e2e.mjs /tmp/hu01-t3.env
```

El helper lee el archivo simple `VARIABLE=valor` sin comillas/interpolación, carga usuario/contraseña iniciales en variables del proceso hijo y no los coloca en argumentos. Si se usa un puerto distinto, definir `FRONTEND_HTTP_PORT` en ese archivo; alternativamente usar `E2E_BASE_URL`. También puede ejecutarse `npm run test:e2e` suministrando `E2E_ADMIN_USERNAME`, `E2E_ADMIN_PASSWORD` y la URL por entorno. Sin credenciales, la configuración falla explícitamente y no presenta pruebas omitidas como exitosas.

Playwright prueba escritorio y móvil con el frontend de producción, login real, contraseña incorrecta, acceso directo protegido, recarga, logout y 401 del Gateway. Se desactivan trazas/vídeos para no capturar JWT o credenciales; las capturas explícitas contienen el login vacío o el panel con un usuario de prueba. Artefactos de ejecución en `test-results/`, excluidos de Git.

Para detener el entorno desde la raíz:

```bash
docker compose --env-file /tmp/hu01-t3.env \
  -f src/auth-service/compose.yaml -f src/gateway/compose.t2.yaml \
  -f frontend/compose.t3.yaml -p hu01-t3-local down
```

`down` conserva los datos SQL del proyecto. Eliminar su volumen borra los usuarios de ese entorno de desarrollo.

Evidencia real, capturas y pendientes: `docs/sprint-1/evidencia-hu-01-t3.md`.


### Verificación visual de los mockups

`npm run test:mockups` inicia Vite en 127.0.0.1:5179 y ejecuta seis recorridos
Chromium en escritorio (1365 px), móvil (390 px) y compacto (320 px).
Usa API simulada y datos sintéticos; no necesita credenciales ni reemplaza
`npm run test:e2e`, que comprueba Gateway/Auth/Catálogo reales.
Cubre búsqueda, duplicados en modal, Escape/Tab/retorno de foco, eliminación
restringida, malla contextual, filtro por nivel, unidades y navegación de Docente.
Las capturas se guardan en `test-results/mockups/` (excluido de Git).

El catálogo conserva `/admin/carreras`; `?seccion=asignaturas&carrera=<UUID>` abre
la malla de una carrera. La sesión continúa en memoria: recargar exige login.
