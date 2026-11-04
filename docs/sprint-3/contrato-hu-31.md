# Contrato HU-31 — análisis propio del docente junto al del agente

Historia del Sprint 0 (Sprint 3, prioridad Alta, 5 PH): «Como docente, quiero registrar mi propio análisis
por asignatura (de acuerdo o ajuste, con observación) junto al del agente.» Adelantada al 04/11/2026 con
autorización de Shirley, transmitida por Edwin. Base: `feature/hu-19-analisis-externo`; el PR se integra
después de #56.

## Decisiones (Edwin, 04/11/2026)

1. **Cada docente asignado registra su propio análisis.** Los dos se ven entre sí. El informe de
   homologación (HU-23) se genera a partir de ambos y luego pasa al coordinador (HU-32/33); eso queda fuera.
2. **Por pareja**: asignatura aprobada + asignatura receptora, la misma unidad del análisis del agente
   (HU-18 interno, HU-19 externo).
3. **También sin análisis completo del agente** (incompleto, fallido o no ejecutado, que es siempre el caso
   externo mientras rija D2 de HU-19): el docente registra su propia decisión con observación obligatoria.

## Reglas

| Postura | Cuándo | Decisión guardada | Observación |
|---|---|---|---|
| `DeAcuerdo` | Solo si el último análisis guardado del agente para la pareja es `Completo` y tiene sugerencia | La sugerencia del agente; el cliente no puede enviar otra | Opcional (hasta 2000) |
| `Ajuste` | Siempre | `Homologable` o `NoHomologable`, elegida por el docente | Obligatoria (1–2000) |

- La identidad sale del claim `sub` del JWT, nunca del cuerpo de la petición. Solo los docentes asignados
  (HU-29) pueden registrar o consultar; la asignación se vuelve a comprobar en Solicitudes en cada petición.
- La asignatura aprobada debe pertenecer a la solicitud, y la receptora a su carrera receptora (Catálogo).
- Se guarda una fotografía del análisis del agente al registrar: estado y sugerencia. Si el agente se vuelve a ejecutar
  después, el registro conserva contra qué análisis se emitió.
- Un docente puede corregir su análisis: el último registro por (solicitud, pareja, docente) reemplaza al
  anterior y actualiza la fecha. No hay cierre todavía; el bloqueo al generar el informe es de HU-23.
- No es una resolución oficial ni aprueba la solicitud.

## API (Homologación, rol Docente)

| Método | Ruta | Resultado |
|---|---|---|
| PUT | `/api/homologaciones/solicitudes/{id}/revisiones-docentes` | Registra o corrige el análisis propio de una pareja. Cuerpo: `asignaturaAprobadaId`, `asignaturaReceptoraId`, `postura`, `decision?`, `observacion?`. 200 con el registro; 400 si las reglas no se cumplen; 404 si la solicitud no está asignada al docente; 503 si una fuente o la base no responde. |
| GET | `/api/homologaciones/solicitudes/{id}/revisiones-docentes` | Los análisis de los docentes asignados, por pareja y docente. |

Persistencia: tabla `RevisionesDocentes` en `HomologacionAnalisis`, clave (solicitud, aprobada, receptora,
docente), migración propia. El Gateway publica ambas rutas con `AcademicAccess`.

## Vista docente (HU-30)

Bajo el resultado del agente de la pareja seleccionada, «Mi análisis»:
- La opción «De acuerdo» solo está habilitada si el agente dio una sugerencia.
- La opción «Ajuste» pide la decisión y la observación.
- Debajo aparecen los análisis registrados de ambos docentes para esa pareja.

## Criterios de aceptación propuestos

- **CA1.** Un docente asignado registra «De acuerdo» sobre un análisis completo del agente y se guarda la sugerencia del agente con fecha y docente.
- **CA2.** Un docente registra «Ajuste» con su decisión y una observación obligatoria, con o sin análisis completo del agente.
- **CA3.** «De acuerdo» sin análisis completo del agente, o «Ajuste» sin observación, se rechazan sin guardar.
- **CA4.** Cada docente asignado ve su análisis y el del otro docente; corregir el propio reemplaza el anterior.
- **CA5.** Un docente no asignado, el Coordinador y el Administrador no registran ni consultan; un docente no puede registrar en nombre de otro.
- **CA6.** El análisis se conserva tras reiniciar el servicio y un fallo de la base no confirma el guardado.

## Fuera de alcance

Informe (HU-23), remisión (HU-32), finalización por el coordinador (HU-33) y cierre del análisis docente.
