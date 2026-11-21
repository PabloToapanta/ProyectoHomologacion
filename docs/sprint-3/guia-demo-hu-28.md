# HU-28 — Arranque local, guía por rol y guion de demostración

**Issue:** [#73 — Documentación y guion de demo](https://github.com/PabloToapanta/ProyectoHomologacion/issues/73)  
**Revisión de la guía:** 21/11/2026 · **Base revisada:** `origin/develop` en `10a2b5e`  
**Estado:** borrador de documentación. El 21/11/2026 el stack HTTP aislado `hu28-demo-20261121` compiló y sus siete servicios quedaron saludables. Está disponible en `http://127.0.0.1:15173/login` (frontend) y `http://127.0.0.1:15080` (Gateway). La página de login respondió 200, el login de la cuenta local respondió 200 y una ruta protegida sin sesión respondió 401. Para suplir dos claves vacías se generaron valores aleatorios solo en el proceso de Compose; no se escribieron en `.env.sprint1`. El volumen del stack es independiente de los datos existentes. El overlay HTTPS solo pasó `config --quiet`; no se ejecutó con certificado ni en navegador. La revisión visual, el flujo completo, la aceptación de la PO y la revisión cruzada siguen pendientes.

Esta guía usa cuentas y registros sintéticos. No copiar contraseñas, tokens, certificados privados ni datos personales en el repositorio, capturas o evidencias.

## 1. Requisitos y preparación

- Docker Desktop/Engine con Docker Compose v2 y conexión para descargar imágenes.
- Puertos locales libres: 80 y 443 para el acceso HTTPS; 5080–5083 y 5173 para el modo HTTP local.
- PowerShell o una terminal desde la raíz del repositorio.
- Para HTTPS local: `mkcert` instalado en el equipo y certificado de desarrollo confiado por el navegador. No desactivar la validación TLS.
- Si se demostrará el análisis interno con el modelo real: servidor Ollama accesible desde los contenedores y modelo `qwen3:4b` disponible, según `docs/sprint-2/hu-18-analisis-interno.md`. Si Ollama no responde, la aplicación conserva la opción de revisión manual; no anunciar un resultado del agente.
- Una copia privada de `.env.example`, llamada `.env.sprint1`, con valores locales aleatorios. No utilizar contraseñas institucionales.

En PowerShell:

```powershell
Copy-Item .env.example .env.sprint1
notepad .env.sprint1
```

Completar los campos vacíos del archivo privado, en especial `AUTH_SQL_PASSWORD`, `AUTH_JWT_KEY`, `APP_SECRET_KEY`, `AUTH_JWT_ENCRYPTION_KEY` y `AUTH_ADMIN_PASSWORD`. Usar valores distintos y aleatorios según los comentarios de `.env.example`. El usuario `AUTH_ADMIN_USERNAME` será la cuenta Administrador local de demostración. No pegar secretos en chats, Issues o descripciones del PR.

Para generar por separado `APP_SECRET_KEY` y `AUTH_JWT_ENCRYPTION_KEY`, ejecutar dos veces y copiar cada resultado solo al campo correspondiente del archivo privado:

```powershell
python -c "import base64,secrets; print(base64.b64encode(secrets.token_bytes(32)).decode())"
```

No guardar el mismo valor en ambos campos. El stack aislado comprobado el 21/11 recibió suplentes aleatorios solo durante el comando de arranque porque esos dos valores estaban vacíos; para reiniciarlo después de detenerlo, completa y guarda ambos en `.env.sprint1`.

## 2. Arranque en HTTP local

Desde la raíz del repositorio, validar la configuración sin imprimirla (el comando `config` sin `--quiet` puede exponer secretos):

```powershell
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml -f compose.hu10.yaml config --quiet
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml -f compose.hu10.yaml up -d --build --wait --wait-timeout 180
```

Abrir:

- Aplicación: <http://127.0.0.1:5173>
- Gateway: <http://127.0.0.1:5080>
- Swagger local de APIs: Auth `:5081/swagger`, Catálogo `:5082/swagger`, Homologación `:5083/swagger`.

El entorno HTTP es para desarrollo local. No usarlo para transmitir credenciales fuera del equipo. El arranque aplica migraciones y crea el Administrador configurado; no crea carreras, asignaturas ni cuentas Docente/Coordinador de demostración.

## 3. Arranque con HTTPS local

El overlay `compose.hu34.yaml` publica la aplicación a través del Gateway en `localhost` y monta el certificado en solo lectura. Crear certificados **fuera del repositorio**, por ejemplo en una carpeta temporal del usuario:

```powershell
mkcert -install
$certDir = Join-Path $env:TEMP 'hu34-certs'
New-Item -ItemType Directory -Force $certDir | Out-Null
mkcert -cert-file (Join-Path $certDir 'localhost.pem') `
  -key-file (Join-Path $certDir 'localhost-key.pem') localhost 127.0.0.1 ::1
```

En `.env.sprint1`, añadir `HU34_CERT_DIR` con la ruta absoluta a esa carpeta, usando barras `/` (por ejemplo `C:/Users/<usuario>/AppData/Local/Temp/hu34-certs`). No versionar el certificado ni su clave privada. Si el equipo ya dispone de un certificado de desarrollo aprobado, puede usarlo siempre que los archivos se llamen `localhost.pem` y `localhost-key.pem`.

Arrancar los tres archivos Compose con el mismo nombre de proyecto:

```powershell
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml -f compose.hu10.yaml -f compose.hu34.yaml config --quiet
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml -f compose.hu10.yaml -f compose.hu34.yaml up -d --build --wait --wait-timeout 180
```

Abrir <https://localhost/login>. También se puede visitar `http://localhost/login` para observar la redirección configurada. El navegador debe confiar en el certificado; si muestra un error TLS, corregir el certificado en vez de ignorar la advertencia. La comprobación histórica de HU-34 está registrada en `docs/sprint-2/evidencia-hu-34-avance.md`; no sustituye una nueva ejecución de esta guía.

## 4. Preparar datos sintéticos

1. Iniciar sesión como el Administrador local configurado en `.env.sprint1`.
2. En **Usuarios**, crear una cuenta Coordinador y al menos dos cuentas Docente con contraseñas locales temporales. Entregarlas por un canal privado y no guardarlas en este documento.
3. En **Carreras** y **Asignaturas**, usar únicamente registros de demostración identificados como sintéticos. No copiar ni inventar contenidos de sílabos reales; si la demo requiere unidades o temas, usar un fixture sintético aprobado por el equipo o declarar que ese paso queda pendiente.
4. Antes de cada recorrido, anotar las cuentas de prueba en un registro local privado, no en el repositorio.

## 5. Manual breve por rol

### Administrador

1. Iniciar sesión con la cuenta local configurada.
2. En **Usuarios**, crear, activar/desactivar o cambiar el rol de cuentas Docente y Coordinador. El sistema no vuelve a mostrar la contraseña después de guardar; entregarla directamente al usuario.
3. En **Carreras** y **Asignaturas**, gestionar el catálogo de demostración. En **Programas analíticos**, la carga y confirmación son acciones separadas con revisión humana.
4. Usar **Validaciones académicas** cuando el recorrido requiera comprobar antigüedad o mínimos de cambio.
5. Cerrar sesión al terminar. El Administrador no realiza las acciones reservadas al Coordinador ni al Docente.

### Coordinador

1. Iniciar sesión con la cuenta creada por el Administrador; el acceso lleva a **Solicitudes**.
2. Registrar una solicitud de demostración, elegir el tipo de cambio y proporcionar los datos requeridos por el formulario. El número de trámite debe ser sintético.
3. Abrir el detalle, registrar las asignaturas aprobadas que correspondan al fixture y asignar dos docentes diferentes.
4. Consultar el estado de la solicitud y, si forma parte del escenario, revisar **Configuración**. Los valores académicos son parámetros del proyecto pendientes de validación institucional; no presentarlos como normativa oficial.
5. La generación del informe está disponible cuando el flujo y los análisis requeridos están completos. La remisión y finalización del Coordinador están pendientes de integrar y verificar en el `develop` revisado (véase §7).

### Docente

1. Iniciar sesión con una de las cuentas asignadas; el acceso lleva a **Mis solicitudes**.
2. Abrir solo una solicitud asignada y revisar los datos disponibles.
3. Registrar su análisis individual para el caso correspondiente. El agente entrega una sugerencia; el docente revisa y registra su decisión. No presentar la salida del agente como decisión académica oficial.
4. Repetir con la segunda cuenta Docente para comprobar que cada análisis es individual.
5. La pantalla del informe es de consulta. Remisión y finalización requieren la integración pendiente indicada abajo.

## 6. Guion de demo

Duración orientativa: 8–12 minutos, más el tiempo de inicio de contenedores. Usar un registro interno con datos sintéticos para que el ejemplo no dependa de documentación académica inventada.

1. **Arranque (1 min):** mostrar que Compose inicia los servicios y abrir el login. Para la revisión HTTPS, enseñar la URL `https://localhost` y la redirección HTTP sin mostrar credenciales.
2. **Administrador (2 min):** iniciar sesión, crear una cuenta Coordinador y dos Docentes de prueba, y mostrar el catálogo sintético.
3. **Coordinador (2 min):** registrar una solicitud interna sintética, seleccionar las asignaturas preparadas y asignar a los dos docentes.
4. **Docentes (2–3 min):** iniciar sesión separadamente con cada cuenta, abrir la solicitud asignada y guardar cada análisis individual. Mostrar que una cuenta no edita el análisis de la otra.
5. **Informe (1 min):** volver a la cuenta Coordinador y mostrar el informe disponible cuando se completen los análisis. Describir únicamente los campos y el estado que aparezcan en pantalla.
6. **Remisión (solo después de integrar HU-32, PR #86):** iniciar sesión como uno de los docentes asignados, abrir el informe y pulsar **Remitir al coordinador**. Volver a Coordinador y comprobar el estado **Remitida**.
7. **Revisión y finalización (solo después de integrar HU-33, PR #89):** como Coordinador, abrir la revisión del informe; si hay parejas **Sin acuerdo**, registrar una decisión propia con justificación; finalizar y comprobar que la solicitud queda en solo lectura. Mostrar eventos del historial solo si HU-25 (PR #85) está integrada.
8. **Calidad y cierre (1 min):** enseñar los checks de la PR y la matriz de pruebas solo si están visibles y actuales. Cerrar sesión y detener el stack conservando el volumen.

No afirmar que el flujo completo de remisión/finalización, adjuntos, auditoría o exportación PDF está disponible hasta verificar que sus PR correspondientes están integradas y pasar el recorrido de extremo a extremo.

## 7. Estado verificado frente a pendiente

| Elemento | Estado en esta guía |
| --- | --- |
| Compose base `compose.sprint1.yaml` y overlay de Solicitudes `compose.hu10.yaml` | Stack aislado compilado el 21/11; 7/7 servicios saludables. Login `/login`: 200; login local: 200; ruta protegida sin sesión: 401. Dos claves de prueba temporales no se guardaron. |
| Overlay HTTPS `compose.hu34.yaml` y rutas de login por rol | `docker compose config --quiet` pasó el 21/11 con valores de prueba; no se montó certificado ni se recorrió en navegador. |
| Roles Administrador, Coordinador y Docente; solicitudes, análisis e informe | Rutas y componentes presentes en la base `10a2b5e`; requieren comprobación manual de la demo. |
| Adjuntos HU-12 (#82) y exportación PDF HU-24 (#83) | PR abiertas en la revisión del 21/11; no dar por integradas hasta comprobar `develop`. |
| Remisión HU-32 (#86), auditoría HU-25 (#85) y finalización HU-33 (#89) | PR abiertas en la revisión del 21/11; los pasos 6–7 del guion dependen de integración y de verificar el recorrido en navegador. |
| Certificado, cuentas, datos de prueba, captura y aceptación de PO | Preparación/ejecución pendiente del equipo; no se adjuntan secretos ni resultados simulados. |

Antes de la demo, actualizar esta tabla con el SHA real de `develop`, el estado de cada PR, fecha, persona que ejecutó Compose, resultado observado y evidencias saneadas. Un check de CI o una prueba de servicio no equivale por sí solo a aceptación funcional de la PO.

## 8. Detener el entorno sin borrar los datos

Usar el mismo nombre de proyecto y archivos que se usaron al arrancar:

```powershell
docker compose --env-file .env.sprint1 -p fisei-uta-sprint1 -f compose.sprint1.yaml -f compose.hu10.yaml stop
```

Si se arrancó con HTTPS, incluir también `-f compose.hu34.yaml` en el comando. `stop` conserva el volumen de SQL. No usar `down --volumes` para conservar la base de demostración; cambiar el nombre del proyecto crea otro volumen y puede parecer que los datos desaparecieron.

## Referencias del repositorio

- `README.md`
- `docs/sprint-1/ejecucion-integrada.md`
- `docs/sprint-2/evidencia-hu-10.md`
- `docs/sprint-2/evidencia-hu-29.md`
- `docs/sprint-2/evidencia-hu-30.md`
- `docs/sprint-2/evidencia-hu-34-avance.md`
- `docs/sprint-3/planificacion-detallada.md`
- `docs/sprint-3/decisiones-validacion-ingeniero.md`
