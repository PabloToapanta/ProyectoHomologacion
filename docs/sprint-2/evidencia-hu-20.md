# HU-20 — persistencia del resultado del análisis / Issue #44

Responsables: Edwin Caraguay y Shirley Amaguaña. Rama `feature/hu-20-persistencia-analisis`
desde `develop` `cbe9ac1`. Fecha: 04/11/2026.

## Origen de la implementación

La persistencia se implementó dentro de PR #55 (HU-18, Leslie), según el
[contrato vigente de HU-18](contrato-integracion-hu-18.md): Homologación es dueño
del resultado, base lógica `HomologacionAnalisis`, migración
`20261104000100_AnalisisInternosHu20`, tabla `AnalisisInternos` con clave
`solicitudId + asignaturaAprobadaId + asignaturaReceptoraId`. El último intento por
pareja reemplaza al anterior; si SQL falla, la API responde 503 sin confirmar guardado;
la lectura vuelve a comprobar la asignación docente en Solicitudes. La vista HU-30
recupera los guardados al abrir el detalle.

Este incremento **no modifica código de producto**: contrasta #44 con lo integrado,
añade las pruebas de SQL Server real que faltaban y su ejecución en CI.

## Cambios de este incremento

- `tests/Homologacion.Infrastructure.Tests/ResultadosAnalisisSqlServerTests.cs`:
  tres pruebas que solo se ejecutan con `HU20_TEST_SQL` (base temporal `Hu20Tests_*`
  creada con la migración real y eliminada al terminar).
  - Guardar `Completo`, `Incompleto` y `Fallido` en dos solicitudes y leerlos con
    contexto y conexión nuevos: campos, fecha, porcentajes, sugerencia y evidencias
    conservados; resultado nulo en los no completos; cada solicitud solo ve lo suyo.
  - Reanálisis de la misma pareja desde otro contexto: una sola fila, último estado.
  - Primeras inserciones concurrentes de la misma pareja (4 en paralelo, 5 rondas):
    una fila; si alguna pierde la carrera, recibe `AlmacenAnalisisNoDisponibleException`
    (503 en la API), nunca una excepción SQL.
- `.github/workflows/hu20-persistencia.yml`: ejecuta esas pruebas con un SQL Server
  efímero de CI (credencial sintética del contenedor). No se modificó el workflow de HU-18.

Las pruebas pasaron en la primera ejecución: verifican código existente, no hubo fase
roja. La prueba concurrente no garantiza que la carrera ocurra en cada ronda; si no
ocurre, comprueba igualmente que no se duplican filas.

## Criterios de #44

| Criterio | Evidencia | Estado |
|---|---|---|
| CA1 — guardar con porcentajes, estado, sugerencia, justificación y evidencias | SQL Server real (nueva prueba) y smoke: `justificacion persistida` | Verificado |
| CA2 — lectura tras reiniciar el servicio | Smoke: `Fallido conservado al recrear servicio`, `Completo conservado tras reinicio` | Verificado |
| CA3 — incompleto o fallido conservan estado sin homologable | Pruebas de Application, SQLite y SQL Server; smoke `resultado nulo conservado` | Verificado |
| CA4 — fallo de persistencia controlado | Trigger SQL en el smoke: 503 y resultado anterior intacto; pruebas de API y repositorio | Verificado |
| CA5 — base propia por migración y acceso por asignación | Migración real; smoke `lectura guardada ajena denegada` (404); prueba de pérdida de asignación | Verificado |

Política de reintentos (dependencia de #44): último guardado por pareja, sin historial,
decidida por Leslie en el contrato de HU-18. Debe confirmarla Shirley.

## Ejecuciones del 04/11/2026

Entorno: Windows con Docker; SDK .NET 8 en contenedor; SQL Server 2022 temporal.

1. `Homologacion.Infrastructure.Tests` con `HU20_TEST_SQL`: 39 aprobadas, 1 omitida
   (`OllamaRealTests`, requiere modelo real). Base y red temporales eliminadas.
2. `python tests/security/hu18-smoke.py` sobre una copia limpia de `develop` `cbe9ac1`:
   49 comprobaciones aprobadas, 0 fallidas. Proyecto Docker aleatorio, volúmenes eliminados.
   Transporte HTTP local aislado y doble HTTP de Ollama: no acredita HTTPS ni inferencia real.

**No verificado:** CI en GitHub del nuevo workflow, revisión cruzada (Leslie revisa a Edwin)
y aceptación de Shirley. El análisis externo (HU-19) no tiene persistencia todavía; ver
[propuesta de contrato HU-19](propuesta-contrato-hu-19.md).

## Revisión para PR del 04/11/2026

Se volvió a ejecutar `Homologacion.Infrastructure.Tests` con SDK .NET 8 y SQL
Server 2022 en contenedores aislados: **39 aprobadas, 1 omitida, 0 fallidas**.
La omitida requiere Ollama real. Se añadieron aserciones explícitas para créditos
y justificación persistidos. Los contenedores y la red temporales se eliminaron.
`git diff --check` sin errores. No se repitió el smoke integrado en esta revisión;
su ejecución anterior queda registrada arriba. HU-19 sigue como propuesta sin código.
