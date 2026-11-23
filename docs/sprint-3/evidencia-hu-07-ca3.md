# HU-07 — CA3: instantáneas de malla en análisis e informe

Issue [#68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68) · Draft PR [#96](https://github.com/PabloToapanta/ProyectoHomologacion/pull/96). Incremento del 22/11/2026 posterior al acuerdo de contrato transmitido por Leslie desde Pablo. La aceptación funcional corresponde a Shirley.

## Base y coordinación

Leslie autorizó incorporar `origin/develop` únicamente en la rama `feature/hu-07-versiones-malla`. Se integró `1542986` mediante el commit `388a0db`, sin conflictos de texto. Se conservaron HU-17, HU-25, HU-32/33, HU-37 y HU-27, incluido el límite actualizado de HU-08. No se integró la PR en develop.

El contrato aprobado mantiene la vigencia de carrera separada de la de malla. Homologación guarda las versiones conocidas de origen y receptora al analizar; informe y HU-22 utilizan esas copias. No se infiere una malla externa ni se atribuyen versiones a datos históricos sin esa información.

## Implementación

- `AsignaturaParaAnalisis.Malla` recibe el contrato de Catálogo mediante un DTO propio de Application; no hay referencia a entidades EF de otro servicio.
- `MallasAnalisis` contiene `origenes` y `receptoras`. Cada entrada distingue `asignaturaAprobadaId` de `asignaturaCatalogoId`; `malla` copia id, carrera, nombre y `vigenteAlAnalizar`. Se validan identificadores, pertenencia y vigencia explícita antes de usar la metadata. Una malla ausente es null; una malla presente pero mal formada se rechaza como fuente no verificable.
- Los análisis internos, externos y las salidas por RN-07 incluyen la instantánea. En externo, la entrada de origen conserva malla e id de Catálogo null cuando no existe información autorizada.
- PrepararAgrupacion captura una entrada por origen/receptora, y GestionarAgrupaciones la guarda en el último análisis del grupo dentro del cierre existente. Los porcentajes y el veredicto siguen saliendo del motor existente.
- La persistencia utiliza los JSON existentes de análisis, agrupaciones e informes. **No necesita una migración nueva de Homologación ni backfill histórico.** Los registros anteriores sin `mallas` siguen deserializando como null.
- GenerarInformeHomologacion copia los metadatos desde los análisis guardados de parejas y grupos. HU-22 emite la alerta informativa de malla de origen no vigente únicamente cuando la instantánea lo acredita. Un null no se interpreta como no vigente. No cambia decisiones ni bloquea el trámite.
- Las respuestas HTTP incluyen la metadata. La UI de análisis, grupos e informe muestra nombre, identificador y vigencia al analizar. Los históricos indican «Versión no registrada». La misma vista forma parte del contenido de impresión del informe; no consulta de nuevo Catálogo.

Se conserva el funcionamiento de la reutilización HU-37: los metadatos de malla no cambian la fórmula ni se usan para inventar evidencia. La evaluación completa antes/después se compara con los mismos parámetros en las pruebas.

## Pruebas y fallos reales

Se prepararon pruebas fallidas antes de implementar la captura y antes de conectar el informe: inicialmente faltaban tipos/propiedades de malla en Application. Las pruebas de la vista también fallaron primero por ausencia del componente.

Se corrigieron dos problemas de los fixtures nuevos sin debilitar aserciones: comparación de colecciones por referencia y parámetros con UUID nuevo entre dos ejecuciones; la comparación final serializa toda la evaluación con parámetros fijos. En SQL, se extrajo una serialización fuera de una expresión EF y se corrigió el fixture histórico para representar el formato almacenado real (ResultadoInternoSolicitud directamente, no el contenedor AnalisisGuardado).

La primera ejecución de SQL no arrancó porque Docker Desktop estaba apagado; no se contabilizó como una prueba de negocio. Se inició Docker y se repitió sobre un contenedor propio. El arnés ahora sanea los errores de Docker para no imprimir sus argumentos con credenciales efímeras.

### Resultados sobre develop incorporado más CA3

Comando backend: `python tests/security/hu07-sql.py --homologacion`. Usa SQL Server 2022 efímero propio, puertos aleatorios de loopback y bases temporales; no lee `.env` ni toca bases persistentes. Las suites se ejecutan secuencialmente para evitar competir por los mismos archivos de compilación.

| Suite | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Catalogo.Tests | 148 | 0 | 0 |
| Homologacion.Infrastructure.Tests | 103 | 0 | 1 |
| Homologacion.Application.Tests | 237 | 0 | 0 |
| Homologacion.Domain.Tests | 115 | 0 | 0 |
| Homologacion.Api.Tests | 160 | 0 | 0 |
| Auth.Application.Tests | 13 | 0 | 0 |
| AuthService.Tests | 79 | 0 | 0 |
| Gateway.Tests | 156 | 0 | 0 |
| Solicitudes.Tests | 169 | 0 | 5 |
| Frontend, 33 archivos, un worker | 216 | 0 | 0 |
| **Total sin sumar repeticiones** | **1396** | **0** | **6** |

Omisiones explícitas: un caso optativo de Ollama real de HU-18 y cinco casos SQL de Solicitudes que requieren HU10_TEST_SQL. No se contabilizan como comprobados. No se cambiaron timeouts ni se retiraron aserciones para obtener resultados correctos. La suite frontend terminó en 219,57 s; jsdom emitió avisos de scrollTo no implementado, sin fallos.

La nueva prueba SQL `HU07_SQL_conserva_mallas_de_parejas_grupos_informe_y_lee_historicos` reabre contextos para leer desde SQL las instantáneas de pareja, grupo, informe y origen externo sin malla. Un registro JSON antiguo sin `mallas` conserva su lectura. Las pruebas HTTP comprueban la proyección de la metadata y que cambiar la fuente después de analizar no modifica el informe ni habilita revisiones después del cierre.

TypeScript, compilación de producción Vite y verificación de formato de los archivos C# modificados: correctos. Los proyectos backend compilaron al ejecutar las suites. Los avisos XML de AnalisisInternoRequest y de estilo xUnit en las pruebas SQL de HU-21 provienen de archivos ya presentes en develop y no se ocultaron.

Registros locales: `TestResults/hu07-ca3-sql/` (logs de cada suite), `TestResults/hu07-ca3-frontend.log`, `TestResults/hu07-ca3-types.log`, `TestResults/hu07-ca3-build-frontend.log`, `TestResults/hu07-ca3-resumen.json` y logs TDD `hu07-ca3-rojo-*`. En esta ejecución el logger de la solución raíz sobrescribió su TRX entre proyectos; los resultados de los seis proyectos se conservaron en `solucion.log`. Se corrigió el arnés para que futuras ejecuciones de la solución usen nombres automáticos de TRX.

## Integración de navegador y aceptación

El E2E existente de HU-27 usa Compose propio, credenciales sintéticas, Chromium y agente simulado. El primer intento agotó el tiempo de construcción antes de ejecutar pruebas. El segundo levantó y verificó la semilla, pero NVM4306 rechazó npm. Se preparó un adaptador local ignorado por Git para ejecutar exactamente playwright test mediante su CLI Node, manteniendo el caso y los timeouts del arnés de Pablo. El primer intento con ese adaptador detectó Chromium ausente y un problema de lectura cp1252 del diagnóstico. Se instaló el Chromium fijado por Playwright y se repitió con Python en modo UTF-8. La ejecución final terminó con código 0 y PASS: T66 Playwright completo. Los intentos anteriores no se cuentan como E2E correcto. No se cambió el arnés de Pablo ni la instalación global de NVM.

Pendientes humanos: revisión del incremento por Pablo y Edwin, aceptación funcional de Shirley y autorización para integrar la PR. Antes de actualizar una base persistente del Catálogo, conservar el requisito de respaldo y ensayo de restauración. No se declara aceptada HU-07 por estas pruebas.

### Resultado integrado final

**E2E HU-27 correcto en Chromium** con el código CA3 y todos los servicios reales de Compose: login → solicitud → aprobada → dos docentes → análisis → revisiones → informe → remisión → finalización → nueva consulta del estado persistido. El agente fue simulado, conforme al alcance; no se usó Ollama real.

Ejecución de Windows: `python -X utf8 TestResults/hu07-ca3-run-e2e.py`, adaptador local de CLI explicado arriba. En un entorno con npm disponible, el comando del repositorio sigue siendo `python tests/security/hu27-e2e.py`. Log final: `TestResults/hu07-ca3-e2e-verificado.log`. Se verificó que no quedaron contenedores ni volúmenes de los proyectos aislados HU-07/HU-27.

El recorrido de navegador es una regresión del flujo completo; la conservación de versiones al cambiar la fuente y los casos de grupos/históricos se comprueban específicamente en las pruebas Application, HTTP y SQL anteriores. No se atribuyen al E2E escenarios que no ejecuta.

Resultado global: **1396 pruebas de suites correctas más 1 E2E correcto; 6 omisiones explícitas**. CA3 queda implementado y comprobado automáticamente; su aceptación funcional continúa pendiente de Shirley y de la revisión cruzada del equipo.
