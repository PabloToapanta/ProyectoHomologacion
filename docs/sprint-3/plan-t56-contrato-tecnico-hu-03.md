# T56 — Plan y contrato técnico de HU-03

**Issue:** #67 · **Historia:** HU-03 · **Responsable:** Pablo Toapanta · **Estimación:** 1 PH  
**Estado:** contrato revisado y aprobado técnicamente por Edwin; listo como base para T57/T58.

## Objetivo de T56

Definir representación, validación, versionado y consulta de parámetros antes de integrar el motor o construir la pantalla. T56 no cambia aún las reglas de HU-14/15/16 ni implementa endpoints o UI.

## Plan ejecutado

1. Inventariar valores actuales del motor y mapearlos a la spec acordada.
2. Definir modelo inmutable de configuración vigente con identificador de versión.
3. Definir auditoría, validaciones y autorización.
4. Definir cómo se fija la versión de parámetros para cada evaluación y cómo se resuelven actualizaciones concurrentes.
5. Definir contratos HTTP para consultar configuración, actualizarla y consultar historial.
6. Revisar persistencia de resultados/informes existentes y registrar su compatibilidad.

## Contrato de parámetros

Configuración versionada, propiedad del servicio Homologación. Cada versión es inmutable:

| Campo lógico | Tipo | Default | Validación |
| --- | --- | ---: | --- |
| `versionId` | UUID | generado por servidor | único e inmutable |
| `umbralPct` | entero | 80 | 1–100 |
| `antiguedadMaximaAnios` | entero | 10 | 1–50 |
| `periodosMinimosInterno` | entero | 1 | 1–50 |
| `asignaturasHomologablesMinimasInterno` | entero | 1 | 1–50 |
| `periodosMinimosExterno` | entero | 2 | 1–50 |
| `asignaturasHomologablesMinimasExterno` | entero | 2 | 1–50 |

La regla HU-16 de cero cambios previos para cambio externo no es editable. Cada actualización envía el conjunto completo de parámetros editables para evitar cambios parciales.

## Auditoría

Cada versión creada incluye `actorId` (claim `sub` del JWT validado), `fechaHoraUtc` (servidor), `motivo` obligatorio de 1–500 caracteres y `respaldo` obligatorio de texto libre de 1–2.000 caracteres. El cliente no envía actor, fecha, valores anteriores ni `versionId` como autoridad. Versión, auditoría y puntero vigente se escriben en una sola transacción.

## Persistencia y concurrencia

Modelo recomendado en la base propia de Homologación:

- `ConfiguracionesHomologacion`: fila por versión inmutable con parámetros y metadatos de auditoría.
- `ConfiguracionHomologacionVigente`: una fila que apunta a la versión activa.
- Restricción única para garantizar un solo puntero vigente.
- Actualización transaccional: inserta versión nueva y mueve el puntero.

El endpoint de lectura devuelve ETag de la versión vigente. El `PUT` exige `If-Match` con ese ETag. Si la configuración cambió después de la lectura, devuelve 409 ProblemDetails y no guarda; el Coordinador vuelve a consultar antes de intentar otra actualización. **Edwin aprobó este mecanismo.**

Al iniciar una evaluación, la aplicación lee una vez la versión apuntada y conserva el objeto inmutable durante el caso de uso. Si un Coordinador cambia parámetros mientras el análisis corre, este completa con la fotografía inicial; análisis que comiencen después de la confirmación usan la nueva versión. El resultado almacenado incluye `configuracionVersionId` y valores usados. No se mantiene una transacción SQL abierta durante todo el cálculo.

## API propuesta

Todas las rutas pasan por Gateway y exigen JWT. Rol `Coordinador` para consultar, actualizar e historial.

| Método | Ruta | Uso |
| --- | --- | --- |
| `GET` | `/api/homologaciones/configuracion` | Devuelve configuración vigente y ETag. |
| `PUT` | `/api/homologaciones/configuracion` | Exige `If-Match`; crea versión completa con motivo y respaldo y devuelve nuevo ETag. |
| `GET` | `/api/homologaciones/configuracion/historial` | Lista versiones en orden descendente, solo lectura. |

El historial se pagina con `cursor` y `limit` (default 50, máximo 100). Validación integral: rangos acordados, motivo/respaldo no vacíos tras trim y dentro de longitud; campos desconocidos rechazados. Errores: 400 entrada inválida, 401 sin autenticación, 403 rol incorrecto, 409 ETag obsoleto, 503 almacenamiento no disponible.

## Historial en UI

La pantalla muestra configuración vigente y formulario de edición. Incluye historial de solo lectura para Coordinadores con fecha/hora, actor, cambios anterior/nuevo, motivo y respaldo. Las entradas históricas no se editan ni borran.

## Compatibilidad revisada con persistencia existente

1. El umbral actual vive en `Equivalencia:UmbralPct`/`IUmbralEquivalencia`; HU-15 fija diez años en el validador; HU-16 codifica mínimos 1/1 y 2/2. T57 reemplazará estos valores por configuración manteniendo contratos y regresiones.
2. **Edwin confirma que basta con que un cambio de parámetros no modifique los resultados ya guardados.** La tabla HU-20 mantiene su comportamiento vigente de último análisis por pareja; HU-03 no exige retener cada ejecución que una ejecución posterior reemplace. El guardado de una nueva versión de parámetros no toca análisis ni informes existentes. Cada resultado almacenado conserva su propia fotografía mientras permanezca guardado.
3. HU-23 genera un informe por solicitud. Un cambio de configuración no modifica ni vuelve a generar el informe existente.
4. Registros anteriores a HU-03 sin `configuracionVersionId` son legado; la migración no inventa qué configuración histórica usaron. Se identifican como legado/desconocido cuando el esquema requiera ese dato.

## Criterio de cierre de T56

- Campos, defaults, rangos, rol, auditoría e interfaces coinciden con la spec acordada.
- Edwin revisó que los parámetros de HU-15/HU-16 mantienen las reglas actuales y confirmó que actualizar la configuración no debe alterar resultados ya guardados.
- Edwin aprobó ETag/`If-Match` para actualizaciones concurrentes.
- El comportamiento de HU-20 de último resultado por pareja se conserva; no se exige historial de ejecuciones reemplazadas.
- Este contrato queda aprobado como base de T57/T58; eso no declara HU-03 implementada ni aceptada.
