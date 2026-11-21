# HU-21 — Plan técnico borrador para Pablo y Leslie

**Base funcional:** [spec HU-21](hu-21-agrupacion-nm.md) · **Issue:** [#75](https://github.com/PabloToapanta/ProyectoHomologacion/issues/75)

**Estado:** plan técnico borrador sobre el alcance funcional que Pablo comunicó como revisado y acordado por Shirley y el ingeniero el 21/11/2026. Requiere cerrar contrato, reestimar y verificar la implementación.
**Rama de trabajo:** `feature/hu-21-agrupacion-nm` desde `develop`. Pablo creará la Draft PR hacia `develop`; Pablo y Leslie harán commits en su rama.

## Contrato funcional que guía el diseño

1. Un caso puede contener varias parejas 1:1, un vínculo 1:N o una agrupación N:M. Presentar varias parejas 1:1 juntas no mezcla sus cantidades.
2. Una agrupación identifica una solicitud, sus orígenes, sus receptoras y los vínculos origen–receptora. Un origen pertenece a una sola unidad de análisis dentro de la solicitud; puede vincularse a varias receptoras **dentro** de esa unidad.
3. Cada vínculo declara horas y créditos atribuidos con hasta dos decimales. La suma atribuida desde un origen, en todas las receptoras del grupo, no supera sus horas ni sus créditos aprobados. `null` significa pendiente y `0` un aporte conocido de cero. No hay reparto automático ni conversiones.
4. Un contenido o resultado de origen puede respaldar varias receptoras con evidencia revisada y vigente. Cada elemento receptor cuenta una sola vez en su propia cobertura.
   La agrupación reutiliza únicamente las revisiones ya guardadas en el Catálogo; no crea ni aprueba correspondencias. El análisis conserva una fotografía de los vínculos y ubicaciones de evidencia usados.
5. Se calculan cobertura, horas y créditos por receptora. La sugerencia única del grupo es «Homologable» si **todas** las receptoras alcanzan el umbral en los tres criterios; «No homologable» si los datos están completos y alguna no lo alcanza; «Información incompleta» si falta información necesaria de cualquiera. Se conserva la versión de HU-03 usada.
6. Los dos docentes revisan la agrupación como una unidad; el informe conserva el detalle por receptora y la leyenda de regla pendiente de validación institucional.

## Orden de implementación y entregables

| Paso | Responsable inicial | Entregable | Dependencia |
| --- | --- | --- | --- |
| 0. Cerrar contrato y reestimar | Pablo y Leslie; Shirley revisa estimación; Edwin revisa contrato | Esquema de petición/respuesta, identidad del grupo y estimación actualizada en #75 | Acuerdo funcional N:M comunicado por Pablo |
| 1. Pruebas del motor | Pablo | Pruebas fallidas para 1:1 conjunto, 1:N, N:M, límites de asignación, cobertura y veredicto del grupo | Paso 0 |
| 2. Motor determinista | Pablo | Modelo de grupo y cálculo por receptora; resultado único con desglose; pruebas verdes | Paso 1 |
| 3. Persistencia y contratos de grupo | Leslie | Identidad del grupo, vínculos y aportes persistidos; API de consulta/registro con validación de pertenencia y exclusividad | Paso 0; integrar con 2 |
| 4. Análisis docente e informe | Pablo y Leslie, con archivos asignados antes de editar | Dos revisiones por grupo, análisis guardado e informe de una sola unidad con desglose; migración compatible con parejas históricas | Pasos 2–3 |
| 5. Interfaz | Leslie | Tabla de orígenes × receptoras: vínculos con revisión vigente, aportes por celda, sumas/saldos por fila, resultados por columna y revisión grupal | Contratos de 3–4 |
| 6. Integración y evidencia | Pablo y Leslie; Edwin revisa | OpenAPI, pruebas de regresión, integración Docker Compose, evidencia por CA y revisión cruzada | Pasos 2–5 |

## Separación inicial de archivos

| Pablo | Leslie | Coordinación obligatoria |
| --- | --- | --- |
| `Homologacion.Application/Equivalencias` y pruebas del motor; agregación de resultados y política de veredicto | Contratos y persistencia de agrupaciones en `Homologacion.Infrastructure`; pantalla en `frontend/src/features/solicitudes` y sus pruebas | `AnalizarSolicitudInterna`, `GestionarAnalisisInterno`, `ResultadosDbContext`, `RegistrarRevisionDocente`, `GenerarInformeHomologacion`, controladores, `Program.cs`, Gateway y migraciones |

Los archivos de coordinación tienen **un solo editor a la vez**. Antes del primer cambio compartido, acordar campos y nombres de endpoints en la Draft PR. El otro integrante revisa y consume ese contrato en su parte. Evitar commits simultáneos sobre `ResultadosDbContext`, controladores de informe/revisión y páginas que ensamblan los paneles.

## Contrato HTTP de agrupaciones

Mantener intactas las rutas actuales de parejas 1:1. Pablo confirmó rutas propias de agrupaciones bajo `/api/homologaciones/solicitudes/{id}/agrupaciones`. El detalle de cuerpos, respuestas y errores está en el contrato técnico 6 de la spec.

| Operación propuesta | Función |
| --- | --- |
| `POST /` | Registrar un borrador con `origenes[]`, `receptoras[]` y `aportes[]`; devuelve `201` y `grupoId`. |
| `GET /` y `GET /{grupoId}` | Consultar composición, aportes, estado y último análisis. |
| `PUT /{grupoId}` y `DELETE /{grupoId}` | Corregir o eliminar un borrador antes de su primer análisis guardado. |
| `POST /{grupoId}/analisis` | Ejecutar evaluación; devuelve resultado único y desglose por receptora. |
| `GET /{grupoId}/revisiones` y `PUT /{grupoId}/revisiones` | Consultar ambas revisiones y registrar/corregir la del docente autenticado. |

El detalle devuelve ETag; `PUT`, `DELETE` y `POST /{grupoId}/analisis` exigen `If-Match`. Versión obsoleta: 409 sin escritura; versión ausente: 428. El informe existente de la solicitud debe admitir unidades individuales y grupales sin reinterpretar los campos históricos `asignaturaAprobadaId`/`asignaturaReceptoraId`. Los nombres de clases y tablas se fijan en el paso 0 sin cambiar el contrato público descrito. La falta de evidencia académica produce un resultado «Información incompleta» con detalle.

## Persistencia y compatibilidad propuestas

- Crear un registro de grupo con `grupoId` estable y pertenencia a solicitud; tablas hijas para orígenes, receptoras y aportes origen–receptora. Guardar horas aportadas como `decimal(12,2)` y créditos aportados como `decimal(6,2)`; las horas aprobadas actuales son enteras, pero el reparto admite fracciones con dos decimales. Validar cada suma por origen sin redondear, y conservar la distinción entre `null` y `0`.
- Guardar el último análisis por `grupoId` y una revisión por `(grupoId, docenteId)`, con el desglose por receptora. El docente puede corregir su revisión hasta que se genere el informe. No usar un ID de origen o receptora ficticio para encajar en la clave compuesta de las parejas existentes. El informe enumera unidades 1:1 y grupales, cada grupo una sola vez; sus registros históricos 1:1 permanecen legibles.
- Mantener una tabla de reservas con clave única `(solicitudId, asignaturaAprobadaId)` y propietario (pareja o grupo). Serializar por solicitud y actualizar reservas/grupo en una sola transacción; una colisión devuelve 409 sin escritura parcial. Integrar la reserva en el primer guardado de análisis 1:1 y comprobar parejas históricas antes de agrupar. El borrador puede editarse o eliminarse con liberación transaccional hasta el primer análisis guardado; después su composición queda fija. Un intento con aportes `null` devuelve faltantes sin guardar el primer análisis. La migración conserva resultados e informes ya emitidos; no les inventa agrupaciones.
- Para orígenes externos sin programa analítico registrado, mantener «Información incompleta» según D2 de HU-19. HU-21 no crea ese programa.

## Pruebas que cierran los criterios

- **T62:** porcentajes por receptora y tope 100 %; un elemento receptor con dos evidencias cuenta una vez; un elemento de origen puede respaldar dos receptoras con vínculos trazables; suma de horas/créditos por origen ≤ disponible; tres criterios por cada receptora; grupo con un fallo o un faltante; unidades distintas.
- **T63:** pertenencia a solicitud y carrera, elegibilidad HU-15/HU-17, exclusividad del origen frente a otra agrupación o pareja, concurrencia, dos revisiones docentes por grupo, informe único con desglose, lectura de datos históricos 1:1.
- **T64:** crear y consultar las tres situaciones, introducir aportes explícitos, mostrar faltantes/errores y la leyenda; prueba de integración de la pantalla con el contrato.
- **Regresión:** rutas individuales, informe y configuración HU-03 continúan funcionando; OpenAPI y ejecución integrada con Docker Compose.

## Coordinación de la Draft PR

1. Publicar la spec y este plan en la rama y abrir la Draft PR enlazando #75. La descripción distingue el acuerdo funcional comunicado, la validación institucional aún pendiente y el estado real de pruebas.
2. Leslie revisa el contrato y comenta allí los campos/archivos de su parte. Pablo incorpora los acuerdos a la spec antes de iniciar TDD. Edwin revisa cambios sobre motor, revisión docente e informe; Shirley valida criterios y alcance.
3. Cada uno trabaja en su checkout de **la misma rama remota**; antes de empezar un archivo compartido, sincroniza la rama y avisa qué archivo tomará. Commits pequeños con Conventional Commits en español y referencia a T62/T63/T64.
4. Integrar los commits cada día, resolver conflictos de uno en uno y mantener la PR como Draft hasta pruebas, revisión y aceptación. No hacer merge ni declarar la historia terminada antes de la definición de hecho.

**Capacidad:** las 14 h del backlog estimaban un alcance N:1 más acotado. 1:N y N:M añaden reparto, nuevas entidades y cambios a revisión e informe. Pablo y Leslie deben reestimar T62–T64 con Shirley antes de comprometer fecha o avance.
