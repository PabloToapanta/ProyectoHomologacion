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
- Estados Completo, Incompleto, Fallido y exclusión RN-07 conservan la trazabilidad disponible.
- Grupos conservan una entrada por asignatura y distinguen los dos espacios de UUID.
- Cambiar únicamente nombre/vigencia de malla no cambia el motor, el JSON del contexto ni la clave HU-37; una nueva ejecución captura metadatos nuevos y reutiliza la explicación.
- Tras reabrir almacenamiento se recupera la fotografía guardada; históricos sin campo deserializan como null. API y OpenAPI exponen el contrato.
- Las comprobaciones de autorización, reservas y cierre del informe siguen pasando.

## Evidencia de ejecución

Pendiente de ejecutar pasos 2–6. No se atribuyen pruebas ni aceptación antes de ejecutarlas.
