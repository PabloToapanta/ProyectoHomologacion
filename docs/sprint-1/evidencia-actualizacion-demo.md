# Actualización de la demostración local — actividad 1 de integración

Fecha: 20/10/2026, 11:27 (America/Guayaquil). Responsable: Pablo.
Vinculada a HU-01 / Issue #1 y comprobaciones de integración de HU-04 / #2,
HU-05 / #3 y HU-14 / #5. No constituye aceptación de la Product Owner.

## Versión y alcance

Se consultó `origin/develop` mediante fetch y se creó la rama local
`feature/hu-01-actualizar-demo` desde
`ad9ee26577ab2be2d44855461dd66a8298359250`.
La rama anterior `feature/hu-14-equivalencia` se conserva. Durante la actualización
del entorno no se realizaron commits, pushes ni merges ni se modificó código
de otros integrantes. La publicación posterior de esta evidencia fue autorizada
por Pablo y no incluye la configuración privada de la demostración.

Se reconstruyeron Auth, Catálogo, Homologación, Gateway y Frontend del proyecto
Docker Compose local `fisei-uta-demo`. Las imágenes de servicios sin cambios
reutilizaron la caché; Gateway y Frontend se recrearon. Se mantuvo el contenedor
SQL Server en ejecución y no se eliminaron volúmenes.

El arranque sigue usando los archivos privados ignorados `.env.demo`,
`.env.demo.compose.yaml` y `.env.demo.control.sh`, creados para esta demostración.
Una instancia SQL aloja bases separadas de Auth y Catálogo. Las credenciales
permanecen fuera de Git. El Compose integrado versionado corresponde a la
siguiente actividad y todavía no se entrega en esta evidencia.

## Resultados ejecutados

| Comprobación | Resultado real |
|---|---|
| Reconstrucción de cinco servicios con Docker Compose | Correcta; incluye publicación .NET y build React con chequeo de tipos |
| Login válido mediante Gateway y Auth con SQL Server | HTTP 200 y JWT |
| Contraseña incorrecta | HTTP 401 |
| Catálogo sin token o con token inválido | HTTP 401 |
| Listar carreras, asignaturas por carrera y consultar asignatura | HTTP 200 |
| Consultar carrera individual mediante Gateway | HTTP 200; antes de actualizar respondía 405 |
| Frontend publicado | HTTP 200 |
| Fixture sintético de HU-14 por Gateway | HTTP 200, tres criterios al 80 %, estado Homologable |
| HU-14 sin JWT / JWT inválido / cantidades inválidas | HTTP 401 / 401 / 400 |
| HU-14 con múltiples orígenes | Cobertura académica sin duplicación; horas/créditos desconocidos; InformacionIncompleta |
| Inventario antes/después | Misma carrera y asignatura, con idénticos datos JSON |
| Montajes del SQL antes/después | Mismo volumen y destino |
| Playwright de autenticación, escritorio y móvil | 2 pruebas aprobadas, 0 fallidas, 2,5 segundos |

La comparación de inventario fue de solo lectura; las pruebas no añadieron ni
eliminaron carreras o asignaturas. Se comprobó login correcto después de la
actualización, además del inventario académico y el montaje SQL. No se realizó
un respaldo ni una auditoría completa de las bases de datos.

Pruebas de navegador: `frontend/e2e/auth.spec.ts`, ejecutadas contra el frontend
real, sin mocks. Verifican protección de rutas, error de credenciales, login,
sesión solo en memoria, recarga, logout y ausencia de desbordamiento horizontal
en login/panel móvil. Se reutilizó `tests/homologacion-service/smoke.py` para HU-14.
No se repitió aquí toda la suite unitaria ni el E2E de catálogo que crea registros.

Los logs y capturas de esta sesión se guardaron temporalmente fuera de Git:

- `/tmp/fisei-demo-actualizacion-build.log`
- `/tmp/fisei-demo-actualizacion-e2e.log`
- `/tmp/fisei-demo-actualizacion-e2e/`

## Uso y pendientes

Demostración disponible en <http://127.0.0.1:5173>. Se conservan las credenciales
locales existentes, consultables en el archivo privado `.env.demo.acceso.txt`.
Los servicios están publicados solo en la interfaz loopback.

Siguiente actividad: preparar un arranque integrado reproducible y versionado,
incluyendo Homologación, sin requerir el PR pendiente de Edwin ni modificar sus
módulos. HU-06, HU-15 y HU-16 siguen pendientes de integración; esta actualización
no declara terminado el Sprint 1 ni sustituye la aceptación de Shirley.
