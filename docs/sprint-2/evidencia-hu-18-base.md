# Evidencia de la base de HU-18

Registro histórico de la primera base e01767b. No describe la composición actual.
Consultar la [verificación de integración sobre fb40d0a](evidencia-hu-18-integracion.md)
para los resultados y pendientes vigentes. Los contadores de ambas etapas no se suman.

03/11/2026. Issue #42. Leslie, implementación asistida. Rama
`feature/hu-18-analisis-interno`, base develop `e01767b`. Worktree separado de
HU-36 para conservar commits y revisiones independientes. Incremento local sin
commit, push ni merge. No se comunica ningún resultado externamente.

## Alcance comprobado

Se añadieron el puerto y los modelos en Application, el paso
`AsistirEvaluacionInterna`, la validación de referencias y el adaptador HTTP Ollama
en Infrastructure. No se alteraron Domain, las fórmulas, el umbral, DTO HTTP,
controladores, frontend, archivos de arranque, solución ni configuración Docker.
No se implementaron otros módulos del sprint.

El adaptador solicita JSON Schema, verifica la salida nuevamente, acota entrada y
respuesta, exige referencias existentes y rechaza propiedades de decisión añadidas.
Sin datos completos no llama al agente. Conserva siempre la evaluación del motor;
indisponibilidad/timeout/salida inválida producen revisión manual sin asistencia
exitosa ficticia. Una cancelación del usuario se propaga. No sigue enlaces ni usa
herramientas del modelo, y sus errores no publican el cuerpo del proveedor.

## TDD y verificación

Se escribieron contratos y estructuras mínimas y luego se ejecutaron las pruebas
antes de implementar: 10 fallos de Application y 14 del adaptador. La primera
compilación paralela encontró un bloqueo del archivo DLL compartido; se repitió
secuencialmente y se observaron los fallos funcionales con NotImplementedException.
Tras implementar pasaron las pruebas.

La revisión encontró dos problemas adicionales del incremento: un done_reason de
tipo no textual y un corte de lectura escapaban como excepciones sin traducir.
Se reprodujeron con tres pruebas fallidas, se corrigieron y se volvieron a ejecutar.
Dos pruebas adicionales combinan motor, adaptador y HTTP simulado (200 y 503).

Entorno: Windows y .NET SDK 8.0.130. Regresión Release ejecutada:

| Suite | Correctas | Fallidas | Omitidas |
|---|---:|---:|---:|
| Homologacion.Domain.Tests | 71 | 0 | 0 |
| Homologacion.Application.Tests | 42 | 0 | 0 |
| Homologacion.Api.Tests | 85 | 0 | 0 |
| Auth.Application.Tests | 13 | 0 | 0 |
| AuthService.Tests | 44 | 0 | 0 |
| Gateway.Tests | 71 | 0 | 0 |
| Homologacion.Infrastructure.Tests | 19 | 0 | 1 |
| Total de esta rama | 345 | 0 | 1 |

La única omitida es `OllamaRealTests.Explica_datos_sinteticos_con_motor_y_modelo_reales`:
no están configuradas HU18_OLLAMA_URL/HU18_OLLAMA_MODEL y no se pudo consultar
`http://127.0.0.1:11434/api/tags`. No se instaló ni descargó un modelo. No hay evidencia
de modelo real ni se presenta el HTTP simulado como tal. Se solicitó a Leslie el
modelo y servidor acordados. Los contadores de HU-36 no se suman a estos: son
ramas distintas y cuentan con bases y cambios diferentes.

Comandos ejecutados: `dotnet test Homologacion.sln -c Release` y
`dotnet test tests/Homologacion.Infrastructure.Tests/Homologacion.Infrastructure.Tests.csproj -c Release`.
La compilación Release de ambas terminó con cero errores y cero advertencias.
Formato whitespace de los archivos C# nuevos verificado; se corrigieron dos
saltos de línea en el objeto del esquema antes de repetir la comprobación.
`git diff --check` y enlaces locales correctos.
TRX finales en TestResults/hu18-final y TestResults/hu18-final-infra; fallos iniciales
en TestResults/hu18-red. El workflow específico está preparado, sin ejecución en GitHub.
El resumen verificable se conserva en
[evidencias/hu18-20261103/resumen.json](evidencias/hu18-20261103/resumen.json).

No cambia la composición del producto: el adaptador todavía no se registra ni se
expone desde la API, por lo que no se presenta un arranque Docker integrado de HU-18.
Los controles HTTP de las historias existentes sí se ejecutaron mediante su suite
de regresión. No se repitieron frontend/SQL: este incremento no modifica sus capas.

## Pendientes que impiden cerrar HU-18

1. Acordar modelo/servidor y ejecutar la prueba real con evidencia sintética saneada.
2. Coordinar HU-11 para datos de asignaturas aprobadas y evidencia de los planes.
3. HU-29/HU-30: autorizar en servidor al docente asignado. No habilitar un endpoint
   que permita analizar solicitudes ajenas basándose en datos remitidos por el cliente.
4. HU-20: acordar solicitudId, estados y política de reanálisis/persistencia. Este
   paso no guarda resultados ni implementa el flujo de solicitudes.
5. Completar la orquestación de aprobación/antigüedad y mínimos internos cuando
   estén sus datos; este incremento de equivalencia no afirma aplicar HU-15/HU-16.
6. Coordinar Program.cs, Gateway/Swagger y vista consumidora; revisión cruzada,
   CI, integración real y aceptación de Shirley.

La base es revisable y comprobada con datos sintéticos; HU-18 continúa incompleta.
