# HU-03 — Configuración de umbral, años máximos y mínimos

**Issue:** [#67 — HU-03](https://github.com/PabloToapanta/ProyectoHomologacion/issues/67)  
**Estado:** especificación de borrador para revisión; no autoriza todavía la implementación.  
**Sprint:** 3 (planificación del Issue: 5–25 de noviembre de 2026).  
**Responsable de la historia:** Pablo Toapanta.  
**Revisión técnica de reglas HU-15/HU-16:** Edwin Caraguay. **Aceptación funcional:** Shirley Amaguaña (PO).

## Historia de usuario

| Dato | Definición acordada/propuesta |
| --- | --- |
| Número de historia | HU-03 |
| Usuario | Coordinador |
| Prioridad | Media (Issue #67) |
| Riesgo | Alto: la configuración afecta decisiones del motor y reglas compartidas HU-14/15/16 |
| Nombre | Configuración de umbral, años máximos y mínimos |
| Puntos de historia | 3 PH (Product Backlog del Sprint 0 / Issue #67) |
| Responsable | Pablo Toapanta |
| Descripción | Como coordinador, quiero consultar y configurar los parámetros académicos vigentes de equivalencia, antigüedad y requisitos mínimos de cambio, para que los análisis nuevos usen reglas explícitas y trazables. |

## Criterios de aceptación propuestos

Los criterios se expresan en Given / When / Then. La PO debe confirmar la versión final antes de codificar.

### CA1 — Valores iniciales

- **Given** el sistema se inicia con su configuración predeterminada.
- **When** se consulta la configuración o se ejecuta una evaluación de equivalencia, antigüedad o requisitos de cambio.
- **Then** el umbral es 80 %, la antigüedad máxima es 10 años y los mínimos de HU-16 conservan sus valores actuales: internos 1 período y 1 asignatura homologable; externos 2 períodos y 2 asignaturas homologables, sin cambios previos.

### CA2 — Consulta, autorización y rangos

- **Given** un Coordinador autenticado consulta la configuración.
- **When** el servicio autoriza la petición.
- **Then** devuelve los parámetros vigentes.
- **Given** un Coordinador autenticado modifica uno o más parámetros con valores válidos.
- **When** ingresa también un motivo y un respaldo en texto libre, ambos obligatorios.
- **Then** el sistema valida y guarda el cambio y su registro de auditoría.
- **Given** una consulta o modificación llega sin sesión válida o con un rol distinto de Coordinador.
- **When** el servicio autoriza la petición.
- **Then** responde 401 sin sesión válida y 403 para roles no autorizados.
- **Given** la modificación contiene umbral igual o menor que 0, umbral mayor que 100, o años/mínimos no enteros positivos o fuera de límites acordados.
- **When** el servicio valida la entrada.
- **Then** rechaza el cambio y conserva intacta la configuración vigente.

### CA3 — Configuración aplicable a análisis futuros

- **Given** el Coordinador guarda una configuración nueva.
- **When** se inicia un análisis posteriormente.
- **Then** el motor usa los parámetros vigentes al inicio de ese análisis y conserva con el resultado la configuración usada.
- **Given** ya existe un análisis calculado con una configuración anterior.
- **When** cambia la configuración vigente.
- **Then** el análisis anterior no se recalcula ni modifica; HU-03 no ofrece cálculo previo ni recálculo de análisis con la nueva configuración.

### CA4 — Auditoría de cambios

- **Given** un Coordinador realiza un cambio válido.
- **When** el sistema persiste el cambio.
- **Then** guarda actor, fecha, valores anteriores y nuevos, motivo ingresado por el Coordinador y respaldo ingresado por el Coordinador como texto libre.
- **Given** se consulta el historial de configuración.
- **When** el usuario cuenta con autorización acordada.
- **Then** puede revisar los cambios registrados conforme a los permisos que se definan para esa consulta.

### CA5 — Parámetros de mínimos HU-16

- **Given** el Coordinador configura los campos actuales de mínimos de HU-16.
- **When** se valida un cambio interno o externo.
- **Then** el sistema utiliza los mínimos configurados para cada tipo, manteniendo separadas las reglas internas y externas; la condición actual de cero cambios previos para cambio externo permanece fija y no se convierte en parámetro editable.
- **Given** el Coordinador envía valores inválidos para esos campos.
- **When** se valida la configuración.
- **Then** se rechaza toda la modificación y no se guarda parcialmente.

### CA6 — Pantalla, OpenAPI y verificación

- **Given** un Coordinador autenticado abre la pantalla de configuración.
- **When** consulta o modifica los parámetros.
- **Then** la pantalla presenta los campos configurables, valores vigentes, validaciones, campos obligatorios de motivo y respaldo, confirmación de guardado y errores del servidor.
- **Given** se ejecutan las verificaciones de HU-03.
- **When** corren las pruebas unitarias, API, autorización, interfaz y regresión.
- **Then** comprueban valores iniciales, límites, auditoría, aplicación a análisis futuros, no alteración de análisis existentes y compatibilidad de las reglas HU-14/15/16; OpenAPI documenta los endpoints.

## Alcance acordado y decisiones pendientes

### Alcance

- HU-03 vuelve configurables los parámetros existentes: umbral HU-14, años máximos HU-15 y campos de mínimos HU-16. No agrega reglas nuevas.
- Valores iniciales: 80 %, 10 años, mínimos internos de 1 período/1 asignatura homologable y externos de 2 períodos/2 asignaturas homologables.
- Coordinador consulta y configura. Shirley confirmó el rol editor.
- Cada cambio requiere motivo y respaldo obligatorios, ambos ingresados por el Coordinador y guardados en la auditoría. El respaldo es texto libre.
- Los parámetros nuevos aplican a los análisis iniciados posteriormente. No se recalculan análisis existentes ni se permite hacer cálculos previos con una nueva configuración.
- La historia incluye pantalla de configuración.
- Los cambios se persisten y versionan para saber qué configuración aplicó cada análisis; valores históricos no se modifican.

### Detalles técnicos aún por acordar antes de codificar

| Decisión | Acuerdo actual | Pendiente |
| --- | --- | --- |
| Umbral | Mayor que 0 y menor o igual a 100. | Ninguno sobre el rango. Definir precisión decimal permitida en el contrato (porcentaje entero o decimal). |
| Años y mínimos | Deben ser enteros positivos. | Acordar valores máximos razonables para validación y UX. |
| Campos HU-16 | Son los actuales: períodos y asignaturas homologables mínimos por tipo de cambio. Interno 1/1, externo 2/2. Cero cambios previos para externo queda fijo. | Edwin revisa que esta lectura conserve íntegramente las reglas vigentes y su comportamiento. |
| Auditoría | Actor, fecha, valores anteriores/nuevos, motivo obligatorio y respaldo obligatorio de texto libre. | Definir límites de longitud del motivo y respaldo, y permisos de consulta del historial. |
| Revisión de configuración | Se registra una nueva versión; análisis futuros usan esa versión, los ya calculados permanecen iguales. | Definir cómo se selecciona una única versión ante cambios concurrentes mientras corre un análisis. |
| Pantalla | Está dentro de HU-03. | Confirmar junto con Edwin/PO si el historial será visible en la pantalla o solo auditable en almacenamiento/API. |

## Tareas propuestas

Distribución de puntos: **1 + 1 + 1 = 3 PH**. Las horas del Issue son estimaciones de esfuerzo, no puntos de historia.

| Número de tarea | Número de historia | Nombre | Descripción | Puntos de historia |
| --- | --- | --- | --- | ---: |
| T56 | HU-03 | Definir contrato y persistencia versionada | Acordar límites restantes, contrato de parámetros, motivos/respaldo, auditoría y versión aplicada a cada análisis. Coordinar revisión de HU-15/HU-16 con Edwin. | 1 PH |
| T57 | HU-03 | Integrar configuración con las reglas HU-14/15/16 | Tras aprobar la spec, implementar persistencia/versionado y hacer que el motor consuma los parámetros actuales, sin cambiar reglas existentes. Mantener análisis históricos intactos y cubrir regresiones de porcentajes, antigüedad y mínimos. | 1 PH |
| T58 | HU-03 | Implementar API, auditoría y pantalla de configuración | Exponer consulta/actualización al Coordinador, validar límites, guardar motivo y respaldo, documentar OpenAPI e implementar la pantalla acordada. Verificar permisos e integración. | 1 PH |
| **Total** | **HU-03** |  | **Suma de tareas** | **3 PH** |

## Fuera de alcance

- Cambiar por inferencia la fórmula de HU-14, los mínimos existentes de HU-16 o la regla externa de cero cambios previos.
- Afirmar que diez años representa una norma institucional definitiva; es el valor inicial acordado para el producto y podrá configurarse por Coordinador.
- Modificar o recalcular análisis ya generados cuando cambie la configuración.
- Implementar cálculos previos con parámetros nuevos o recálculo en lote.
- Implementar historias adicionales de solicitudes, informes o administración de usuarios.

## Secuencia de trabajo

1. Edwin revisa la lista de campos HU-16 y los detalles técnicos pendientes indicados arriba.
2. Shirley confirma criterios finales y detalles de experiencia de la pantalla.
3. Se actualiza la spec con el resultado de la revisión.
4. Una vez acordada, el PR draft puede marcarse listo y comienza la implementación con pruebas primero.
5. La revisión cruzada, CI, prueba integrada y aceptación de la PO se registran como evidencia separada.

La ficha no afirma que la historia esté implementada ni aceptada.
