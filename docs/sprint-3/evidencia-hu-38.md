# HU-38 · Catálogo de normativas — contrato y evidencia

**Issue:** [#107](https://github.com/PabloToapanta/ProyectoHomologacion/issues/107).
**Estado:** implementada el 25/11/2026 en la rama `feature/hu-38-catalogo-normativas`, desde `develop` (`b326843`). Pendiente: revisión cruzada y aceptación de la Product Owner.
**Origen:** propuesta en `docs/backlog/propuesta-hu-38-catalogo-normativas.md` (PR #106). Shirley la aprobó para el Sprint 3 el 25/11/2026, según transmitió Edwin, con las decisiones de la tabla siguiente.

## Decisiones de la Product Owner (25/11/2026)

| Tema | Decisión |
| --- | --- |
| ¿Qué normativa rige? | Siempre la aprobada más reciente. Al registrar una reforma, la anterior pasa a «Reformada». |
| ¿Quién registra y deroga? | Administrador y Coordinador. El Docente solo consulta. |
| ¿Quién elimina? | Solo el Administrador, para corregir un registro equivocado (por ejemplo, del Coordinador), con un motivo. |
| Parámetros de homologación (HU-03) | Los gestionan el Coordinador y también el Administrador. El respaldo de cada cambio se elige entre las normativas vigentes. |
| Automatización | Aviso determinista cuando la normativa citada por la configuración vigente queda reformada o derogada. El sistema y el agente **no** leen los PDF ni cambian valores. |

## Contrato

| Elemento | Detalle |
| --- | --- |
| Dominio (Catálogo) | `Normativa`: código único sin distinguir mayúsculas; título; emisor; fechas de emisión y vigencia (la vigencia no puede ser anterior a la emisión); estado `Vigente`/`Reformada`/`Derogada`; reforma a; reformada por; PDF validado por su contenido (`%PDF-`, hasta 10 MB) con huella SHA-256. `EventoNormativa`: Registrada, Reformada, Restituida, Derogada y Eliminada, con fecha y usuario. |
| Persistencia | Migración `20261125165611_NormativasHu38`: tablas `Normativas` (índice único por código normalizado) y `EventosNormativa` (sin clave foránea, para conservar el historial tras una eliminación). Trigger `TR_EventosNormativa_SoloInsercion` en SQL Server. |
| API (Catálogo, vía Gateway) | `GET /api/normativas?estado=`, `GET /api/normativas/{id}`, `GET /api/normativas/{id}/pdf`, `GET /api/normativas/{id}/historial`: Administrador, Coordinador y Docente. `POST /api/normativas` (multipart) y `POST /api/normativas/{id}/derogacion`: Administrador y Coordinador. `DELETE /api/normativas/{id}` con `{ motivo }`: solo Administrador; 409 si la normativa ya fue reformada; si era una reforma, la anterior vuelve a «Vigente» (evento Restituida). |
| Configuración (HU-03) | `ConfiguracionController` y la política del Gateway `ConfigurationManagement` admiten también al Administrador. El contrato no cambia: el respaldo se guarda como texto con el formato `CÓDIGO — Título [· detalle]`. |
| Frontend | Página **Normativas** para los tres roles: lista con filtro, registro, derogación, eliminación, PDF e historial. En **Configuración**, el respaldo es un selector de normativas vigentes más un detalle opcional, con un aviso si la normativa citada ya no rige. El menú muestra Configuración también al Administrador. |
| Gateway | Rutas `normativas*` hacia Catálogo con `AcademicAccess`; los permisos por operación los aplica Catálogo. Ruta de interfaz `/normativas` en `appsettings.Hu34.json`. |

## Criterios de aceptación y su prueba

| Criterio | Prueba | Estado |
| --- | --- | --- |
| CA1. Registro con datos obligatorios y PDF; rechazo de datos incompletos, archivo que no es PDF o fechas incoherentes | `NormativaTests`; `NormativasApiTests.CA1_*`; smoke HU-38 | Verificado |
| CA2. Código único; una reforma deja la anterior como Reformada y rige la más reciente | `NormativasApiTests.CA2_*`; smoke HU-38 | Verificado |
| CA3. Coordinador y Docente consultan, filtran y descargan; el Docente no registra ni deroga | `NormativasApiTests.CA3_*`; `NormativasPage.test.tsx`; smoke HU-38 | Verificado |
| CA4. Historial con fecha y usuario; derogar no elimina. Eliminación solo del Administrador, con motivo y evento en el historial | `NormativasApiTests.CA4_*` y `Administrador_elimina_*`; trigger probado en el smoke con SQL Server real | Verificado |
| CA5. La configuración cita la normativa y avisa cuando la citada ya no rige, sin alterar el cálculo | `ConfiguracionPage.test.tsx` (HU-38); `normativaDelRespaldo` | Verificado en el frontend. Ver limitación 1. |
| CA6. 401 sin sesión, 403 por rol, Swagger | `NormativasApiTests.CA6_*`; `GatewayHttpTests.NormativasHu38_*`; `ConfiguracionEndpointTests.HU38_*` | Verificado |

## Ejecución registrada (25/11/2026)

| Suite | Resultado |
| --- | --- |
| Catalogo.Tests | 167 superadas, 4 omitidas (16 nuevas) |
| Gateway.Tests | 164 superadas |
| Homologacion.Api.Tests | 166 superadas |
| Homologacion.Application.Tests | 257 superadas |
| Frontend (Vitest) | 230 superadas; `tsc` y `vite build` correctos |
| Smoke `tests/security/hu38-smoke.py` (Gateway, JWE, servicios y SQL Server reales; proyecto aislado) | 24 comprobaciones superadas; el proyecto aislado se eliminó al terminar |
| Migración en el stack local del equipo (SQL Server 2022) | Aplicada al arrancar Catálogo; tablas y trigger presentes. |

Las pruebas se escribieron antes que el código, pero la primera ronda no llegó a ejecutarse en rojo porque todavía no compilaba.

## Limitaciones conocidas

1. Homologación no valida en el servidor que el respaldo cite una normativa vigente. La validación y el aviso están en la interfaz. Validarlo en el servidor exigiría una consulta de Homologación a Catálogo; queda como mejora.
2. Los respaldos guardados antes de HU-38 son texto libre: la página indica que no citan una normativa del catálogo.
3. No se revisaron los instructivos de 2026 frente a las reglas actuales. Es una consulta académica, no una tarea de esta historia.
4. No se probó con HTTPS en el smoke aislado. El stack local con HTTPS sirve `/normativas` (200) y protege `/api/normativas` sin sesión (401).
