# Validación integrada HU-04/HU-05

19–20/10/2026, Leslie con asistencia de Codex. Rama de seguimiento: `feature/hu-05-validacion-integrada`, creada desde develop (`3ac1a1d`) después de la fusión del PR #20. No se modifica la entrega ya revisada en su rama original.

## Revisión y estado de entrega

- PR #20 aprobado por Pablo y fusionado en develop. CI de catálogo y frontend correctos en el commit `5224cf1`.
- La aceptación funcional de Shirley permanece pendiente; el Issue #3 no debe cerrarse por la sola revisión de código.
- Acta preparada: `docs/sprint-1/aceptacion-hu-05.md`. Incluye pasos de aceptación y contrato de entrega para Edwin/HU-06. No se han enviado mensajes externos ni se ha marcado aceptación humana.

## Correcciones necesarias para integrar

1. Gateway: se permite GET en `/api/carreras/{id}` conservando PUT/DELETE y la política JWT existente. La carga T8 necesita esa consulta. Pruebas previas: 1 fallida (405 en vez de 200) y 23 correctas; después, suite Gateway completa con **61 correctas**. Evidencias `hu-05-integracion/gateway-red.trx` y `gateway-green.trx`.
2. Se añade `compose.hu05.yaml` reutilizando servicios de Auth, Gateway, Catálogo y frontend mediante extends, con contextos de compilación explícitos y una red común. SQL Server conserva bases/volúmenes independientes para Auth y Catálogo. Homologación no forma parte del stack de esta validación.
3. Se retiran estilos inline de los enlaces nuevos para respetar la política CSP del frontend de producción (`style-src 'self'`). Se usa una clase CSS local del catálogo. Tras inspeccionar capturas, se fija un ancho mínimo de tabla con desplazamiento horizontal para evitar texto comprimido en móvil y se conserva el contraste del botón activo incluso durante hover.
4. Se añaden scripts de arranque y validación reproducible. La configuración privada `.env.hu05-validacion` se genera aleatoriamente, está excluida de Git y no se imprime. No hay contraseñas/JWT reales en evidencias.

## Recuperación del entorno local

Docker Desktop fallaba antes de iniciar contenedores por sockets temporales inaccesibles en `%LOCALAPPDATA%/Docker/run` y `%LOCALAPPDATA%/docker-secrets-engine`. Se verificó que los procesos estaban detenidos y que las carpetas solo contenían sockets vacíos. Se conservaron en carpetas de respaldo, sin borrar imágenes, volúmenes, datos ni preferencias, y se reinició Docker. El motor respondió como versión **28.5.1**.

