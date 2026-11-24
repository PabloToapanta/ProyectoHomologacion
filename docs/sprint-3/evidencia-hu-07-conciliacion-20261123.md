# HU-07 — Conciliación con develop del 23/11/2026

Refs [Issue #68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68), [PR #96](https://github.com/PabloToapanta/ProyectoHomologacion/pull/96). Leslie solicitó resolver los conflictos. Se incorporó develop `6842f94` en `feature/hu-07-versiones-malla`, partiendo de `d406301`. No se integró la PR en develop.

## Resolución

Los siete conflictos provenían del solapamiento entre las instantáneas de Pablo, ya presentes en develop, y CA3 de la PR #96. Se conserva una sola implementación canónica: `InstantaneasMalla.cs`, con `MallaParaAnalisis`, `InstantaneaMalla`, `MallaAsignaturaAnalizada` y `MallasAnalisis`. Se elimina el archivo duplicado `MallasAnalisis.cs` de nuestra rama, sin eliminar pruebas.

Se mantienen las validaciones adicionales de HU-07: nombre con máximo técnico de 100 y vigencia explícita cuando existe un objeto de malla. Un valor ausente no se convierte en no vigente. Se preservan captura interna/externa/grupal, RN-07, huellas y contexto del agente de HU-37, reservas, ETag, cierre del informe, UI y alerta HU-22 basada en las instantáneas guardadas.

El orden de parámetros del tipo de Pablo es Catálogo/aprobada; los fixtures propios se ajustaron a ese orden. **El JSON mantiene sus nombres de campos**. Una prueba nueva lee el formato publicado previamente, donde aprobada aparecía primero, y comprueba que ambos identificadores y la vigencia se recuperan correctamente. No se añade migración SQL ni se reescriben datos históricos.

También se conservaron las correcciones de impresión de HU-24: clon del informe completo fuera del diálogo y limpieza después de imprimir. La vista conserva las mallas guardadas dentro del contenido imprimible.

## Fallos reproducidos y corregidos

- La primera comprobación de Application pasó 249 casos. La prueba adicional de compatibilidad del JSON elevó el resultado final a 250.
- La combinación automática del fixture HTTP dejaba dos preparaciones de malla y la segunda sobrescribía los UUID de la primera. Se ejecutó la prueba y falló por identificador inesperado. Se unificó la preparación y se conservaron todas las aserciones de ambos desarrolladores, incluida la lectura del último análisis y el cierre.
- La nueva prueba JSON tuvo inicialmente un error de compilación CS9007 por llaves de un literal interpolado. Se corrigió el fixture y se ejecutó completa la suite Application. No se cambiaron reglas productivas ni se retiraron aserciones.

## Verificación final

| Suite | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Catálogo con SQL Server aislado | 148 | 0 | 0 |
| Homologación Infrastructure con SQL real | 108 | 0 | 1 |
| Homologación Application | 250 | 0 | 0 |
| Homologación API | 161 | 0 | 0 |
| Homologación Domain | 115 | 0 | 0 |
| Auth Application | 13 | 0 | 0 |
| AuthService | 79 | 0 | 0 |
| Gateway | 156 | 0 | 0 |
| Solicitudes | 169 | 0 | 5 |
| Frontend, 33 archivos | 216 | 0 | 0 |
| **Total de suites, sin duplicar ejecuciones** | **1415** | **0** | **6** |

Además: **1 E2E completo correcto** en Chromium con servicios reales de Docker Compose y agente simulado: login, solicitud, asignación, análisis, revisiones, informe, remisión, finalización y consulta persistida.

Omisiones: el caso opcional de Ollama real y cinco pruebas SQL de Solicitudes que requieren HU10_TEST_SQL; no se contabilizan como verificadas. Los avisos XML y xUnit preexistentes y los avisos scrollTo de jsdom no se ocultaron. No se modificaron timeouts.

Comandos: `python tests/security/hu07-sql.py --homologacion`; suite Application repetida después de corregir el fixture; suite Solicitudes ejecutada por separado porque el arnés se detuvo ante aquel error de compilación. Los demás proyectos de la solución habían pasado. TypeScript, Vite y formato de los archivos conciliados: correctos.

Para E2E se reutilizó el arnés de Pablo con el adaptador local de CLI Node/UTF-8 descrito en la [evidencia CA3](evidencia-hu-07-ca3.md), manteniendo caso y timeouts. Logs: `TestResults/hu07-conciliacion-*`, resultados de SQL en `TestResults/hu07-ca3-sql/`. La evidencia de la ejecución previa se conservó en `TestResults/hu07-conciliacion-baseline/`. No se tocaron bases persistentes del equipo.

Pendientes: revisión del incremento conciliado por el equipo, CI remoto y aceptación funcional de Shirley. Resolver los conflictos no equivale a aprobar ni integrar la PR.
