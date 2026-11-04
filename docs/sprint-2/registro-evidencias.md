# Registro del Sprint 2

Planificación vinculada al [Issue #8](https://github.com/PabloToapanta/ProyectoHomologacion/issues/8).
Este registro no acredita implementación, pruebas ni aceptación. Completar solo con datos reales.

| HU | Pareja prevista | Issue específico | Rama / PR | Pruebas y evidencias | PO acepta |
| --- | --- | --- | --- | --- | --- |
| HU-34 HTTPS | Pablo / Leslie | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-35 JWT cifrado | Pablo / Leslie | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-36 Clave interna | Pablo / Leslie | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-18 Agente interno | Pablo / Leslie | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-10 Solicitudes | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-11 Aprobadas | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-29 Dos docentes | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-30 Vista docente | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-19 Agente externo | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-20 Persistencia | Edwin / Shirley | Por crear o enlazar | Pendiente | Pendiente | Pendiente |
| HU-08 Carga masiva | Por asignar | [#50](https://github.com/PabloToapanta/ProyectoHomologacion/issues/50) | Pendiente | [Propuesta de contrato](propuesta-contrato-hu-08.md), sin implementación | Pendiente |

## Seguimiento de jornadas

| Fecha | Realizado y evidencia | Siguiente paso | Bloqueo | Responsable |
| --- | --- | --- | --- | --- |
| 22/10/2026 | Por registrar | | | |
| 23/10/2026 | Por registrar | | | |
| 26/10/2026 | Por registrar | | | |
| 27/10/2026 | Por registrar | | | |
| 28/10/2026 | Por registrar | | | |
| 29/10/2026 | Por registrar | | | |
| 30/10/2026 | Por registrar | | | |
| 04/11/2026 | Por registrar | | | |

## Sprint Review y retrospectiva

- Fecha prevista: 04/11/2026. Horario y asistentes por confirmar.
- Historias demostradas y aceptación de Shirley: pendiente.
- Observaciones del docente: pendiente.
- Qué funcionó, dificultades y acciones de mejora con responsable: pendiente.
- Horas reales: registrar por tarea sin sustituirlas por estimaciones.

## Condiciones para comenzar cada implementación

1. Issue y criterios verificables confirmados por PO; las fichas nuevas de seguridad están en la planificación.
2. Contratos acordados antes de tocar arranques, Gateway, DTO compartidos o APIs de otro responsable.
3. Prueba fallida, implementación y prueba correcta para reglas/validaciones; evidencia sin secretos.
4. PR a develop, revisión cruzada, CI y prueba integrada antes de aceptación.

No convertir propuestas de recorte del Sprint 3 ni apoyo en T37 en decisiones ya aceptadas.

## HU-36 — avance local del 03/11/2026

Leslie inició Issue #37 en `feature/hu-36-clave-interna`, desde develop `e44b939`,
en un worktree separado para preservar la documentación local anterior. Se abrió
la propuesta de contrato antes de editar los puntos compartidos. Auth, Catálogo y
Homologación validan la clave; Gateway la incorpora; Compose cierra puertos internos.
TDD registrado: 28 fallos iniciales; resultado final Release: 411 pruebas correctas.
Build de ambas soluciones sin errores/advertencias. Ver
[evidencia y pendientes](evidencia-hu-36.md) y
[contrato de integración](hu-36-clave-interna.md).

Solicitudes (PR #45), combinación con HTTPS/JWE (PR #47/#49), CI, revisión cruzada
y aceptación de Shirley siguen pendientes. Sin commits, push ni merge. No se declara
HU-36 terminada ni se inicia HU-18 en este incremento.

### Ampliación HU-36 con HU-10 integrada

Tras actualizar la base a develop `e01767b`, se añadió la guarda a Solicitudes y
la clave privada a su cliente HTTP hacia Catálogo, conservando Bearer/roles.
Otras 11 pruebas fallidas registradas antes de implementar. Regresión final:
449 correctas y una prueba SQL opcional de HU-10 omitida por ausencia de conexión
aislada HU10_TEST_SQL. La comprobación Docker con SQL Server real sí pasó 52 casos,
incluidos Coordinador, creación/consulta de solicitud sintética, llamada entre
servicios, guardas y arranque de las cuatro API y Gateway. Ver evidencia HU-36.
Se conserva el registro anterior de 411 pruebas como resultado histórico de la
primera base. Leslie autorizó posteriormente los commits y push de HU-36 para
revisión. La base verificada sigue siendo e01767b; al preparar la publicación,
develop remoto avanzó a afacebc. Su combinación no está acreditada por esas pruebas.
HTTPS/JWE, CI, revisión cruzada y aceptación humana siguen pendientes. Sin merge.

### Revisión e integración local de la PR #54

El 03/11/2026 se combinó localmente la rama HU-36 con `origin/develop` en
`afacebc`. Cuatro conflictos textuales fueron resueltos y la regresión reveló dos
incompatibilidades de seguridad adicionales: la factoría HU-29 carecía de la clave
interna/JWE y Solicitudes aún no validaba el JWE emitido por Auth. Después de una
prueba roja reproducible se corrigieron ambos contratos. Resultado final: 572
pruebas xUnit correctas, ninguna fallida y 3 SQL omitidas por falta de
`HU10_TEST_SQL`. La repetición Docker/SQL, CI, commit, push, revisión cruzada,
aceptación y merge siguen pendientes; no se declara HU-36 terminada.

## HU-18 — base local del 03/11/2026

Leslie preparó `feature/hu-18-analisis-interno` desde develop `e01767b`, en un
worktree independiente del incremento HU-36. Puerto y caso de uso en Application,
adaptador Ollama en Infrastructure y pruebas sintéticas. Motor/fórmulas intactos;
sin endpoints, persistencia, frontend ni módulos ajenos. TDD: 24 fallos iniciales y
tres fallos de manejo de errores reproducidos antes de sus correcciones.
Regresión Release: 345 correctas y una prueba de modelo real omitida por falta de
configuración. La omisión no equivale a aceptación. Ver
[alcance y contrato](hu-18-analisis-interno.md) y
[evidencia de la base](evidencia-hu-18-base.md).
Pendientes: modelo real, datos HU-11, autorización docente, HU-20, composición/API,
revisión/CI y aceptación de Shirley. Sin commits, push ni merge.

### HU-18: integración sobre develop fb40d0a, 03/11/2026

Se preservó la base anterior y se continuó desde develop con HU-36/HTTPS/JWE/HU-29.
Orquestación por solicitud, autorización del docente asignado y lecturas académicas
privadas; motor determinista conservado, sin implementar HU-20/HU-30. Tres defectos
de validación reproducidos antes de corregirlos y una llamada inexistente de Catálogo
corregida tras fallo de compilación. Regresión final: 652 xUnit correctas, 0 fallidas,
4 omitidas (modelo real y tres SQL de Solicitudes). Swagger repetido tras actualizar
la expectativa de rutas. Docker/SQL aislado: 34 comprobaciones correctas, modelo
deshabilitado, transporte HTTP local; entorno efímero eliminado.

Ver [evidencia de integración y límites](evidencia-hu-18-integracion.md) y
[propuesta para Pablo/Edwin](contrato-integracion-hu-18.md). Faltan modelo real,
persistencia/vista de Edwin, revisión cruzada/CI y aceptación de Shirley. No se
acredita HTTPS o navegador con esta prueba ni HU-18 terminada. Sin nuevos commits,
push, merge ni comunicaciones externas.

### HU-18: entrega autorizada con persistencia y vista

Leslie autorizó modelo qwen3:4b, elección e implementación de persistencia/vista,
omisión de Ollama real, commits atómicos, push y PR. Indicó que Shirley no hará
revisión/aceptación en esta entrega; no se registra ninguna aceptación realizada.
Se actualizó la rama desde develop 95e604d conservando HU-30 y sus pruebas. Base
anterior preservada; conflictos de factorías/contador de rutas resueltos conservando
los controles de seguridad. Homologación guarda el último intento por pareja en
su base SQL propia y la vista docente recupera los resultados al abrir el detalle.

Regresión final: 682 xUnit correctas, 4 omitidas (una de modelo real y tres SQL
existentes); frontend 123 correctas, TypeScript y Vite correctos. SQL Server/Gateway
reales con modelo deshabilitado y doble HTTP: 49 comprobaciones correctas, incluida
durabilidad tras reinicio y error de guardado sin reemplazar datos previos. Los
fallos intermedios de herramientas, expectativas y saturación se documentan en
[evidencia de entrega](evidencia-hu-18-entrega.md). No se alteraron timeouts ni se
acredita inferencia real, HTTPS/navegador nuevo o aceptación humana. No cerrar #42
automáticamente. Publicación de PR autorizada; sin autorización para merge.

### PR #55: revisión de Shirley y corrección de CI HU-36

Shirley registró APPROVED para la versión 6781678. La comprobación HU-36 falló
porque Homologación resolvía migraciones antes de validar seguridad. Leslie autorizó
la corrección y publicación en la misma rama. Cuatro pruebas rojas reprodujeron
el orden incorrecto sin conexión SQL; después del ajuste la solución pasó 436
pruebas, ninguna fallida u omitida. El smoke encontró además un cierre transitorio
durante la espera de arranque del Gateway: se contempló sin aumentar su límite.
Repetición Docker/SQL: 52 comprobaciones correctas, entorno temporal eliminado.
Ver [evidencia de la corrección](evidencia-correccion-arranque-hu18-hu36.md).
No se acredita modelo real ni se atribuye a Shirley revisión de cambios posteriores
a su aprobación. Se comprobará la nueva CI tras publicar; sin merge.
