# HU-37 · Reutilizar el análisis del agente — evidencia

Issue [#77](https://github.com/PabloToapanta/ProyectoHomologacion/issues/77). Rama `feature/hu-37-reutilizar-analisis`, actualizada con `develop`. Responsables: Edwin y Shirley.

- **Contrato:** [propuesta-contrato-hu-37.md](propuesta-contrato-hu-37.md). Pablo lo aceptó completo el 22/11/2026, según transmitió Edwin.
- **Revisión cruzada:** Pablo, porque el cambio toca la orquestación de HU-18 y la persistencia de HU-20.
- **Pendiente:** la confirmación de Shirley (PO) de CA1–CA5 y su aceptación funcional.

## Reglas aplicadas

Son reglas de análisis del proyecto, no un pronunciamiento institucional.

- **Qué se reutiliza:** solo la explicación del agente (`ExplicacionAgente`).
  - El motor determinista se ejecuta en cada análisis: porcentajes, veredicto, antigüedad e información faltante.
  - El agente nunca decide.
- **Alcance:**
  - Solo el análisis interno (HU-18).
  - El externo (HU-19) no llama al agente (D2) y no cambia.
  - El caso de tercera matrícula (HU-17) no llega al agente.
- **Clave de reutilización:** SHA-256 de un JSON canónico con:
  - la firma de la pareja: identificadores de catálogo, firmas de ambos programas y firma de las correspondencias;
  - `VersionId` de los parámetros (HU-03);
  - el modelo configurado;
  - `OllamaAgenteInterno.VersionPlantillaActual` (`hu18-v1`);
  - la huella del `ContextoAgente` que se enviaría al modelo.
- **Firmas de los programas (opción A1):** las calcula Homologación con el perfil, las horas, la unidad, los créditos, el sistema y los contenidos y resultados con sus textos.
  - La firma del programa de origen usa las horas y los créditos registrados en la aprobada.
  - No se toca Catálogo.
- **Caducidad:** no caduca por tiempo. Una clave distinta simplemente no encuentra nada.
- **Qué se guarda:** solo se guarda un análisis «Completo».
  - «Incompleto» no llama al agente ni consulta el almacén.
  - «Fallido» no se guarda.
- **Validación antes de reutilizar:** la explicación guardada vuelve a pasar `ValidarExplicacionAgente` contra el contexto actual.
  - Si no la supera o es ilegible, se trata como no encontrada y se llama al agente.
- **Almacén no disponible:**
  - Al buscar, se llama al agente.
  - Al guardar, responde 503 sin confirmar el análisis, como en HU-20.
- **Agente sin identificación:** sin modelo configurado (`AgenteNoConfigurado`) no hay reutilización ni se consulta el almacén. Solo un agente que declara modelo y plantilla (`IDescripcionAgente`) participa.
- **Datos del estudiante:** `ExplicacionesAgente` no guarda ningún dato del estudiante.

## API

`POST /api/homologaciones/solicitudes/{id}/analisis-interno` (rol Docente):

- **Petición:** admite `forzarNuevo` (bool, opcional) y `motivo`.
  - Con `forzarNuevo`, el motivo es obligatorio: hasta 500 caracteres, sin caracteres de control salvo salto de línea y tabulador.
  - El motivo sin `forzarNuevo` también es 400. Esta regla se añadió para no aceptar datos que se ignorarían.
  - Ambas validaciones van antes de consultar Solicitudes.
- **Autorización y asignación:** no cambian. 401, 403 y 404 se resuelven antes de cualquier consulta al almacén (CA5).
- **Respuesta:** gana `agente`:
  - **Explicación nueva:** `{ explicacionId, reutilizada: false, explicacionOriginalId: null, fechaOriginal: null, modelo, versionPlantilla }`.
  - **Reutilizada:** `{ explicacionId, reutilizada: true, explicacionOriginalId, fechaOriginal, modelo, versionPlantilla }`.
  - **«Incompleto» o «Fallido»:** `null`.
  - **`GET` del mismo recurso (HU-20):** devuelve el bloque guardado. Los análisis anteriores a HU-37 se leen con `agente = null`.
- **Rutas:** no se añaden rutas al Gateway.

## Persistencia

- **Tabla `ExplicacionesAgente`:** migración `20261124000100_ExplicacionesAgenteHu37`.
  - Columnas: `Id`, `Clave char(64)` con índice no único, `Fecha`, `Modelo`, `VersionPlantilla`, `VersionParametros`, `Explicacion` (JSON), `ForzadoPorId` y `Motivo`.
  - El trigger `TR_ExplicacionesAgente_SoloInsercion` impide UPDATE y DELETE en SQL Server.
  - El puerto `IExplicacionesAgente` solo ofrece `BuscarAsync` y `GuardarAsync`.
- **`AnalisisInternos` (HU-20):** conserva su política de reemplazo por pareja. Su JSON gana el bloque `agente`.

## Interfaz

`AnalisisInternoPanel`, solo en el análisis interno:

- Casilla «Forzar un análisis nuevo» con el campo «Motivo» obligatorio. Al ejecutar, envía `forzarNuevo` y el motivo recortado.
- Tras una reutilización muestra «Reutilizado · {fecha original} · análisis {id original}».
- El análisis externo no ofrece la casilla.

## Archivos

| Capa | Archivos |
| --- | --- |
| Application | `AnalisisInterno/ReutilizacionAgente.cs` (nuevo), `AsistirEvaluacionInterna.cs`, `AnalizarSolicitudInterna.cs`, `ContratoSolicitud.cs`, `Modelos.cs`, `ResultadosAnalisisInterno.cs` |
| Infrastructure | `AnalisisInterno/ExplicacionesAgenteSql.cs` (nuevo), `ResultadosDbContext.cs`, `MigracionResultados.cs`, `ResultadosDbContextModelSnapshot.cs`, `OllamaAgenteInterno.cs`, `RegistroAnalisisInterno.cs` |
| Api | `Contracts/AnalisisInternoRequest.cs`, `Contracts/AnalisisInternoResponse.cs`, `Controllers/AnalisisInternoController.cs` |
| Frontend | `features/solicitudes/AnalisisInternoPanel.tsx` y su prueba |
| Pruebas | `ReutilizacionAgenteTests.cs`, `ExplicacionesAgenteSqlTests.cs`, `ReutilizacionAnalisisEndpointTests.cs`, `tests/security/hu37-smoke.py` |

## Verificación ejecutada el 22/11/2026

| Prueba | Resultado |
| --- | --- |
| Homologación Domain | 115 superadas |
| Homologación Application (incluye 27 de HU-37) | 224 superadas |
| Homologación Infrastructure (SQLite; incluye 6 de HU-37) | 86 superadas, 17 omitidas (requieren SQL Server real u Ollama) |
| Homologación Infrastructure con SQL Server 2022 desechable | 98 superadas, 5 omitidas (Ollama real y otras externas); incluye la migración y el trigger de HU-37 |
| Homologación Api (incluye 6 de HU-37) | 160 superadas |
| Frontend (Vitest, `--maxWorkers=2`) | 211 superadas; `npm run build` compila |
| Smoke Compose `tests/security/hu37-smoke.py` | 60/60 comprobaciones superadas (Gateway, JWE, Auth, Catálogo, Solicitudes, Homologación y SQL Server reales; doble HTTP de Ollama). Incluye la reutilización entre solicitudes, el forzado con motivo, el cambio de correspondencia, el trigger frente a UPDATE y DELETE, y la reutilización con el modelo detenido |

Las pruebas de HU-37 se escribieron antes del código. La primera ejecución falló al compilar porque los tipos aún no existían.

### Cobertura de los criterios

| CA | Pruebas |
| --- | --- |
| CA1 | Misma clave sin llamar al agente, con id y fecha originales; otra aprobada u otra solicitud con el mismo programa reutiliza; la explicación guardada no contiene datos del estudiante (también en el smoke) |
| CA2 | Cambiar el modelo, la plantilla, la versión de parámetros, la firma de la pareja, las horas de la aprobada, el texto de un contenido, el perfil del receptor o una correspondencia provoca una llamada nueva |
| CA3 | Ningún tipo de fallo del agente se guarda, y tras el fallo se llama de nuevo; «Incompleto» no busca ni guarda; forzar sin modelo queda «Fallido» sin fila nueva |
| CA4 | Forzar llama al agente, guarda docente, motivo y fecha y conserva la anterior; después se reutiliza la más reciente; 400 sin motivo, con motivo vacío o con motivo sin forzar |
| CA5 | 401, 403 y 404 antes de consultar el almacén; un docente ajeno no obtiene nada por la reutilización |

### No verificado

- Ollama real: el smoke usa el doble HTTP `hu18-ollama-fake.py`.
- HTTPS: el smoke usa HTTP local aislado.
- Prueba manual en el navegador.
- Aceptación de la PO.
