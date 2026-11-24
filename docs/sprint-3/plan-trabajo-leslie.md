# Sprint 3 — Continuación del trabajo de Leslie

Consulta del remoto: 21/11/2026, zona America/Guayaquil. Base de HU-07: `843bf4f`. Este registro distingue código integrado, propuestas y aceptación; no sustituye la matriz de pruebas de Shirley.

## Estado comprobado

| Historia | Estado observado | Siguiente trabajo de Leslie |
| --- | --- | --- |
| HU-07, Issue #68, T58/T59 | Sin implementación en develop; en esta rama se implementó Catálogo/T59; pruebas Catálogo 147, Gateway 149 y frontend 178 correctas; CA3 implementado y probado automáticamente, con revisión humana pendiente | Revisar [evidencia](evidencia-hu-07-t58-t59.md) y [contrato pendiente](propuesta-integracion-hu-07-analisis-informe.md); revisar instantáneas con Pablo/Edwin y completar aceptación |
| HU-09, Issue #69, T60/T61 | Sin implementación publicada de la consulta entre carreras; existen motor HU-14 y correspondencias revisadas | Preparar contrato después de confirmar criterios; reutilizar motor y lecturas, sin IA ni escrituras |
| HU-21, Issue #75 | PR [#91](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91) integrada en develop; incluye correcciones posteriores a `7740a69` | Conservar implementación; regresión en integración y aceptación de PO documentada por separado |
| HU-27, Issue #65, T66/T67 | Draft PR [#95](https://github.com/PabloToapanta/ProyectoHomologacion/pull/95) de Pablo; plan T65 en PR [#94](https://github.com/PabloToapanta/ProyectoHomologacion/pull/94) | Leslie: caso `frontend/e2e/hu27-flujo-completo.spec.ts` sobre el arnés de Pablo; no duplicar arnés ni workflow |

HU-02 y HU-03 corresponden a Pablo. HU-32/33 siguen en PR #86/#89 al consultar; el E2E completo de remisión/finalización depende de su integración. No se dará por comprobado ese flujo mediante mocks de sus endpoints de negocio.

## Secuencia propuesta

1. HU-07: Leslie confirmó aprobación de Shirley de los CA1–CA4 del Issue #68. Revisar las precisiones técnicas sobre vigencia, asignaturas y código por versión. Preparar respaldo/restauración antes de una eventual migración persistente; desarrollo y pruebas sobre SQL aislado.
2. Acordar con Pablo los cambios de Gateway, DI y montaje React; con Edwin, metadatos de malla para HU-22 e informe. Propuesta concreta en la spec de HU-07; no se ha enviado comunicación externa en este trabajo.
3. Implementar T58 y T59, con pruebas fallidas de reglas primero; verificar datos históricos, concurrencia, seguridad y trazabilidad. No crear migraciones destructivas ni copiar programas automáticamente.
4. HU-09: Leslie confirmó aprobación de Shirley de los CA1–CA4 del Issue #69. Preparar una lectura autenticada para Docente/Coordinador entre dos carreras FISEI: tres porcentajes y veredicto del motor existente; correspondencia ausente u obsoleta produce información incompleta. Nunca generar correspondencias ni invocar IA. Precisar selección de mallas con HU-07 y paginación/límites técnicos antes de implementar; no descartar pares incompletos.
5. HU-27: seguir la propuesta de Pablo en #95. Su arnés aporta URL, credenciales efímeras e identificadores por variables `E2E_*`; Leslie crea solicitud, asigna docentes, analiza, revisa y comprueba informe/remisión/finalización desde UI. Revisión de selectores y contratos conjunta; prueba final sobre HU-32/33 integradas.
6. Ejecutar comprobaciones y registrar resultados reales; revisión cruzada por Edwin y aceptación por Shirley. Una PR integrada no equivale por sí sola a aceptación funcional.

## Estado de este incremento

Se mantienen sin publicar los cambios de HU-07 sobre `843bf4f`. Catálogo incorpora entidad/casos de uso/repositorio, migración nueva, API, DI, Gateway y panel dentro de AsignaturasPage; no se necesitó modificar App.tsx ni navegación. Leslie transmitió el acuerdo técnico de Pablo para continuar. Los contratos de análisis/informe/HU-22 quedan concretados en una propuesta para revisión conjunta antes de integrarlos.

Resultado: **147 pruebas de Catálogo con SQL aislado, 149 de Gateway y 178 de frontend correctas**; tipos, compilación y formato comprobados. Ver detalles y límites en la evidencia. No se modificaron bases persistentes ni módulos de Pablo/Edwin en Homologación; CA3 sigue parcial. Leslie autorizó preparar commits atómicos en español y publicar la rama de HU-07. El nuevo develop `715b5a1` queda pendiente de conciliación e integración; las pruebas registradas corresponden a la base `843bf4f`. No se autorizaron merges ni aceptación funcional; Shirley conserva esa aceptación.

## Incremento CA3 tras el acuerdo de Pablo

Leslie autorizó incorporar develop únicamente en HU-07; merge local 388a0db sobre 1542986. Se implementó la captura de versiones en análisis internos, externos, agrupaciones y salidas por RN-07; el informe y HU-22 consumen los datos guardados. Históricos sin versión y origen externo sin información autorizada conservan null. Ver [evidencia CA3](evidencia-hu-07-ca3.md): 1396 pruebas correctas, 1 E2E completo con Chromium/Compose correcto, 6 omisiones explícitas, tipos y Vite correctos. Revisión cruzada, aceptación e integración de la PR siguen pendientes; no se acepta el trabajo propio.
