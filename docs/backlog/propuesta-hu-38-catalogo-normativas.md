# HU-38 · Catálogo de normativas — propuesta para el Product Backlog

**Estado:** propuesta. La Product Owner (Shirley) no la ha aprobado y no forma parte del Sprint 3. No hay Issue ni código.
**Fecha:** 25/11/2026 · **Propone:** Edwin.
**Relacionada con:** HU-16 (requisitos de cambio interno y externo) y, de forma indirecta, con HU-03, HU-15, HU-17 y HU-22.

## Historia de usuario

Como **administrador**, quiero registrar las normativas institucionales que respaldan las reglas de homologación, con su vigencia y su documento, para que **coordinadores y docentes** sepan qué norma rige y si sigue vigente.

## Motivo

- Las reglas del sistema citan el instructivo UTA RES-CAU-P-005-2023 y el Reglamento de Régimen Académico del CES (`docs/sprint-3/decisiones-validacion-ingeniero.md`). El sistema no guarda esas fuentes ni su vigencia.
- El equipo tiene dos documentos de 2026, que todavía no se han revisado:
  - la reforma del instructivo de cambios de carrera (RES-CAU-P-011-2026);
  - el instructivo de homologación de DIRENI (RES-CAU-P-122-2026).
- Hoy no hay dónde registrar que una norma fue reformada, ni qué reglas del proyecto dependen de ella.
- Se propone como historia nueva y no como ampliación de HU-16:
  - HU-16 ya está entregada y aceptada con sus cuatro criterios (Sprint 1);
  - el catálogo sirve también a otras reglas, no solo a los mínimos de cambio.

## Alcance propuesto

- **Registro de cada normativa:**
  - código de resolución;
  - título;
  - órgano emisor;
  - fecha de emisión;
  - vigente desde y, si aplica, hasta;
  - estado: Vigente, Reformada o Derogada;
  - si es una reforma, la normativa que reforma;
  - el PDF oficial.
- **Consulta:** lista con filtro por estado y detalle con descarga del PDF. La consultan Administrador, Coordinador y Docente; solo el Administrador registra y modifica.
- **Historial:** los cambios de estado o de vigencia quedan registrados con fecha y usuario. Una normativa no se elimina; se marca como Derogada.
- **Vínculo con las reglas (lectura):** cada regla de análisis del proyecto muestra qué normativa cita, o «sin normativa registrada». Las reglas son HU-15 antigüedad, HU-16 mínimos, HU-17 RN-07 y HU-22 ruta alternativa.

## Fuera de alcance

- Cambiar automáticamente parámetros o reglas cuando se registra o reforma una normativa. Los cambios de reglas siguen el proceso de HU-03 y requieren aprobación.
- Que el agente o el motor lean o interpreten los PDF. Ninguna decisión se delega al agente.
- Cargar normativas en la base desde el repositorio. Los PDF se suben desde la aplicación y no se versionan en Git, igual que los adjuntos de HU-12.

## Criterios de aceptación propuestos

*Propuestos por el equipo; la Product Owner los confirma o ajusta antes de codificar.*

- [ ] **CA1.** El Administrador registra una normativa con sus datos obligatorios y su PDF. Si falta un dato obligatorio, el archivo no es PDF o supera el tamaño permitido, el sistema lo rechaza con un mensaje claro.
- [ ] **CA2.** El código de resolución es único. Una reforma referencia a la normativa que modifica, y la normativa reformada muestra sus reformas.
- [ ] **CA3.** Coordinador y Docente consultan la lista y el detalle, filtran por estado y descargan el PDF. No pueden crear ni modificar.
- [ ] **CA4.** Los cambios de estado o de vigencia se registran con fecha y usuario. Las normativas no se eliminan.
- [ ] **CA5.** Cada regla de análisis del proyecto muestra la normativa que cita, o «sin normativa registrada», sin alterar su cálculo.
- [ ] **CA6.** Sin sesión, 401. Un rol sin permiso para registrar o modificar, 403. Endpoints documentados en Swagger.

## Tareas y estimación (orientativa)

| Tarea | Horas |
| --- | --- |
| Dominio, persistencia y API del catálogo de normativas (servicio Catálogo), con TDD | 5 |
| Almacenamiento y descarga del PDF, reutilizando las reglas de HU-12 (tipo, tamaño, huella) | 3 |
| Pantalla de administración y consulta en el frontend | 4 |
| Vínculo de lectura entre reglas y normativa (CA5) | 3 |
| Rutas en el Gateway (HTTP y HTTPS), pruebas de integración y evidencia | 2 |
| **Total** | **17** |

## Decisiones pendientes de la Product Owner

1. ¿Se aprueba como HU-38 y en qué sprint entra?
2. ¿Quién registra normativas: solo el Administrador o también el Coordinador?
3. ¿Se incluye CA5 en esta historia o va en una historia aparte?
4. ¿Quién revisa los instructivos de 2026 frente a las reglas actuales? La revisión podría cambiar valores como los 10 años de antigüedad (el instructivo de 2023 menciona 5). Es una consulta académica, no una tarea de esta historia, y su resultado se aplicaría por HU-03.

## Responsables

Por asignar por la Product Owner. La revisión cruzada sigue la regla del equipo: nadie acepta sus propios cambios.