La clase de fallo y la conservación de las carpetas temporales están descritas en el [reporte del repositorio de Docker](https://github.com/docker/desktop-feedback/issues/460). No se hizo factory reset ni se desinstaló Docker.

## Comprobaciones locales previas al recorrido real

- `docker compose ... config --quiet`: configuración válida. Contextos apuntan a los directorios correctos y los secretos quedan fuera de los contextos de compilación.
- Suite frontend: **84 correctas** con `--maxWorkers=2`; TypeScript y Vite compilaron. Primer intento simultáneo a la preparación pesada de Docker: 83 correctas y un timeout de 5 segundos en la prueba larga de navegación. Se acotó el timeout de esa prueba a 15 segundos y se redujo el paralelismo de la ejecución local, sin cambiar configuración global de Vite. Se conservaron ambos resultados JSON.
- Navegador de pruebas instalado en la caché ignorada de node_modules; las pruebas E2E no simulan servicios y no guardan trazas con credenciales.

## Ejecución del stack y pruebas reales

Se ejecutó `docker compose --env-file .env.hu05-validacion -f compose.hu05.yaml -p hu05-validacion up -d --build --wait --wait-timeout 240`. Se compilaron Auth, Gateway, Catálogo y frontend, se crearon volúmenes nuevos del proyecto hu05-validacion y los seis contenedores quedaron en ejecución. Ambas bases SQL Server respondieron healthy. No se borraron datos de otros proyectos.

| Comprobación real | Resultado y evidencia |
|---|---|
| Login por Gateway/Auth y protección sin sesión | 200 para credenciales correctas; 401 para incorrectas o ausencia de sesión |
| CRUD de carrera | 201 creación, 200 consulta individual/edición, 409 duplicado, 204 eliminación sin asignaturas |
| Registro de asignaturas | 201 válido; 400 para nivel/créditos/horas cero o negativos; 404 carrera inexistente |
| T8 por API real | Álgebra Lineal, nivel 1, 3 créditos, 144 horas. Ejecutar la carga dos veces conserva un único registro |
| Consulta de malla | Aislamiento por carrera y orden correcto de nivel 1 antes de nivel 2 |
| Restricción de eliminación | DELETE carrera con asignaturas devuelve 409 y conserva datos |
| Concurrencia | Dos solicitudes simultáneas con el mismo código: exactamente una 201 y otra 409 |
| Integridad en SQL Server 16.0.4305.5 | Dos migraciones aplicadas; índice único rechaza duplicado con 2601/2627, FK bloquea eliminación con 547 y CHECK rechaza horas cero. Pruebas directas revertidas con transacciones; cero asignaturas huérfanas |
| Navegador de escritorio | Login → panel → carreras → registro → asignaturas → validación → registro → bloqueo de borrado → regreso → logout; sin excepciones de página ni violaciones CSP |
| Navegador móvil (390×844) | Login, navegación, datos reales, tabla desplazable, ausencia de desbordamiento de página y recarga que exige iniciar sesión nuevamente |

Evidencias: [HTTP real](evidencias/hu-05-integracion/http-real.txt), [SQL Server](evidencias/hu-05-integracion/sqlserver-real.txt), [navegador](evidencias/hu-05-integracion/navegador-real.txt), [contenedores](evidencias/hu-05-integracion/compose-estado.txt) y capturas en `evidencias/hu-05-integracion/navegador/`. Se inspeccionaron capturas de escritorio y móvil.

El primer intento del lanzador E2E encontró una ruta relativa de registro afectada por el cambio del directorio de trabajo; se corrigió usando rutas absolutas sin cambiar globalmente de directorio. La primera ejecución completa encontró que el selector por texto de etiqueta no coincidía con el select real aunque su nombre accesible era Carrera; se usó el rol combobox. Se conservaron sus errores en `navegador-primer-intento.txt`. La ejecución posterior de las dos pruebas fue correcta. La prueba móvil prepara sus datos propios vía API real, sin depender del orden ni de registros dejados por otras pruebas.

## Repetir la validación

Requiere Docker Desktop activo, SDK .NET 8 y Node 24 configurado. Desde la raíz:

```powershell
./scripts/iniciar-hu05.ps1
./tests/catalogo-service/smoke-hu05-integrado.ps1
./scripts/e2e-hu05.ps1
```

Para instalar el navegador de pruebas una sola vez desde frontend, con Node 24:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = Join-Path (Get-Location) 'node_modules/.cache/playwright'
npx playwright install chromium
```

La comprobación SQL directa usa `tests/catalogo-service/hu05-integridad.sql` en sqlcmd del contenedor catalogo-db, tomando la contraseña del entorno del propio contenedor sin imprimirla. No usa una instancia SQL externa ni datos reales.

El primer script conserva una configuración privada existente para no cambiar las credenciales de los volúmenes ya creados. No eliminarla para regenerar contraseñas sobre bases existentes. El segundo deja datos sintéticos identificables para la demostración de Shirley y evita imprimir tokens. El tercero genera capturas de escritorio/móvil.

Para detener el proyecto sin borrar volúmenes:

```powershell
docker compose --env-file .env.hu05-validacion -f compose.hu05.yaml -p hu05-validacion down
```

## Pendientes humanos

- Shirley ejecuta el recorrido y registra aceptación u observaciones en el acta.
- Edwin recibe el contrato de Asignatura para HU-06; no se inventan temas/resultados ni se adelantan esas entidades.
- Publicar y revisar las correcciones de seguimiento antes de incorporarlas a develop. No hacer merge automático ni cerrar el Issue #3 sin aceptación.
