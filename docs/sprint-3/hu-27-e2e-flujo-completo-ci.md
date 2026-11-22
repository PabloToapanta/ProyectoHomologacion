# HU-27 — Plan de pruebas, E2E del flujo completo y CI

**Issue:** [#65](https://github.com/PabloToapanta/ProyectoHomologacion/issues/65) · **Sprint:** 3 · **Prioridad:** alta · **Backlog:** 8 PH.
**Estado:** propuesta de spec para revisión de Pablo, Leslie, Edwin y Shirley. No acredita implementación ni aceptación.
**Base:** `develop` del 21/11/2026 (`843bf4f`); rama `feature/hu-27-pruebas-e2e` hacia `develop` mediante Draft PR.

## Historia de usuario

Como equipo, queremos relacionar los criterios de las historias entregadas con pruebas y ejecutar un recorrido E2E del trámite en CI, para detectar fallos de integración antes de aceptar un incremento.

## Alcance y dependencias

- T65 cubre **solo historias efectivamente entregadas**: cada fila de la matriz indica HU, criterio, prueba automatizada o manual, ubicación de evidencia, resultado y responsable. Un criterio sin prueba o pendiente se marca como tal; no se inventa cobertura.
- T66 recorre por la **interfaz React en Chromium**, a través de Gateway y servicios reales con SQL Server: login → solicitud interna sintética → asignación de dos docentes → análisis → dos revisiones → informe → remisión → revisión/finalización por coordinador. Las comprobaciones HTTP/SQL complementan el recorrido, sin sustituir la interacción de cada rol.
- T67 ejecuta integración y E2E en GitHub Actions sobre la PR hacia `develop`; un fallo deja el check rojo. El E2E se ejecuta sin Ollama real con una respuesta simulada y validada. Una prueba con modelo real, si se hace, queda como evidencia local separada.
- **Dependencia bloqueante para el último tramo:** HU-32 [#63](https://github.com/PabloToapanta/ProyectoHomologacion/issues/63) y HU-33 [#64](https://github.com/PabloToapanta/ProyectoHomologacion/issues/64) deben estar integradas y tener rutas/estados estables. En la base consultada todavía no aparecen remisión ni finalización; no se simulan sus endpoints para declarar CA2 cumplido.
- El camino principal usa dos docentes que coinciden para llegar a finalización. Las decisiones «Sin acuerdo» se cubren en las pruebas propias de HU-33 y se referencian en T65; no se omiten silenciosamente si se vuelven necesarias para el camino principal.

## Criterios de aceptación y evidencia requerida

| Criterio del Issue | Evidencia que debe quedar en la PR |
| --- | --- |
| **CA1 — matriz HU → criterio → prueba** | Matriz versionada para las HU entregadas, con enlaces a pruebas y evidencias manuales, estado y huecos explícitos. Shirley confirma cobertura; Edwin ayuda a contrastarla con los criterios. |
| **CA2 — flujo E2E completo** | Un caso Playwright termina en «Finalizada» y verifica estado persistido tras recarga. Usa identidades reales de administrador/coordinador/dos docentes, Gateway, servicios y SQL; guarda trace/capturas solo al fallar. Comprueba que remisión ocurrió antes de finalización. |
| **CA3 — integración y E2E en CI** | Workflow en la PR con jobs visibles para backend/integración y E2E; ejecución de la rama correcta; salida distinta de cero ante fallo; checks obligatorios configurados en protección de `develop` por quien administra el repositorio. |
| **CA4 — datos sintéticos y secretos** | Fixture temporal y determinista en estructura, con IDs/nombres sintéticos y credenciales aleatorias por ejecución fuera de Git; logs y artefactos saneados; limpieza de contenedores, volúmenes y archivos temporales. |

## Contrato de la prueba E2E

1. El arnés levanta un proyecto Docker Compose propio, espera salud de SQL, servicios, Gateway y frontend, y crea datos sintéticos por API o semilla de prueba explícita. Nunca usa bases ni `.env` existentes.
2. La prueba utiliza las acciones visibles de cada rol y aserciones semánticas estables (etiquetas, encabezados, estados). No usa tiempos fijos para ocultar carreras; espera respuestas o estados observables.
3. El análisis interno usa el doble HTTP de Ollama ya existente, sin credenciales de servicios ni evidencia inventada. El motor determinista conserva el veredicto; el doble solo suministra la explicación de prueba.
4. Tras generar el informe, el E2E comprueba sus dos revisiones y su identidad; después remite una sola vez, comprueba «Remitida» y el coordinador finaliza. Una segunda lectura tras recarga confirma «Finalizada» y solo lectura.
5. Si HU-32/33 aún no están integradas, el caso completo queda pendiente. Puede prepararse fixture o un tramo preliminar separado, pero no se presenta como E2E aprobado de CA2.
6. La evidencia de CI conserva TRX/resultado JSON y trace/capturas de fallos, sin tokens ni contraseñas. El arnés limpia recursos aun cuando una aserción falla.

## Tareas y reparto propuesto

| Orden | Tarea | Entregable | Responsable | Estimación del Issue |
| --- | --- | --- | --- | ---: |
| 1 | **T65** Plan de pruebas | Matriz de HU entregadas, criterios y pruebas, con huecos visibles | Shirley; Edwin contrasta | 3 h |
| 2 | **T66** E2E completo | Arnés/fixture aislado y caso Playwright de principio a fin | Pablo: arnés/semilla (3 h); Leslie: recorrido de interfaz (3 h) | 6 h |
| 3 | **T67** CI | Workflow que ejecuta integración y E2E, publica evidencia saneada y falla ante regresión | Pablo (3 h); Leslie revisa la ejecución | 3 h |

La estimación técnica del Issue suma **12 h** y los **8 PH** son del Product Backlog; no se convierten entre sí. El reparto se confirma con Leslie, Shirley y Edwin antes de codificar archivos compartidos. T66 depende de las rutas finales de HU-32/33; T67 puede prepararse, pero solo se declara verificado cuando ejecute el E2E completo.

## Fuera de alcance y definición de hecho

HU-27 prueba el flujo; no implementa remisión, finalización ni reglas académicas faltantes. No modifica casos de negocio para hacer pasar la prueba. La historia queda hecha cuando T65–T67 y CA1–CA4 están verificados, la PR tiene revisión cruzada de Edwin, CI verde con Docker Compose y Shirley acepta el incremento. No hacer merge ni release desde esta tarea.
