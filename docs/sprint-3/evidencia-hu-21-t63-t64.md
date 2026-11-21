# HU-21 / Issue #75 — incremento T63/T64 de Leslie

21/11/2026, America/Guayaquil. Rama compartida `feature/hu-21-agrupacion-nm`,
[Draft PR #91](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91).
Base sincronizada: `3b8477c`, después del motor T62 y contratos de Pablo.
Esto acredita implementación y comprobaciones locales; no aceptación de HU-21.

## Coordinación y alcance

Se leyeron AGENTS.md, spec, plan técnico e Issue #75. Se consultó la Draft PR y se
registraron antes de intervenir las propuestas de archivos y cambios aditivos:
[fuentes, persistencia y composición](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91#issuecomment-7069673591),
[visualización del informe](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91#issuecomment-7069849584),
[OpenAPI](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91#issuecomment-7069926416),
[datos de regresión](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91#issuecomment-7070023870) y
[vía manual 1:1](https://github.com/PabloToapanta/ProyectoHomologacion/pull/91#issuecomment-7070103511).
No se modificaron EvaluarAgrupacion, RegistrarRevisionAgrupacion,
GenerarInformeHomologacion ni sus contratos de Pablo.

### Implementación

- Domain: GrupoAgrupacion y composición validada. Rechaza duplicados, pares ajenos,
  elementos sin vínculo, cantidades negativas, más de dos decimales y un grupo 1:1.
  Null sigue siendo pendiente y cero un aporte explícito. Límites técnicos: 50
  orígenes/receptoras y 2500 vínculos; no constituyen una regla académica.
- Application: puertos y casos de creación, lectura, reemplazo, borrado y análisis;
  autorización por Solicitudes, ETag, pertenencia a carreras y suma de aportes por
  origen. Construye la entrada del motor existente únicamente desde datos de los
  servicios propietarios y revisiones Registrada/Vigente del Catálogo.
- La ampliación aditiva `intentoMatricula` del contexto interno no incluye número
  de matrícula ni datos del estudiante. Sin intento se informa incompletitud;
  tercera matrícula, reprobación o antigüedad excluyen ese origen de los aportes,
  conservando el resto de la solicitud. Se usa la antigüedad vigente de HU-03.
  Los motivos comprobados se fotografían en faltantes/justificación, sin cambiar
  porcentajes, estado ni sugerencia calculados por EvaluarAgrupacion.
- Infrastructure: seis tablas propias de Homologación para grupo, orígenes,
  receptoras, aportes, reservas y revisiones. Horas decimal(12,2), créditos decimal(6,2).
  La migración nueva añade esquema sin convertir grupos ficticios, borrar ni
  reasignar datos antiguos. El snapshot vigente se separó del archivo de migraciones
  histórico; sus cuerpos Up/Down anteriores no se modificaron.
- Reserva única por solicitud/origen. Grupos, parejas automatizadas y revisiones
  manuales sin agente utilizan el mismo cierre transaccional SQL de informe.
  Las parejas históricas repetidas permanecen consultables/corregibles, pero sus
  orígenes no pueden incorporarse a unidades nuevas. No se inventan reservas históricas.
- El último análisis por grupo congela la composición y reservas, incluso si queda
  incompleto. Null en aportes devuelve guardado=false sin congelar. Reemplazo y
  eliminación del borrador intercambian/liberan reservas atómicamente.
- DI conecta IResultadosAgrupacionInforme, IRevisionesAgrupacion,
  IRevisionesAgrupacionInforme y RegistrarRevisionAgrupacion con el mismo contexto.
  Informe y revisión usan el cierre existente entre instancias. Se conserva una
  revisión por docente/grupo y un grupo por entrada del informe.
- API y Gateway: rutas propias bajo
  `/api/homologaciones/solicitudes/{id}/agrupaciones`. DTO de entrada sin campos de
  aprobación, evidencia ni veredicto; rechaza propiedades extra. Creación 201;
  lecturas y análisis 200; borrado 204. Errores 400/401/403/404/409/428/503 según spec.
  ETag fuerte en creación/detalle/edición/análisis; If-Match obligatorio para PUT,
  DELETE y POST análisis. OpenAPI documenta cabeceras, respuestas y estado textual.
- Frontend: matriz origen × receptora, celdas habilitadas solo con revisión vigente,
  cantidades explícitas, saldos, totales recibidos, resultado por receptora y
  sugerencia única. No permite analizar cambios sin guardarlos. Un 409 recarga la
  composición vigente y descarta la edición local; no mezcla aportes. Revisión
  grupal por docente e informe aditivo con grupos y parejas históricas.

Para orígenes externos sin programa registrado continúa D2: información incompleta;
no se crea un programa externo ni una correspondencia desde HU-21. La pantalla no
habilita una celda cuya revisión vigente no pueda comprobar. El resultado sigue
siendo una sugerencia del proyecto, no una resolución oficial. No se usa LLM.

## Pruebas y resultados reales

TDD registrado en `TestResults/hu21/rojo`: cinco fallos de entidad/composición,
tres de construcción desde fuentes, uno de reservas y tres de matriz antes de
implementarlos. La revisión encontró además dos defectos reproducidos primero:
la revisión manual sin agente podía compartir un origen grupal, y el motivo exacto
de tercera matrícula no quedaba en la fotografía persistida. Ambos tienen pruebas
rojas y corrección posterior. No se modificó el motor para corregirlos.

La regresión detectó la fábrica de informe sin fuentes grupales (se añadieron
vacías para su escenario histórico), el contador anterior de rutas Swagger y la
traducción de errores SQL en las nuevas consultas. Se conservaron sus aserciones.
Dos datos de pruebas SQL HU-20 usaban el mismo origen para unidades nuevas: ahora
usan orígenes distintos, conservando todas las aserciones de durabilidad/concurrencia.
Un caso nuevo actualiza desde esquema de develop con parejas históricas repetidas
y comprueba que su JSON y el informe permanecen idénticos.

La suite frontend tuvo un timeout de 5 s en la prueba existente de UnidadesPanel
durante una ejecución simultánea con compilaciones. Repetida sin esa carga y con
un proceso: **168 correctas en 28 archivos**, cero fallidas. No se editó esa prueba,
no se ampliaron timeouts ni se omitió el caso. TypeScript y Vite pasaron.

Los contadores finales de backend y SQL se consolidan directamente desde los TRX
en la evidencia de esta entrega; no se suman repeticiones ni subconjuntos filtrados.

| Suite final | Correctas | Fallidas | Omitidas |
| --- | ---: | ---: | ---: |
| Homologacion.Domain.Tests | 88 | 0 | 0 |
| Homologacion.Application.Tests | 165 | 0 | 0 |
| Homologacion.Api.Tests | 140 | 0 | 0 |
| Auth.Application.Tests | 13 | 0 | 0 |
| AuthService.Tests | 79 | 0 | 0 |
| Gateway.Tests | 142 | 0 | 0 |
| Homologacion.Infrastructure.Tests, con SQL Server aislado | 68 | 0 | 1 |
| Solicitudes.Tests | 141 | 0 | 5 |
| Frontend, 28 archivos | 168 | 0 | 0 |
| Total único | **1004** | **0** | **6** |

La omitida de Infraestructura es la prueba opcional de Ollama real de otra historia;
HU-21 no usa LLM. Las cinco SQL existentes de Solicitudes se omitieron por falta de
HU10_TEST_SQL en esa suite: no se convierten en éxitos por el recorrido del smoke.
La regresión de SQL de Homologación sí ejecutó todos sus casos, incluidas las cuatro
pruebas de agrupaciones: actualización desde develop, dos grupos concurrentes,
competencia de grupo/pareja/revisión manual con rollback, y revisión/informe concurrentes.
Entorno: Windows, .NET SDK 10.0.403 compilando target net8.0, Node 24.21.0 y Docker 28.5.1.
Los servicios se construyeron además con las imágenes SDK/runtime .NET 8 del Compose.

Evidencia verificable: [resumen y TRX de origen](evidencias/hu21-20261121/resumen.json)
y [62 comprobaciones de integración](evidencias/hu21-20261121/integracion.json).
El último proyecto temporal fue hu21-check-324f6b06d7; se verificó la eliminación
de sus contenedores y volumen. No se suman sus 62 comprobaciones a los tests unitarios.

La prueba de OpenAPI reprodujo además una representación textual global que habría
documentado incorrectamente el estado numérico histórico del informe. Se corrigió
con DTO propios de agrupación; el informe conserva su contrato anterior y los grupos
documentan estados textuales. La API completa se repitió después: 140 correctas.

Comandos reproducibles:

```text
dotnet test Homologacion.sln -c Release --no-restore
dotnet test tests/Solicitudes.Tests -c Release --no-restore
python -u tests/security/hu21-smoke.py
node node_modules/vitest/vitest.mjs run --maxWorkers=1
node node_modules/typescript/bin/tsc --noEmit
node node_modules/vite/bin/vite.js build
```

El smoke genera secretos, cuentas, proyecto y bases temporales exclusivos. No lee
.env existentes ni usa bases persistentes. Comprueba migración, JWT cifrado, Gateway,
Catálogo/Solicitudes, reservas, ETag, null/0, motor determinista, dos revisiones,
informe y durabilidad tras reinicio. Su suite SQL comprueba además competencia
entre grupo/pareja/revisión manual, rollback y revisión concurrente con generación.
HTTP local aislado: no acredita una nueva verificación HTTPS ni navegador real.
Docker estaba apagado en el primer intento; se inició y se repitió. Se documentan
las ejecuciones intermedias fallidas, sin presentarlas como resultados finales.

## Matriz de verificación

| Criterio | Evidencia |
| --- | --- |
| CA1 | GrupoAgrupacionTests, PrepararAgrupacionTests, AgrupacionesEndpointTests y matriz frontend; IDs, pertenencia, vínculos y saldos. |
| CA2 | Elegibilidad individual HU-15/HU-17; intentos ausentes; SQL con reservas únicas, tres escritores y conservación del legado; actualización/borrado atómicos. |
| CA3 | Revisiones vigentes frente a obsoletas, evidencia por vínculo, N:M y cobertura receptora sin duplicación; motor T62 conservado. |
| CA4 | N:M, 1:N y varias parejas; aportes decimales, null/0, exceso y datos faltantes; porcentajes separados, parámetros versionados y sugerencia única. |
| CA5 | API/DI real con almacenamiento: grupo y pareja histórica en un informe; dos revisiones; rechazo tras cierre; carrera SQL revisión/informe. |
| CA6 | TRX, Vitest, TypeScript/Vite, OpenAPI, Docker/SQL y workflow preparado; revisión cruzada/aceptación todavía pendientes. |

## Pendientes de entrega

La rama conserva commits locales pequeños en español. No se ha realizado push ni
merge: la instrucción actual autorizó commits, y AGENTS.md exige permiso humano
para publicar. La Draft PR mantiene su estado; los comentarios de coordinación no
equivalen a publicación del código o ejecución de CI sobre estos commits.

Después de publicar, Edwin debe revisar persistencia, cierre entre instancias,
compatibilidad histórica y UI; Pablo debe revisar el consumo de sus contratos.
Shirley debe comprobar los escenarios de la spec y aceptar la implementación.
La aceptación anterior del alcance N:M no equivale a aceptar este incremento.
HU-21 no se declara terminada y no se incorpora a develop desde este trabajo.
