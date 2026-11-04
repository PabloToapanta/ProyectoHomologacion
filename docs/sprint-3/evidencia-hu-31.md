# HU-31 — análisis propio del docente junto al del agente

Responsable: Edwin Caraguay. Rama `feature/hu-31-revision-docente` sobre `feature/hu-19-analisis-externo`
(`3b2e291`), porque necesita el análisis externo de HU-19. Fecha: 04/11/2026. Historia del Sprint 3
adelantada con autorización de Shirley, transmitida por Edwin. Contrato: [contrato-hu-31.md](contrato-hu-31.md).
Issue pendiente de crear en GitHub: borrador en [issue-hu-31.md](issue-hu-31.md).

## Cambios

- **Dominio:** `ReglaRevisionDocente` aplica las reglas.
  - «De acuerdo» solo vale sobre un análisis completo del agente con sugerencia, y guarda esa sugerencia. Una decisión distinta enviada por el cliente se rechaza.
  - «Ajuste» exige decisión (`Homologable` o `NoHomologable`) y una observación de 1 a 2000 caracteres.
- **Application:** `RegistrarRevisionDocente` hace las comprobaciones y el guardado.
  - Comprueba en Solicitudes la asignación del docente del token, que la aprobada pertenezca a la solicitud y que la receptora sea de su carrera.
  - Lee el último análisis del agente de esa pareja: interno (HU-18) o externo (HU-19), según el tipo de la solicitud.
  - Guarda la fotografía del análisis del agente (estado y sugerencia), la fecha y el docente.
- **Infrastructure:** tabla `RevisionesDocentes` con clave (solicitud, aprobada, receptora, docente) y la migración `20261104000300_RevisionesDocentesHu31`. El registro propio se reemplaza al corregir; un fallo de la base da 503.
- **API:** `PUT` y `GET /api/homologaciones/solicitudes/{id}/revisiones-docentes`, solo para el rol Docente.
  - La identidad sale del `sub` del JWT. Un campo `docenteId` en el cuerpo da 400.
  - Una solicitud no asignada al docente da 404.
- **Gateway:** ruta con `AcademicAccess`. La prueba de OpenAPI pasa de 28 a 29 rutas.
- **Frontend:** `RevisionDocentePanel` («Mi análisis») dentro del panel de análisis, para la pareja seleccionada.
  - «De acuerdo» solo está habilitado si el agente dio una sugerencia.
  - «Ajuste» pide la decisión y la observación.
  - Debajo se listan los análisis de ambos docentes.
- Ajuste a una prueba de HU-19: ahora comprueba que las dos primeras migraciones sean las de HU-20 y HU-19, en ese orden, porque HU-31 añade una tercera.

## Criterios propuestos

| Criterio | Evidencia | Estado |
|---|---|---|
| CA1 — «De acuerdo» guarda la sugerencia del agente | Regla, caso de uso y API con análisis completo | Verificado en pruebas (sin modelo real) |
| CA2 — «Ajuste» con decisión y observación, con o sin agente | Regla, caso de uso, API y smoke externo | Verificado |
| CA3 — rechazos sin guardar | Regla, caso de uso, API y smoke (400) | Verificado |
| CA4 — dos docentes y corrección propia | Caso de uso, API, SQLite, SQL Server real y smoke | Verificado |
| CA5 — no asignado, otros roles y suplantación | Caso de uso, API y smoke (404/403/401/400) | Verificado |
| CA6 — persistencia y fallo controlado | SQL Server real y smoke (reinicio y fallo forzado con trigger) | Ver ejecuciones |

## Ejecuciones del 04/11/2026

TDD: la fase roja se observó en Infrastructure (compilación), API (6 pruebas fallidas) y frontend (módulo
inexistente). En Application las pruebas se escribieron antes que el código, pero no se ejecutaron en rojo por separado.

1. `Homologacion.sln`: Domain 71, Application 101, API 128 e Infrastructure 45 (con 3 omitidas sin SQL), todas aprobadas.
2. `Homologacion.Infrastructure.Tests` con SQL Server 2022 temporal: 47 aprobadas, 1 omitida (Ollama real).
3. Gateway: 119/119. Frontend: 131/131 y `npm run build`.
4. `tests/security/hu31-smoke.py` sobre una copia limpia de la rama, con Docker Compose, Gateway, JWT cifrado
   y SQL Server real: **32/32** (14 de preparación del flujo externo y 18 de HU-31).
   - Comprueba 401, 403 para el Coordinador y 404 para un docente ajeno.
   - Rechaza «De acuerdo» sin sugerencia, «Ajuste» sin observación y un `docenteId` en el cuerpo.
   - Cubre el ajuste de cada docente, la corrección del propio, que ambos se ven, los datos conservados tras reiniciar Homologación y el 503 ante un fallo forzado con trigger, sin reemplazar el registro.
   - Proyecto aleatorio con volúmenes eliminados. HTTP local: no acredita HTTPS.

**No verificado:** «De acuerdo» de punta a punta con análisis completo (requiere el modelo o el doble HTTP de HU-18),
prueba en navegador, CI en GitHub, revisión cruzada y aceptación de Shirley.
