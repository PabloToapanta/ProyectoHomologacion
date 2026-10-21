# Ejecutar el incremento disponible del Sprint 1

Vinculado a INFRA-01 / Issue #14 y HU-01 / #1, HU-04 / #2, HU-05 / #3,
HU-14 / #5, HU-15 / #6 y HU-16 / #7. Este arranque no declara terminado el Sprint:
HU-06 todavía requiere su integración y Shirley debe registrar aceptación.

## Primera ejecución

Requisitos: Docker Engine con Compose v2, acceso a los registros de imágenes y
puertos locales 5080–5083 y 5173 disponibles. Python 3 solo se necesita para el
smoke; Node/.NET locales solo para ejecutar sus suites fuera de los contenedores.

Desde la raíz, copiar `.env.example` a `.env.sprint1` y completar los valores
vacíos con credenciales de desarrollo. Ambos archivos `.env.sprint1` y `.env.demo`
están ignorados por Git. La plantilla contiene nombres y puertos, no secretos.
La contraseña SQL debe cumplir la complejidad de SQL Server y la del administrador
las validaciones de Auth. La clave JWT debe tener al menos 32 caracteres aleatorios.
Se pueden generar valores con `python3 -c "import secrets; print(secrets.token_urlsafe(48))"`.
No usar contraseñas institucionales; proteger el archivo privado con permisos locales.

```bash
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml config --quiet
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml up -d --build --wait --wait-timeout 180
python3 tests/sprint-1/smoke.py --env-file .env.sprint1
```

`--wait` comprueba salud SQL y ejecución de los demás contenedores; no garantiza
por sí solo disponibilidad HTTP. El smoke espera el login hasta 90 segundos y
después verifica peticiones reales. Un fallo del smoke debe investigarse, no
interpretarse como aceptación del producto.

Para pruebas de navegador existentes, en `frontend`:

```bash
node scripts/run-e2e.mjs ../.env.sprint1
```

Este último comando ejecuta autenticación y catálogo y **crea datos sintéticos**
en el catálogo. Para comprobar solo autenticación sin crear registros académicos,
ejecutar Playwright con filtro `e2e/auth.spec.ts` e inyectar por entorno
`E2E_ADMIN_USERNAME`, `E2E_ADMIN_PASSWORD` y `E2E_BASE_URL` con los valores locales.
No colocar contraseñas en argumentos ni versionar archivos con tokens.

## Servicios y datos

| Servicio | Dirección local | Función |
|---|---|---|
| frontend | http://127.0.0.1:5173 | React; `/api` se reenvía al Gateway |
| gateway | http://127.0.0.1:5080 | YARP, login público y rutas académicas con JWT |
| auth-service | http://127.0.0.1:5081/swagger | Login, roles, emisión JWT |
| catalogo-api | http://127.0.0.1:5082/swagger | Carreras FISEI y asignaturas |
| homologacion-service | http://127.0.0.1:5083/swagger | Equivalencia HU-14, antigüedad HU-15 y mínimos de cambio HU-16 |
| auth-db | Sin puerto publicado | SQL Server, bases separadas por servicio |

Todos los puertos publicados están limitados a loopback. Los puertos y destinos
YARP se configuran en el archivo privado sin recompilar; en el navegador se usa
el mismo origen del frontend y Nginx conecta con `gateway:8080`.

La instancia física SQL es compartida únicamente para reducir recursos locales.
Auth usa `HomologacionAuth` y Catálogo `HomologacionCatalogo` por defecto; cada
servicio aplica sus propias migraciones y no consulta datos del otro. Homologación
no necesita base para estas reglas. Se usa el usuario administrador SQL solo en
este entorno de desarrollo; no es una configuración de producción.

El administrador se crea mediante el seeding existente de Auth. No se precargan
carreras reales ni planes analíticos. Un catálogo vacío en una instalación nueva
es válido; sus lecturas y la restricción FISEI siguen comprobándose por smoke.

## Actualizar o detener conservando datos

Mantener el **mismo nombre de proyecto**, archivo privado, contraseña SQL y nombre
de base. El volumen es `<proyecto>_auth-data`. Cambiar el nombre de proyecto crea
otro volumen y puede dar la apariencia de pérdida de datos.

```bash
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml up -d --build --wait --wait-timeout 180
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml ps
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml stop
```

`stop` conserva datos. No usar `down --volumes` para actualizar una demostración.
No imprimir ni publicar `docker compose config` completo: puede mostrar secretos;
usar `config --quiet` o `config --services`.

### Demostración previa de Pablo

Para conservar sus datos, el proyecto sigue siendo `fisei-uta-demo`, el archivo
privado `.env.demo` y `CATALOGO_DATABASE=HomologacionCatalogoDemo`. El controlador
local `.env.demo.control.sh` pasa a usar el Compose versionado. La configuración
anterior `.env.demo.compose.yaml` ya no es necesaria para ese arranque.
No se debe ejecutar el proyecto nuevo sobre los mismos puertos mientras la demo
anterior esté activa; tampoco cambiar su nombre de base o su volumen.

Los registros históricos con facultades sintéticas siguen identificados para
revisión y se preservan; esta integración no los reasigna a FISEI. Crear datos
reales o corregir históricos requiere la decisión explícita de sus responsables.

## Qué verifica el smoke

Sin mocks: login real con SQL, rechazo de credenciales, protección JWT por Gateway,
consulta de carreras y asignaturas existentes, rechazo de otra facultad, cinco
escenarios HU-15, siete escenarios HU-16 y errores de datos, fixture sintético HU-14 con evidencia y estado
incompleto para múltiples fuentes, y frontend HTTP 200. No escribe datos académicos.

HU-14, HU-15 y HU-16 conservan endpoints y resultados independientes: `Elegible` no significa
`Homologable`, y cumplir mínimos no representa una aprobación institucional. La cantidad
de homologables de HU-16 es una entrada declarada y debe provenir de evaluaciones completas
verificadas; no se calcula a partir del catálogo ni se infiere procedencia institucional.
No se agrega un flujo
combinado de solicitudes, una UI de homologación, IA, PDF ni nuevas normas.
