# HU-37 · Reutilizar el análisis del agente — propuesta de contrato

Issue [#77](https://github.com/PabloToapanta/ProyectoHomologacion/issues/77). Rama `feature/hu-37-reutilizar-analisis`, desde `develop`. Responsables: Edwin y Shirley. **Revisión previa de Pablo antes de codificar (T70)**, porque cambia la orquestación de HU-18 (`AsistirEvaluacionInterna`) y la persistencia de HU-20.

**Estado: propuesta.** No hay código. Las decisiones marcadas como **[Pablo]** esperan su respuesta; las demás vienen del issue y de las [decisiones del 20/11/2026](decisiones-validacion-ingeniero.md).

## 1. Qué se reutiliza y qué no

- **Se reutiliza solo la explicación del agente** (`ExplicacionAgente`: resumen y evidencias citadas). Es lo único que cuesta esperar a Ollama.
- **El motor determinista se ejecuta siempre.** Porcentajes, veredicto, antigüedad e información faltante se recalculan en cada análisis. El agente nunca decide.
- **Alcance:** solo el análisis interno (HU-18), que es el único que llama al agente. El externo (HU-19) no invoca al agente (D2), así que no cambia.
- Se reutiliza **entre solicitudes**: el mismo par de asignaturas con los mismos programas puede aparecer en solicitudes de estudiantes distintos. La explicación solo trata los programas, la evaluación y las correspondencias; no contiene datos del estudiante, de Quipux ni de la matrícula.

## 2. Clave de reutilización

SHA-256 (hex) de un JSON canónico con, en este orden:

| Campo | Origen | Nota |
| --- | --- | --- |
| `origenId`, `receptoraId` | Identificadores del catálogo | Asignaturas del catálogo, no la aprobada de la solicitud |
| `firmaOrigen`, `firmaReceptora` | Firma SHA-256 de cada programa | Ver la opción **[Pablo] A** |
| `firmaCorrespondencias` | SHA-256 de las correspondencias revisadas y vigentes | Contenidos y resultados, en el orden recibido |
| `versionParametros` | `ParametrosHomologacion.VersionId` (HU-03) | Cambiar el umbral u otro parámetro crea otra versión y, con ella, otra clave |
| `modelo` | `OllamaAnalisisOptions.Modelo` | |
| `versionPlantilla` | Nueva constante `OllamaAgenteInterno.VersionPlantilla` (por ejemplo `"hu18-v1"`) | Se incrementa al cambiar el mensaje de sistema, el esquema JSON o las opciones de generación |
| `contexto` | SHA-256 del `ContextoAgente` serializado que se enviaría al modelo | Ver la opción **[Pablo] B** |

- **Sin caducidad por tiempo.** Una clave distinta, por el cambio de cualquier campo, simplemente no encuentra nada guardado (CA2).
- **[Pablo] A — de dónde salen las firmas de los programas:**
  - **A1 (recomendada):** Homologación las calcula con los datos que ya recibe en `AsignaturaParaAnalisis`: perfil, horas, créditos, contenidos y resultados con sus textos. No se toca Catálogo.
  - **A2:** Catálogo expone en su contexto interno las firmas que ya calcula en `ComparacionCatalogoService.Programa`. Es la firma «oficial», pero cambia código de Catálogo (Leslie) y su contrato interno.
- **[Pablo] B — huella del contexto:**
  - La explicación describe una evaluación concreta. Las horas y los créditos de la aprobada salen del registro de la solicitud y pueden diferir del catálogo, y el motor añade información faltante según el caso. Con el mismo par y los mismos programas, la evaluación podría ser otra.
  - Incluir la huella del `ContextoAgente` garantiza que solo se reutiliza una explicación hecha para exactamente la misma entrada.
  - **Recomendación: incluirla.** Los demás campos quedan igualmente en la clave por trazabilidad, como pide la decisión del 20/11.

## 3. Cuándo se guarda y cuándo se reutiliza

1. **Se guarda** una explicación solo si el análisis queda «Completo»: `EstadoAsistencia.Completada` y `ValidarExplicacionAgente` superada.
   - «Incompleto» (el motor no llega a llamar al agente) y «Fallido» (revisión manual) no se guardan ni se reutilizan (CA3).
2. **Se reutiliza** si existe una explicación guardada con la misma clave y el docente no pide forzar.
   - Se toma la más reciente.
   - Antes de usarla se vuelve a pasar por `ValidarExplicacionAgente` contra el contexto actual, como defensa ante datos guardados alterados. Si no supera la validación, se trata como «no encontrada» y se llama al agente.
3. **Sin explicación reutilizable**, se llama al agente como hoy y, si el análisis queda «Completo», se guarda.
4. Si el almacén de explicaciones no responde al **buscar**, se llama al agente: la reutilización es una optimización y no debe impedir el análisis. Si falla al **guardar**, se aplica el comportamiento actual de HU-20 (503 sin confirmar el guardado).

## 4. Forzar un análisis nuevo (CA4)

- `POST /api/homologaciones/solicitudes/{id}/analisis-interno` admite dos campos opcionales: `forzarNuevo` (bool) y `motivo` (obligatorio si `forzarNuevo` es true; hasta 500 caracteres, sin caracteres de control).
- Solo lo usa el docente asignado. La autorización y la asignación se comprueban igual que hoy, antes de buscar en el almacén (CA5).
- Con `forzarNuevo` siempre se llama al agente. La explicación nueva se guarda con el motivo, el docente y la fecha. **La anterior no se borra.**

## 5. Persistencia

- **Tabla nueva `ExplicacionesAgente`**, solo de inserción, que sirve también de historial de explicaciones:

  | Columna | Tipo |
  | --- | --- |
  | `Id` (PK) | uniqueidentifier |
  | `Clave` | char(64), con índice no único: dos análisis simultáneos pueden insertar la misma clave y no pasa nada |
  | `Fecha` | datetimeoffset |
  | `Modelo` | nvarchar(128) |
  | `VersionPlantilla` | nvarchar(32) |
  | `VersionParametros` | uniqueidentifier |
  | `Explicacion` | nvarchar(max): el JSON de `ExplicacionAgente` |
  | `ForzadoPorId` | uniqueidentifier, null |
  | `Motivo` | nvarchar(500), null |

  En SQL Server, un trigger impide UPDATE y DELETE, igual que en `EventosInforme` (HU-25).
- **`AnalisisInternos` (HU-20) no cambia de política:** el último análisis por pareja de la solicitud sigue reemplazando al anterior. Su JSON gana un bloque `agente`; los análisis guardados antes se leen con `agente = null`.
- No se guarda ningún texto del estudiante en `ExplicacionesAgente`.

## 6. Contrato de respuesta e interfaz

- `ResultadoInternoSolicitud` gana un campo opcional `agente`:
  - **Explicación nueva:** `{ explicacionId, reutilizada: false, modelo, versionPlantilla }`.
  - **Reutilizada:** `{ explicacionId, reutilizada: true, explicacionOriginalId, fechaOriginal, modelo, versionPlantilla }`.
  - Con «Incompleto» o «Fallido», `agente` es null.
- **Interfaz (`AnalisisInternoPanel`):** muestra «Reutilizado · {fecha original} · análisis {id original}» y una casilla «Forzar un análisis nuevo» con el campo «Motivo».
- **Informes (HU-23):** el informe ya generado conserva su copia fija y no cambia (CA5). La reutilización solo afecta a los análisis nuevos.

## 7. Archivos afectados

| Archivo | Dueño | Cambio |
| --- | --- | --- |
| `Homologacion.Application/AnalisisInterno/AsistirEvaluacionInterna.cs` | Pablo | Recibe la clave y el puerto `IExplicacionesAgente`; busca, reutiliza o llama al agente y guarda |
| `Homologacion.Application/AnalisisInterno/AnalizarSolicitudInterna.cs` | Pablo | Calcula las firmas (A1) y pasa `forzarNuevo` y `motivo` |
| `Homologacion.Application/AnalisisInterno/Modelos.cs` | Pablo | Bloque `agente` en el resultado; puerto nuevo |
| `Homologacion.Infrastructure/AnalisisInterno/OllamaAgenteInterno.cs` | Pablo | Constante `VersionPlantilla` |
| `Homologacion.Infrastructure/...` | Edwin | `ExplicacionesAgenteSql`, migración y trigger |
| `Homologacion.Api` (controlador del análisis interno) | Pablo | Campos `forzarNuevo` y `motivo`; Swagger |
| `frontend/.../AnalisisInternoPanel.tsx` | — | Indicación «Reutilizado» y opción de forzar |

Con la opción A2 se añade el contexto interno de Catálogo (Leslie).

## 8. Pruebas previstas (TDD)

- **CA1:** la segunda ejecución con la misma clave no llama al agente (agente simulado con contador); el resultado lleva `reutilizada: true`, la fecha y el id originales.
- **CA2:** cambiar el modelo, la versión de la plantilla, la de los parámetros, una correspondencia, un texto de un programa o las horas de la aprobada provoca una nueva llamada.
- **CA3:** «Incompleto» y «Fallido» no se guardan; tras un fallo, la siguiente ejecución llama al agente.
- **CA4:** `forzarNuevo` llama al agente, exige el motivo (400 sin él) y conserva la explicación anterior.
- **CA5:** 401, 403 y 404 antes de cualquier consulta al almacén; un docente de otra solicitud no obtiene nada por la reutilización.
- **Otras:**
  - la explicación guardada alterada no se reutiliza;
  - con el almacén caído al buscar, se llama al agente;
  - en SQL Server real, la migración y el trigger de solo inserción;
  - smoke en Compose con el agente simulado.

## 9. Preguntas para Pablo

1. **Firmas:** ¿A1 (Homologación las calcula) o A2 (las expone Catálogo)?
2. ¿Se incluye la huella del contexto en la clave (B)?
3. ¿Te parece bien la tabla nueva solo de inserción, sin cambiar la política de reemplazo de `AnalisisInternos`?
4. ¿Algún cambio de nombre o de lugar en `AsistirEvaluacionInterna`? Por ejemplo, un decorador de `IAgenteAnalisisInterno` en lugar de modificar la clase.

## 10. Confirmación de la PO

Shirley confirma los criterios CA1–CA5 con estas reglas antes de codificar.
