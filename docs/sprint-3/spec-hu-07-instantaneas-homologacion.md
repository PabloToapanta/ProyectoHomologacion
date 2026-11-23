# HU-07 — Instantáneas de malla en Homologación (Pablo)

Refs #68, CA3. Complementa la PR #96 de Leslie. Rama `feature/hu-07-instantaneas-homologacion`, desde `origin/develop` en `1542986`. Autorización de Pablo: preparar y ejecutar este plan y publicar una PR Draft. El contrato se basa en la propuesta de Leslie y en la condición de Edwin transmitida por Pablo: malla fuera del contexto del agente y de las huellas de HU-37.

## Resultado esperado y límites

Cada análisis nuevo conserva la malla consultada de cada asignatura: id, carreraId, nombre y vigenteAlAnalizar. Es una fotografía de Catálogo al analizar, no una afirmación sobre el plan cursado históricamente por el estudiante. Activar otra malla no reescribe análisis guardados. La vigencia de carrera sigue siendo independiente.

Se cubren parejas internas, parejas externas y agrupaciones. Para origen externo sin programa autorizado no se inventa malla ni UUID de Catálogo. Los JSON antiguos siguen leyéndose con `mallas = null`, que el consumidor debe mostrar como «versión no registrada». No hay backfill ni migración SQL: los repositorios existentes ya guardan JSON.

No se cambia el motor, los umbrales, la elegibilidad, el contexto del agente, la plantilla ni las claves de HU-37. La misma asignatura con metadatos de malla distintos puede reutilizar su explicación si los datos académicos no cambian; otra asignatura con UUID distinto conserva su clave distinta.

Edwin mantiene el consumo y presentación en informe, PDF y HU-22; Leslie mantiene Catálogo, selector, script HU-05 y reversión. Esta PR no cierra por sí sola CA3, ni acredita aceptación de la PO o integración completa con la PR #96.

## Contrato aditivo

- `AsignaturaParaAnalisis.Malla`: propiedad init nullable, DTO propio de Homologación compatible con `{id,carreraId,nombre,vigente}` de Catálogo. Constructores históricos intactos.
- `ResultadoInternoSolicitud.Mallas` y `AnalisisAgrupacionGuardado.Mallas`: bloque nullable con listas `origenes` y `receptoras`. Cada entrada incluye `asignaturaCatalogoId` nullable, `asignaturaAprobadaId` nullable y `malla` nullable. La instantánea usa `vigenteAlAnalizar`.
- Una malla presente con identidad vacía, nombre vacío o carrera ajena es una fuente no verificable (503 existente). Ausencia del objeto es compatible con Catálogo previo a #96.
- API devuelve el mismo bloque en ejecución y consulta. No se aceptan metadatos del cliente: se capturan desde las fuentes autorizadas ya consultadas, sin nuevas llamadas ni permisos.
- Agrupaciones capturan durante la preparación y guardan dentro del cierre transaccional existente; no confunden id de aprobada con id de Catálogo.

## Plan de implementación

1. Publicar esta spec en una PR Draft hacia develop, vinculada a #68.
2. TDD: añadir pruebas de captura, compatibilidad histórica, exclusión del agente y reutilización tras cambiar solo malla; ejecutar y registrar el fallo inicial.
3. Añadir DTO de entrada, instantáneas y captura validada; conectar resultados internos/externos (incluido RN-07) y agrupaciones sin modificar los cálculos.
4. Exponer los metadatos en respuestas y comprobar guardado/lectura reales con los repositorios JSON existentes, incluidos grupos e históricos sin el campo.
5. Ejecutar regresión de Application, Infrastructure, API y Domain de Homologación y formato de archivos afectados. Registrar resultados y limitaciones reales.
6. Revisar diff, publicar implementación y actualizar la PR Draft con pruebas, contrato y pendientes para Edwin/Leslie/PO. No realizar merge.

## Criterios verificables

- Pareja interna conserva ambas mallas; externa conserva receptora y origen desconocido.
- Estados Completo, Incompleto y Fallido, incluida la exclusión RN-07 (estado Completo, no homologable), conservan la trazabilidad disponible.
- Grupos conservan una entrada por asignatura y distinguen los dos espacios de UUID.
- Cambiar únicamente nombre/vigencia de malla no cambia el motor, el JSON del contexto ni la clave HU-37; una nueva ejecución captura metadatos nuevos y reutiliza la explicación.
- Tras reabrir almacenamiento se recupera la fotografía guardada; históricos sin campo deserializan como null. API y OpenAPI exponen el contrato.
- Las comprobaciones de autorización, reservas y cierre del informe siguen pasando.

