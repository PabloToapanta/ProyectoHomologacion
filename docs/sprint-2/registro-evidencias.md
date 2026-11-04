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
