# HU-03 — Configuración de umbral, años máximos y mínimos

**Issue:** [#67 — HU-03](https://github.com/PabloToapanta/ProyectoHomologacion/issues/67)  
**Estado:** especificación de borrador para revisión; no autoriza todavía la implementación.  
**Sprint:** 3 (planificación del Issue: 5–25 de noviembre de 2026).  
**Responsable de la historia:** Pablo Toapanta.  
**Revisión técnica de reglas HU-15/HU-16:** Edwin Caraguay. **Aceptación funcional:** Shirley Amaguaña (PO).

## Historia de usuario

| Dato | Definición propuesta |
| --- | --- |
| Número de historia | HU-03 |
| Usuario | Coordinador (propuesta; pendiente de confirmación de Shirley) |
| Prioridad | Media (Issue #67) |
| Riesgo | Alto, propuesto: el cambio afecta veredictos del motor y reglas compartidas HU-14/15/16 |
| Nombre | Configuración de umbral, años máximos y mínimos |
| Puntos de historia | 3 PH (Product Backlog del Sprint 0 / Issue #67) |
| Responsable | Pablo Toapanta |
| Descripción | Como coordinador, quiero consultar y configurar los parámetros académicos vigentes de equivalencia, antigüedad y requisitos mínimos de cambio, para que los análisis nuevos usen reglas explícitas y trazables. |

## Criterios de aceptación propuestos

Los criterios de Issue #67 y su comentario del 20/11 son la base. La PO debe confirmarlos antes de codificar.

1. **CA1 — Valores iniciales conservados.** Con la configuración inicial, el umbral es 80 %, los mínimos coinciden exactamente con HU-16 y la antigüedad se inicia en 10 años como valor provisional. El motor mantiene los resultados actuales en las pruebas de regresión.
2. **CA2 — Consulta y modificación autorizadas.** Un usuario con el rol acordado puede consultar la configuración. Solo el rol acordado puede modificarla; sin sesión se responde 401 y con otro rol 403. Los parámetros se validan en servidor: umbral mayor que 0 y hasta 100; años y mínimos enteros positivos dentro de límites acordados.
3. **CA3 — Aplicación temporal.** Los análisis iniciados después de un cambio usan una sola versión vigente de parámetros. Los análisis e informes ya persistidos conservan la fotografía usada al crearse y no cambian retroactivamente.
4. **CA4 — Auditoría y reproducción.** Cada cambio registra actor, fecha, valores anteriores y nuevos, motivo y respaldo. Cada análisis conserva los parámetros/versionado con los que se calculó. Recalcular requiere una acción explícita y genera un nuevo registro auditable; no sobrescribe silenciosamente el análisis previo.
5. **CA5 — Mínimos internos y externos.** La configuración incluye por separado los períodos y asignaturas homologables mínimos para cambio interno y externo, y la regla de cambios previos que ya aplica HU-16. No permite configuraciones que contradigan las reglas acordadas ni elimina la condición externa de cero cambios previos sin aprobación expresa.
6. **CA6 — Contrato y evidencia.** Endpoints documentados en OpenAPI; pruebas unitarias, API, autorización y regresión comprueban límites, configuración inicial, aplicación a análisis nuevos, conservación de resultados históricos y auditoría. La aceptación de Shirley queda registrada como evidencia separada.

## Alcance y decisiones que deben acordarse

### Alcance propuesto

- Crear una fuente de parámetros persistida y versionada, propiedad de Homologación; el motor recibe una instantánea explícita para ejecutar HU-14, HU-15 y HU-16.
- Conservar inicialmente 80 % para HU-14, los mínimos actuales de HU-16 (interno: 1 período y 1 asignatura homologable; externo: 2 períodos y 2 asignaturas homologables, sin cambios previos), y 10 años para HU-15 mientras se resuelve la discrepancia normativa.
- Exponer consulta y actualización controlada por Gateway y servicio. Registrar motivo y referencia de respaldo sin guardar documentos personales o restringidos en el historial.
- Los cambios rigen desde la versión nueva; los registros previos siguen siendo reproducibles con la versión que almacenaron.

### Decisiones requeridas antes de implementación

| Decisión | Evidencia disponible | Acuerdo requerido |
| --- | --- | --- |
| Antigüedad inicial | Código y pruebas HU-15 actuales usan diez años. Comentario de Issue #67 (20/11/2026) señala discrepancia entre lectura del RRA/CES (10) y el instructivo UTA RES CAU-P-005-2023 (5). El ingeniero aclara que su lectura no sustituye pronunciamiento institucional. | Edwin revisa el impacto técnico; Shirley define el valor inicial del producto o confirma 10 como provisional hasta pronunciamiento UTA. No atribuir una decisión legal/institucional al código. |
| Rol editor | Issue propone Coordinador, comentario dice que la PO debe confirmarlo. | Shirley confirma Coordinador frente a Administrador. |
| Mínimos de HU-16 | Valores existentes: interno 1/1; externo 2/2 y ningún cambio previo. | Edwin confirma preservación y aclara si años máximos aplican también a elementos usados en conteos mínimos. |
| Rango de parámetros | Issue solo dice umbral 0–100, años y mínimos positivos. | Acordar si umbral admite 0 (hoy el dominio exige >0), máximos configurables razonables, límites por tipo de cambio y validación de cambios previos. |
| Registro de respaldo | Issue exige motivo y respaldo, sin definir formato. | Acordar si basta referencia/URL/identificador y qué roles pueden consultar el historial. No almacenar datos sensibles sin necesidad. |
| Recalcular análisis | Issue indica una acción explícita registrada. | Confirmar si HU-03 incluye endpoint de recálculo o solo conserva la condición para historias de análisis; definir permisos, idempotencia y relación entre versiones. |
| Interfaz | Issue propone T57 (2 h), pero el objetivo puede implicar más superficie que una pantalla simple. | Confirmar campos, estados y permisos después de acordar el contrato de API y el esfuerzo. |

## Tareas propuestas

Distribución de historia points acordada como propuesta: **1 + 1 + 1 = 3 PH**. Las horas de T56/T57 del Issue son estimaciones originales (4 h + 2 h) y no son puntos de historia.

| Número de tarea | Número de historia | Nombre | Descripción | Puntos de historia |
| --- | --- | --- | --- | ---: |
| T56 | HU-03 | Definir contrato y persistencia versionada | Con Edwin y Shirley, cerrar tabla de parámetros, valores iniciales, límites, rol, auditoría y versionado; proponer DTO/API; definir cómo los análisis capturan una versión y cómo conviven los históricos. No modificar todavía las reglas hasta que el contrato tenga acuerdo. | 1 PH |
| T57 | HU-03 | Integrar parámetros con reglas HU-14/15/16 | Tras aprobación de la spec, implementar el almacén versionado y adaptar los casos de uso para recibir parámetros explícitos. Mantener los valores de HU-16 y proteger invariantes de HU-15; cubrir regresión de equivalencia, aniversarios y mínimos. Coordinar cada cambio de reglas con Edwin. | 1 PH |
| T58 | HU-03 | API, permisos, auditoría y pantalla de configuración | Implementar consulta/actualización para el rol confirmado, validación de límites, registro auditable, documentación OpenAPI, Gateway e interfaz acordada. Verificar autorización e integración y registrar pendientes de PO por separado. | 1 PH |
| **Total** | **HU-03** |  | **Suma de tareas** | **3 PH** |

## Fuera de alcance propuesto

- Cambiar por inferencia los mínimos de HU-16 o la fórmula de HU-14.
- Declarar que 5 o 10 años es una norma institucional definitiva sin pronunciamiento autorizado.
- Modificar retroactivamente análisis o informes ya guardados.
- Recalcular en lote o sobrescribir resultados sin acción explícita y auditoría.
- Implementar historias adicionales de solicitudes, informes o administración de usuarios.

## Secuencia de trabajo

1. Acordar esta especificación con Edwin (reglas HU-15/HU-16) y Shirley (prioridad, rol, criterios y valor provisional).
2. Actualizar esta ficha según los comentarios y reflejar el acuerdo en el Issue #67.
3. Solo con el contrato aprobado, marcar el PR draft como listo y comenzar TDD e implementación.
4. Completar revisión cruzada de Edwin, CI y verificación integrada. La PO acepta funcionalmente y la evidencia se registra aparte.

La ficha no afirma que la historia esté implementada ni aceptada.