## Evidencia de ejecución

Implementación preparada en la [PR Draft #97](https://github.com/PabloToapanta/ProyectoHomologacion/pull/97), desde `1542986` de develop. Plan ejecutado: spec, TDD, implementación, exposición API, persistencia y regresión comprobadas. La implementación se publica en la misma PR Draft; revisión humana e integración completa permanecen pendientes.

### TDD y ajustes de fixtures

La primera ejecución de `InstantaneasMallaTests` falló en compilación (`CS0117`/`CS1061`) porque aún no existían Malla/Mallas. Después de implementar, la ejecución detectó dos supuestos incorrectos en los fixtures nuevos: RN-07 conserva el estado Completo con decisión no homologable, y la reutilización requiere un proveedor de parámetros con versión estable (el fallback de pruebas genera otra versión). Se corrigieron esos fixtures sin cambiar reglas productivas. Las pruebas nuevas se integraron después en las suites completas.

### Verificación ejecutada

| Suite de Homologación | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Application | 236 | 0 | 0 |
| Infrastructure | 91 | 0 | 17 |
| API | 161 | 0 | 0 |
| Domain | 115 | 0 | 0 |
| Total | **603** | **0** | **17** |

Comando por suite: `dotnet test tests/Homologacion.<Suite>.Tests/Homologacion.<Suite>.Tests.csproj --nologo --logger "trx;LogFileName=<suite>.trx" --results-directory TestResults/hu07-instantaneas`. Logs/TRX locales ignorados en esa carpeta. No se suman las ejecuciones filtradas a los totales.

Las 18 pruebas nuevas cubren captura por estado, RN-07, interno/externo, N:1, rechazo de metadatos incoherentes, deserialización HTTP con y sin malla, persistencia de parejas internas/externas y grupos con SQLite al reabrir contextos, históricos reales sin campo y contrato de respuesta/OpenAPI. Además se amplió la prueba integrada HTTP de agrupaciones para comprobar la fotografía al cambiar la fuente. Las regresiones existentes incluyen autorización, reservas, cierre del informe y HU-37.

Se verificó que añadir malla a una fuente anterior, renombrarla o cambiar su vigencia no cambia el motor, el JSON del contexto realmente enviado ni la clave HU-37; nuevas ejecuciones capturan la nueva fotografía y reutilizan la explicación previa. Las pruebas existentes de cambios académicos siguen invalidando la reutilización.

Las 17 omitidas requieren servicios reales (16 SQL Server y 1 Ollama). La persistencia y el flujo HTTP integrado de este cambio se ejecutaron con SQLite y agente simulado. No se ejecutaron SQL Server real, Ollama real, navegador ni Docker Compose para esta PR; no hay cambios de esquema. La compilación de Infrastructure.Tests reportó dos advertencias xUnit2031 preexistentes en AgrupacionesSqlServerTests (líneas 102 y 124), fuera del diff.

Formato: `dotnet format whitespace` aplicado y verificado con `--verify-no-changes --no-restore --include <archivos afectados>` en los proyectos de pruebas Application, Infrastructure y API. Se corrigió espacio en blanco de los nuevos inicializadores; no hubo cambios semánticos después de las suites. `git diff --check` correcto.

### Entrega a Edwin y dependencias

Leer `AnalisisGuardado.Analisis.Mallas` para parejas y `AnalisisAgrupacionGuardado.Mallas` para grupos. Copiar esos valores al informe nuevo y a la fuente informativa de HU-22, sin consultas que sustituyan la fotografía por la vigencia actual. `mallas = null` o una entrada con `malla = null` significa «versión no registrada», nunca «no vigente». En origen externo también falta `asignaturaCatalogoId`. La vigencia se llama `vigenteAlAnalizar`, distinta de CarreraVigente.

Catálogo anterior a la PR #96 produce fotografías con malla null; se conserva compatibilidad, sin atribuir retrospectivamente versiones. Antes de cerrar CA3 faltan integrar y verificar con Catálogo #96, el consumo de Edwin en informe/HU-22/UI/PDF y la aceptación de Shirley. Los informes emitidos permanecen intactos. No se efectuó merge ni se declara HU-07 terminada.
