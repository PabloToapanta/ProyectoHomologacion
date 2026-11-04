# HU-18: entrega con persistencia y vista docente

Fecha local: 03/11/2026, America/Guayaquil. Base develop 95e604d. Rama
feature/hu-18-analisis-interno. Issues relacionados: [#42 HU-18](https://github.com/PabloToapanta/ProyectoHomologacion/issues/42),
[#44 HU-20](https://github.com/PabloToapanta/ProyectoHomologacion/issues/44) y
[#41 HU-30](https://github.com/PabloToapanta/ProyectoHomologacion/issues/41).

## Decisiones autorizadas por Leslie

- Modelo qwen3:4b, configurable en servidor. Se habilita su adaptador en API/Compose;
  no se instala ni descarga el modelo. URL y timeout pueden sobrescribirse.
- Se omite la prueba con Ollama real por instrucción expresa. Se conserva como
  prueba opcional para otra ocasión. No se acredita inferencia ni calidad del modelo.
- Leslie autorizó elegir/completar persistencia y vista, crear commits atómicos y
  publicar la rama y PR. No autorizó integrar la PR a develop/main.
- Shirley no realizará revisión ni aceptación en esta entrega por indicación de
  Leslie. No se registra aceptación humana ni revisión que no se efectuó. Esto no
  transforma las pruebas simuladas en cumplimiento de CA5 de HU-18.

## Implementación entregable

Homologación conserva Domain/Application/Infrastructure/Api. Domain y las fórmulas
no se modifican. Application orquesta datos autorizados, motor, asistencia y puerto
de almacenamiento. Infrastructure implementa HTTP, Ollama y EF Core SQL Server.
La API valida identidad/rol y traduce errores; Gateway publica solo el contrato público.

La base propia HomologacionAnalisis utiliza la migración
20261104000100_AnalisisInternosHu20. Guarda snapshot JSON del resultado, fecha y
los tres identificadores de pareja. PK compuesta evita duplicados accidentales.
Cada pareja conserva el último intento guardado; otras parejas quedan intactas.
Los estados Incompleto/Fallido también se guardan, con resultado nulo. El POST no
responde 200 hasta terminar SaveChanges. Un fallo SQL devuelve 503 sin confirmación.
La migración y el snapshot coinciden con el modelo (prueba sin conexión), y se
aplicaron realmente en SQL Server temporal durante el smoke.

La lectura vuelve a comprobar la asignación del docente a la solicitud. La vista
HU-30 integrada en develop se reutiliza: consulta automática de guardados al abrir
el detalle, selección de asignatura aprobada/receptora y ejecución por pareja. No
incluye aprobar, remitir, revisar formalmente ni módulos HU-19/HU-31/HU-08.
Solo Completo con resultado muestra recomendación. Los mínimos globales no pueden
inferirse sin períodos y cómputo total; la respuesta lo advierte expresamente.

Contrato: POST/GET `/api/homologaciones/solicitudes/{id}/analisis-interno` y GET
`/api/homologaciones/solicitudes/{id}/analisis-interno/opciones`, documentados en Swagger.
El cuerpo POST contiene únicamente los dos IDs. No se reciben cantidades ni docente
desde navegador. Opciones limitadas a la carrera de la solicitud, por nivel/código.
Las lecturas académicas entre servicios permanecen privadas y usan JWT + clave interna.
El cliente del modelo no recibe ninguna de esas credenciales ni campos del estudiante.

## TDD y regresión backend

Antes de implementar este incremento se observaron 5 fallos de Application, 2 de
almacenamiento y 2 de API para guardado/lectura; además falló la prueba del nuevo
parámetro think=false antes de añadirlo. La primera etapa ya tiene sus TRX rojos
documentados por separado. La interfaz nueva registró 3 fallos antes de implementarse.
No se debilitaron reglas ni se modificaron tiempos límite para obtener resultados verdes.

Windows, .NET SDK 8.0.130; compilación Release mediante las suites:

| Suite | Correctas | Fallidas finales | Omitidas |
|---|---:|---:|---:|
| Auth.Application.Tests | 13 | 0 | 0 |
| AuthService.Tests | 64 | 0 | 0 |
| Catalogo.Tests | 94 | 0 | 0 |
| Gateway.Tests | 110 | 0 | 0 |
| Homologacion.Api.Tests | 109 | 0 | 0 |
| Homologacion.Application.Tests | 65 | 0 | 0 |
| Homologacion.Domain.Tests | 71 | 0 | 0 |
| Homologacion.Infrastructure.Tests | 36 | 0 | 1 |
| Solicitudes.Tests | 120 | 0 | 3 |
| Total backend | 682 | 0 | 4 |

Comandos: `dotnet test Homologacion.sln -c Release --no-restore` y los proyectos de
Infrastructure.Tests, Catalogo.Tests y Solicitudes.Tests por separado. La suite de
Infrastructure se repitió tras añadir la comprobación de migración/snapshot. Una
omisión corresponde al modelo real (decisión de Leslie); las tres SQL existentes
requieren HU10_TEST_SQL y conservan su estado omitido. El smoke SQL no cambia sus
contadores. Evidencia [suites-entrega.json](evidencias/hu18-20261103/suites-entrega.json).
TRX finales locales en TestResults/hu18-entrega-final, rojos en hu18-entrega-red.

## SQL Server, Gateway y persistencia reales

`python tests/security/hu18-smoke.py`: 49 comprobaciones correctas. Proyecto aleatorio
hu18-check-b7205a1c4e; volumen, contenedores y archivo temporal eliminados al finalizar.
No se leyeron .env existentes ni se tocaron bases persistentes del equipo. Se ejecutó
en HTTP local aislado: no acredita HTTPS ni navegador real.

Login JWE, Catálogo, Solicitudes y asignación docente reales; programas y cuentas
sintéticos. Se comprobó acceso por asignación, rechazo de cantidades adulteradas,
contextos privados fuera del Gateway, incompletitud y modelo deshabilitado. Después
se usó un doble HTTP de Ollama qwen3:4b solo dentro de esa red para generar Completo,
validar evidencias y guardar. Se recuperaron Fallido y Completo después de recrear y
reiniciar Homologación. El reanálisis conservó una fila por pareja. Un trigger SQL
de prueba produjo fallo de guardado: HTTP 503 y snapshot anterior intacto.

El doble no aceptaba inicialmente cuerpos HTTP chunked de .NET; falló el smoke,
se corrigió exclusivamente esa herramienta y se repitió correctamente. No se cambió
el producto para aparentar inferencia exitosa. Evidencia saneada:
[entrega-docker.json](evidencias/hu18-20261103/entrega-docker.json).

## Frontend y límites de aceptación

Node 24.21.0, dependencias de package-lock restauradas con npm ci. TypeScript
(`tsc --noEmit`) y compilación Vite correctos. La regresión inicial detectó las
expectativas anteriores de rutas Swagger, botones y peticiones de la vista: se
actualizaron para exigir el nuevo contrato, conservando controles anteriores.
Una ejecución simultánea con compilaciones Docker tuvo 7 fallos por timeout en
pruebas existentes. Se repitió limitando concurrencia, sin cambiar límites ni esas
pruebas. Resultado final: 17 archivos, 123 pruebas correctas, ninguna fallida
(`vitest run --maxWorkers=2`). No hay cambios de timeouts. TypeScript y Vite se
ejecutaron después de conectar la recuperación automática de resultados y pasaron.
Total único backend + frontend: 805 pruebas correctas; las 49 comprobaciones del
smoke se informan por separado. Logs locales: frontend/hu18-frontend-publicacion.log,
hu18-typecheck-entrega.log y hu18-vite-entrega.log. Formato C# y diff comprobados.

No hay aceptación funcional realizada por Shirley ni prueba de modelo real. CA1 y
CA5 de HU-18 conservan esa limitación explícita; no cerrar automáticamente #42.
El contrato de persistencia/vista ya no queda bloqueado por coordinación: la elección
de Leslie está documentada en [el contrato vigente](contrato-integracion-hu-18.md).
La CI se ejecutará al publicar la PR y debe informarse con sus resultados reales.
