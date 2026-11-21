# HU-03 — Configuración de umbral, años máximos y mínimos

**Issue:** [#67 — HU-03](https://github.com/PabloToapanta/ProyectoHomologacion/issues/67)  
**Estado:** implementación técnica de T56–T58 en la rama de HU-03; pendiente revisión cruzada y aceptación funcional.
**Sprint:** 3 (planificación del Issue: 5–25 de noviembre de 2026).  
**Responsable:** Pablo Toapanta.  
**Revisión cruzada técnica:** Edwin Caraguay. **Aceptación funcional:** Shirley Amaguaña (PO).

## Historia de usuario

| Dato | Definición |
| --- | --- |
| Número de historia | HU-03 |
| Usuario | Coordinador |
| Prioridad | Media (Issue #67) |
| Riesgo | Alto: la configuración afecta decisiones del motor y reglas compartidas HU-14/15/16 |
| Nombre | Configuración de umbral, años máximos y mínimos |
| Puntos de historia | 3 PH |
| Responsable | Pablo Toapanta |
| Descripción | Como coordinador, quiero consultar y configurar los parámetros académicos vigentes de equivalencia, antigüedad y requisitos mínimos de cambio, para que los análisis nuevos usen reglas explícitas y trazables. |

## Criterios de aceptación

### CA1 — Valores iniciales

- **Given** el sistema se inicia con su configuración predeterminada.
- **When** se consulta la configuración o se ejecuta una evaluación de equivalencia, antigüedad o requisitos de cambio.
- **Then** el umbral es 80 %, la antigüedad máxima es 10 años y los mínimos conservan los valores actuales: cambio interno, 1 período y 1 asignatura homologable; cambio externo, 2 períodos y 2 asignaturas homologables, además de cero cambios previos.

### CA2 — Consulta, autorización y rangos

- **Given** un Coordinador autenticado consulta la configuración vigente.
- **When** el servicio autoriza la petición.
- **Then** devuelve los parámetros actuales.
- **Given** un Coordinador autenticado modifica uno o más parámetros válidos e ingresa motivo y respaldo.
- **When** el servicio valida y persiste el cambio.
- **Then** guarda la nueva configuración y el registro de auditoría.
- **Given** una petición de consulta o modificación llega sin sesión válida o con un rol distinto de Coordinador.
- **When** el servicio autoriza la petición.
- **Then** responde 401 sin sesión válida y 403 para roles no autorizados.
- **Given** la modificación incluye un umbral que no sea un entero entre 1 y 100, años fuera del rango entero 1–50, mínimos fuera del rango entero 1–50, motivo vacío o mayor de 500 caracteres, o respaldo vacío o mayor de 2.000 caracteres.
- **When** el servicio valida la petición.
- **Then** rechaza todo el cambio, devuelve un error de validación y conserva la configuración vigente sin guardar parcialmente.

### CA3 — Aplicación de versión y análisis existentes

- **Given** una evaluación empieza mientras una versión determinada de parámetros es vigente.
- **When** el motor inicia el cálculo.
- **Then** toma una fotografía atómica de esa versión y la usa íntegramente durante el análisis, guardando su identificador/valores junto al resultado.
- **Given** el Coordinador cambia parámetros mientras una evaluación ya está en curso.
- **When** esa evaluación termina.
- **Then** conserva la versión tomada al inicio; las evaluaciones que comiencen después del cambio usan la nueva versión.
- **Given** existen análisis calculados con una configuración anterior.
- **When** se guarda una nueva configuración.
- **Then** los análisis anteriores no se recalculan ni se modifican; HU-03 no permite cálculos previos o recálculos con la nueva configuración.

### CA4 — Auditoría e historial visible

- **Given** un Coordinador realiza un cambio válido.
- **When** el sistema persiste la nueva configuración.
- **Then** registra actor, fecha y hora, valores anteriores y nuevos, motivo obligatorio ingresado por el Coordinador (máximo 500 caracteres) y respaldo obligatorio ingresado por el Coordinador como texto libre (máximo 2.000 caracteres).
- **Given** un Coordinador abre el historial desde la pantalla de configuración.
- **When** consulta los cambios registrados.
- **Then** ve entradas de solo lectura con actor, fecha y hora, valores anteriores y nuevos, motivo y respaldo.

### CA5 — Mínimos configurables HU-16

- **Given** el Coordinador modifica los campos existentes de períodos mínimos y asignaturas homologables mínimas para cambio interno y externo.
- **When** guarda valores enteros válidos entre 1 y 50.
- **Then** el sistema aplica cada mínimo al tipo de cambio correspondiente; los valores iniciales siguen siendo internos 1/1 y externos 2/2.
- **Given** se valida un cambio externo.
- **When** el sistema evalúa los requisitos vigentes.
- **Then** mantiene la regla fija de cero cambios previos, que no es editable en HU-03.

### CA6 — Pantalla, OpenAPI y verificación

- **Given** un Coordinador autenticado abre la pantalla de configuración.
- **When** consulta o cambia parámetros.
- **Then** puede ver los valores vigentes, editar los campos acordados, ingresar motivo y respaldo obligatorios, recibir confirmación o errores de validación y consultar el historial de solo lectura.
- **Given** se ejecuta la verificación de HU-03.
- **When** corren pruebas unitarias, API, autorización, interfaz e integración/regresión.
- **Then** se comprueban valores iniciales, límites, auditoría, lectura del historial, fotografía de versión, aplicación a análisis futuros y conservación de resultados existentes; OpenAPI documenta los endpoints.

## Alcance acordado

- HU-03 vuelve configurables únicamente los campos existentes: umbral HU-14, años máximos HU-15 y períodos/asignaturas homologables mínimos de HU-16. No añade reglas nuevas.
- Valores iniciales: umbral 80 %, antigüedad 10 años, mínimos internos 1 período/1 asignatura homologable y externos 2 períodos/2 asignaturas homologables.
- El umbral es un porcentaje entero de 1 a 100 inclusive. Años y mínimos son enteros positivos hasta 50 inclusive.
- Para cambio externo permanece fija la condición actual de cero cambios previos; no se configura por tipo.
- El Coordinador consulta y modifica la configuración. Cada cambio requiere motivo y respaldo obligatorios, ambos ingresados por el Coordinador. Motivo: texto de hasta 500 caracteres; respaldo: texto libre de hasta 2.000 caracteres.
- La auditoría registra actor, fecha y hora, valores anteriores/nuevos, motivo y respaldo. El historial se muestra en la pantalla de configuración para Coordinadores, en modo de solo lectura.
- Cada análisis toma atómicamente una fotografía de la configuración vigente al comenzar. Un cambio concurrente no altera análisis en curso; los siguientes usan la nueva versión. Cada resultado conserva la versión/valores usados.
- Cambiar la configuración no modifica análisis ya calculados. No hay cálculo previo ni recálculo de anteriores con parámetros nuevos.
- La historia incluye API, persistencia/versionado, integración con reglas, auditoría, pantalla y documentación OpenAPI.

## Tareas propuestas

Distribución: **1 + 1 + 1 = 3 PH**.

| Número de tarea | Número de historia | Nombre | Descripción | Puntos de historia |
| --- | --- | --- | --- | ---: |
| T56 | HU-03 | Definir contrato y persistencia versionada | Traducir esta especificación acordada a contrato de parámetros/API, persistencia y fotografía de versión para análisis. Revisar con Edwin la integración conservadora con las reglas HU-15/HU-16. | 1 PH |
| T57 | HU-03 | Integrar configuración con las reglas HU-14/15/16 | Implementar almacén/versionado y hacer que el motor consuma los parámetros vigentes en cada inicio de evaluación, sin cambiar reglas fuera de los campos configurables. Mantener intactos resultados históricos y cubrir regresión. | 1 PH |
| T58 | HU-03 | Implementar API, auditoría y pantalla de configuración | Exponer consulta/actualización solo a Coordinador, validar rangos y textos obligatorios, persistir auditoría, mostrar historial de solo lectura, implementar interfaz y documentar OpenAPI; verificar integración. | 1 PH |
| **Total** | **HU-03** |  |  | **3 PH** |

## Verificación técnica de T57 (21/11/2026)

La integración de T57 conserva la versión vigente al inicio de cada análisis interno o externo y la incluye en la evaluación serializada del resultado. El umbral, la antigüedad y los mínimos de cambio usan esa fotografía durante la operación. Los requisitos externos solo se declaran cumplidos cuando hay suficientes análisis completos homologables; si aún faltan análisis y el mínimo no está demostrado, el estado es `Incompleto`.

La migración crea la configuración versionada y el puntero vigente con clave foránea, `rowversion` y semilla 80/10/1/1/2/2. Las pruebas verifican la correspondencia del snapshot EF, el SQL generado, la persistencia y deserialización de la versión, y que mover el puntero vigente no cambia un resultado guardado. La migración se aplicó también a una base temporal de SQL Server aislada durante la verificación de T58. Esto no declara aceptada HU-03.

## Verificación técnica de T58 (21/11/2026)

- `GET /api/homologaciones/configuracion` devuelve los parámetros vigentes y un ETag fuerte de la versión. `PUT` exige `If-Match`, el conjunto completo de seis parámetros, motivo y respaldo; rechaza campos desconocidos y valores fuera de rango. Un ETag obsoleto produce 409 sin guardar parcialmente.
- Cada cambio registra actor del JWT, fecha/hora del servidor, motivo, respaldo y vínculo con la versión anterior. La nueva versión y el cambio condicional del puntero ocurren en una transacción. `GET /api/homologaciones/configuracion/historial` devuelve entradas de solo lectura, valores anteriores/nuevos y paginación por cursor (50 por defecto, máximo 100).
- API y Gateway restringen estas tres operaciones al rol Coordinador. La pantalla permite editar, consultar historial y recargar la versión vigente tras un conflicto.
- Las migraciones T57/T58, la actualización, la auditoría, la paginación y dos escrituras concurrentes se probaron en SQL Server 2022 aislado en Docker. Última ejecución: Application 133/133, API 139/139, Infrastructure 61 aprobadas y 1 omitida (prueba ajena que requiere Ollama), Gateway 142/142 y frontend 162/162. Las compilaciones .NET y React pasaron; .NET reportó 0 advertencias y 0 errores. No se declara aceptación funcional de la PO.

## Fuera de alcance

- Cambiar la fórmula de HU-14, agregar parámetros nuevos o cambiar la regla fija de cero cambios previos externos.
- Presentar los 10 años como norma institucional definitiva; es el valor inicial acordado para el producto y es configurable por Coordinador.
- Modificar, recalcular o generar de antemano resultados de análisis con una configuración recién cambiada.
- Implementar cambios de solicitudes, informes u otras historias.

## Secuencia de trabajo

1. Implementar TDD y contrato de la API según los criterios acordados.
2. Coordinar con Edwin durante cambios a las reglas compartidas HU-15/HU-16 y completar revisión cruzada.
3. Ejecutar CI, regresión e integración; registrar evidencia.
4. Shirley realiza la aceptación funcional. La aprobación de esta spec no equivale a aceptación de la historia.

La implementación técnica de HU-03 está en la rama; su revisión cruzada y aceptación funcional siguen el proceso del proyecto.
