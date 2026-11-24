# HU-07 — Evidencia técnica de Catálogo y T59

Esta evidencia corresponde al incremento inicial aeb8485. Fue complementada por el acuerdo y la implementación de [CA3](evidencia-hu-07-ca3.md); los pendientes de trazabilidad de esta versión histórica se consultan en esa evidencia nueva.

Issue [#68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68). Rama local `feature/hu-07-versiones-malla`, base remota `843bf4f`. Criterios y acuerdo técnico confirmados por Leslie en este chat. Implementación para revisión. Leslie autorizó crear commits atómicos en español y publicar esta rama; no autorizó merge ni aceptación funcional.

## Implementación comprobada

| Criterio | Resultado real | Pruebas |
| --- | --- | --- |
| CA1: versiones y vigencia | Crear/listar/consultar/activar; nombre normalizado único por carrera; activación atómica conserva versiones anteriores | `MallaTests`, `MallasApiTests`, SQL: activaciones concurrentes y fallo después de desactivar |
| CA2: pertenencia y migración | UUID existentes preservados; versión Inicial migrada por carrera; FK compuesta impide combinar carrera y malla ajena; código único por versión; listado sin filtro conserva histórico | `MallasApiTests`, cuatro pruebas SQL y regresión HU-08 |
| CA3: versión en análisis | **Parcial**: contexto privado de Catálogo expone `malla` y los DTO de asignaturas incluyen `mallaId`/`malla`; todavía no se persiste la instantánea en los análisis/informes de Homologación | `ContextoAnalisisInternoTests`; [propuesta de integración](propuesta-integracion-hu-07-analisis-informe.md) pendiente de revisión conjunta |
| CA4: autorización | Administrador en API y Gateway; 401 sin token y 403 Docente/Coordinador en gestión de mallas; clave interna conservada | `MallasApiTests`, `GatewayHttpTests.MallasHu07_solo_administrador_y_clave_interna` |
| T59: interfaz | Panel dentro de AsignaturasPage existente; crear, activar, listar y filtrar; registro en versión seleccionada o vigente; errores y recarga; vigencia de carrera separada de malla | `MallasPanel.test.tsx`, regresión de AsignaturasPage, suite frontend, tipos y Vite |

No hay traslado, clonación ni borrado de versiones/asignaturas. La creación de una carrera nueva incluye una versión técnica Inicial en el mismo SaveChanges. HU-08 confirma en la vigente si el cliente no envía mallaId, admite mallaId explícito en el programa confirmado y no sobrescribe códigos de esa versión. El aviso previo de duplicados también se limita a la vigente.

## TDD y fallos observados

1. Las 9 pruebas de Domain se prepararon antes de la entidad en el incremento anterior; el primer fallo fue CS0246. Se conserva su evidencia en la spec.
2. Las 6 pruebas iniciales de MallasApiTests fallaron contra la aplicación previa por ausencia de rutas/relación de malla. Después se implementaron Application, repositorio y API.
3. Las 3 pruebas de UI fallaron al no existir MallasPanel; después se implementó el componente.
4. Las 4 pruebas de Gateway fallaron porque AcademicAccess permitía pasar a Docente/Coordinador. Las nuevas rutas ahora reutilizan UserAdministration; Catálogo vuelve a exigir Administrador.
5. La prueba `Hu07_importacion_y_aviso_de_duplicados_se_resuelven_en_la_malla_vigente` reprodujo el aviso incorrecto cuando el código estaba en otra versión. Se corrigió la búsqueda del aviso; la prueba comprueba además dos asignaturas independientes y rechazo del duplicado en la vigente.

No se eliminaron aserciones históricas. Los fixtures que insertan asignaturas directamente ahora preparan su relación obligatoria con Malla. La prueba del script histórico HU-05 sigue comprobando su segmento original hasta la migración HU-08: HU-07 necesita un UPDATE de asociación, que tiene sus propias pruebas de conservación. Las suites finales mantienen los casos anteriores.

## SQL Server real aislado

Comando reproducible: `python tests/security/hu07-sql.py`. El arnés usa un contenedor SQL Server 2022 efímero propio, puerto aleatorio de loopback, contraseña generada en memoria y bases `Hu07_<UUID>`. No lee `.env` ni usa volúmenes o bases persistentes del equipo. Se comprobó que no quedaron contenedores con su etiqueta.

Las cuatro pruebas reales verifican:

1. Actualización desde `20261104083910_ProgramasAnaliticosHu08` con carrera histórica de otra facultad, asignatura, unidad, temática, contenido, perfil, revisión y PDF sintéticos. Compara las columnas originales y los datos de todas las tablas relacionadas antes/después; conserva bytes y huella del PDF. Reversión a develop segura mientras solo existen versiones técnicas de migración; nueva actualización correcta; reversión con una versión posterior rechazada sin modificar historial ni datos.
2. Base vacía y dos contextos SQL activando versiones simultáneamente: se conservan tres versiones y solo una vigente. SQL rechaza una asignatura con carrera y malla incompatibles sin cambiar su pertenencia.
3. Fallo provocado antes del commit del script de migración: rollback deja historial original y elimina la tabla/columna nuevas; después se puede aplicar normalmente.
4. Trigger de prueba provoca un fallo después de desactivar la vigente y antes de guardar la nueva: la transacción conserva la versión original vigente y no deja la versión fallida.

La migración nueva es `20261122044758_VersionesMallaHu07`. Se conserva cada migración histórica; cambian el snapshot y los archivos nuevos de HU-07. La reversión tiene guardia explícita contra pérdida de versiones. **Antes de migrar una base persistente sigue siendo obligatorio hacer respaldo y ensayar su restauración**; no se realizó esa operación sobre datos del equipo.

## Comandos y resultados finales

| Ejecución | Resultado |
| --- | --- |
| `python tests/security/hu07-sql.py` → suite Catalogo.Tests completa con SQL configurado | **147 correctas, 0 fallidas, 0 omitidas** |
| `dotnet test tests/Gateway.Tests/Gateway.Tests.csproj --no-restore --nologo` | **149 correctas, 0 fallidas, 0 omitidas** |
| `node node_modules/vitest/vitest.mjs run --maxWorkers=1` en frontend | **178 correctas, 30 archivos, 0 fallidas**; 185 s |
| `node node_modules/typescript/bin/tsc --noEmit` | Correcto, salida 0 |
| `node node_modules/vite/bin/vite.js build` | Correcto, salida 0 |
| `dotnet build src/catalogo-service/Catalogo.sln --no-restore --nologo` | Correcto: 0 errores y 0 advertencias |
| `dotnet-ef migrations has-pending-model-changes ... --no-build` | Sin diferencias entre modelo y última migración |
| `dotnet format whitespace ... --no-restore --verify-no-changes --include <archivos modificados>` | Catálogo correcto; Gateway corregido a LF y verificado |

Total de estas suites: **474 pruebas correctas**. No se suman las ejecuciones filtradas ni las repeticiones. Compilación de Domain/Application/Infrastructure/Api y Gateway comprobada al ejecutar sus suites; no hay dependencias nuevas. La restauración necesaria se realizó en el primer incremento de esta rama.

La primera ejecución global de frontend dentro del sandbox se interrumpió sin resultados; la ejecución final fuera del sandbox completó las 178 pruebas con un worker. No se modificaron timeouts ni pruebas para obtener el resultado. La primera verificación de formato de Gateway detectó CRLF frente a LF; solo se normalizaron los finales de línea y se repitió esa comprobación.

Registros locales ignorados por Git: `TestResults/hu07-sql/catalogo-sql.trx`, `TestResults/hu07-sql/pruebas.log`, `TestResults/hu07-gateway/gateway-hu07.trx`, `TestResults/hu07-frontend-final.log`, `TestResults/hu07-build-frontend.log` y registros de TDD `hu07-rojo-*`.

## Límites y aceptación pendiente

- API y Gateway arrancaron en sus hosts HTTP de pruebas. SQL Server fue real y aislado; frontend se comprobó con jsdom y compilación. No se ejecutó en esta entrega un navegador real ni el stack completo con Docker Compose.
- CA3 no está cerrado: Pablo debe revisar captura de instantáneas de malla por análisis; Edwin debe revisar su lectura en informe y HU-22. No se cambiaron sus módulos ni el motor determinista.
- Pendientes revisión cruzada por Edwin, ejecución integrada final y aceptación de Shirley. No se declara terminada HU-07 ni se integra la rama.

Para la aceptación, Shirley deberá registrar las evidencias de: crear dos versiones de una carrera, activar cada una sin mover asignaturas, repetir un código en versiones diferentes y rechazarlo en la misma, consultar histórico, rechazar roles no administrativos y comprobar trazabilidad después de integrar CA3. Debe verificar que cambiar la vigente no modifica un análisis/informe ya guardado y que un histórico sin versión muestra «versión no registrada». La aceptación humana permanece pendiente.

## Comprobación previa a publicar

El 22/11/2026, antes de crear los commits, se verificaron los TRX y la salida de frontend: mantienen 147 + 149 + 178 pruebas correctas. No hubo cambios de código desde esas ejecuciones. `origin/develop` avanzó a `715b5a1` (44 commits posteriores a la base `843bf4f`); este incremento conserva su base comprobada y se publica para revisión. Sus resultados no acreditan la integración con el develop nuevo. Hay archivos coincidentes en HU-08 y Gateway que deberán conciliarse y volver a probarse durante esa actualización, antes del merge. No se realizó merge ni rebase en esta publicación.
