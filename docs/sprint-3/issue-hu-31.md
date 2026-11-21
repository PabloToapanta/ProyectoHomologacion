## Historia de usuario
Como docente asignado, quiero registrar mi propio análisis por asignatura (de acuerdo o ajuste, con observación) junto al del agente, para que el informe de homologación refleje la revisión académica de ambos docentes.

## Planificación
- Historia del Sprint 3 (prioridad Alta, 5 PH), adelantada el 04/11/2026 con autorización de Shirley (PO), transmitida por Edwin.
- Responsable: Edwin. Revisión cruzada: Leslie (módulo de Homologación) y Pablo (Gateway).
- Depende de #42 (HU-18), #43 (HU-19), #44 (HU-20) y #41 (HU-30). Rama `feature/hu-31-revision-docente` sobre `feature/hu-19-analisis-externo`: se integra después de #56.
- Contrato: `docs/sprint-3/contrato-hu-31.md`.

## Decisiones
1. Cada uno de los dos docentes asignados registra **su propio** análisis y ve el del otro. El informe (HU-23) se genera a partir de ambos y pasa al coordinador (HU-32/HU-33).
2. El análisis es **por pareja** (asignatura aprobada + receptora), la misma unidad que el análisis del agente.
3. Se admite aunque el agente no haya completado su análisis, que es siempre el caso externo: el docente registra su decisión con observación.

## Criterios de aceptación
- [ ] **CA1.** Un docente asignado registra «De acuerdo» sobre un análisis completo del agente y se guarda la sugerencia del agente con fecha y docente.
- [ ] **CA2.** Un docente registra «Ajuste» con su decisión (Homologable / No homologable) y una observación obligatoria, con o sin análisis completo del agente.
- [ ] **CA3.** «De acuerdo» sin análisis completo del agente, o «Ajuste» sin observación, se rechazan sin guardar.
- [ ] **CA4.** Cada docente asignado ve su análisis y el del otro; corregir el propio reemplaza el anterior.
- [ ] **CA5.** Un docente no asignado, el Coordinador y el Administrador no registran ni consultan; un docente no puede registrar en nombre de otro.
- [ ] **CA6.** El análisis se conserva tras reiniciar el servicio y un fallo de la base no confirma el guardado.

## Fuera de alcance
Informe (HU-23), remisión (HU-32), finalización (HU-33) y cierre del análisis docente al generar el informe.

## Tareas
- [ ] Regla de dominio y caso de uso con TDD.
- [ ] Tabla `RevisionesDocentes`, migración y repositorio.
- [ ] API `PUT/GET /api/homologaciones/solicitudes/{id}/revisiones-docentes` y ruta del Gateway.
- [ ] «Mi análisis» en la vista docente (HU-30), junto al análisis del agente.
- [ ] Pruebas de integración con SQL Server real, smoke con Docker Compose y evidencia.

## Pruebas y evidencia para revisión

- **PA1 → CA1/CA3:** regla y API: «De acuerdo» requiere análisis completo; se deriva
  la decisión de la sugerencia guardada y se rechaza una decisión distinta del cliente.
- **PA2 → CA2/CA3:** «Ajuste» exige decisión y observación, incluso sin agente;
  probar observación vacía/excesiva y postura/decisión inválidas sin guardar.
- **PA3 → CA4:** dos docentes independientes, corrección solo del propio registro,
  conservación de la fotografía del agente y aislamiento entre parejas/solicitudes.
- **PA4 → CA5:** 401/403/404, rechazo de suplantación mediante `docenteId` y nueva
  comprobación de asignación en cada operación.
- **PA5 → CA6:** migración y lectura con conexiones nuevas en SQL Server real;
  reinicio del servicio y fallo de guardado con 503 sin reemplazar el registro previo.
- **PA6 → CA4:** frontend: mostrar únicamente la pareja seleccionada y no anunciar
  en otra pareja la respuesta tardía de un guardado anterior.

Los resultados se registran en `docs/sprint-3/evidencia-hu-31.md`. Las pruebas en
verde no constituyen aceptación de la PO ni revisión cruzada. Quedan pendientes CI,
prueba manual en navegador y registro en esta issue de la autorización de adelanto
de Sprint 3 comunicada por Edwin. No incluye merge ni cierre automático.
