# HU-18: verificación de integración

03/11/2026, Leslie, Issue [#42](https://github.com/PabloToapanta/ProyectoHomologacion/issues/42).
Rama feature/hu-18-analisis-interno sobre develop fb40d0a. Sin commit, push, merge
ni comunicación externa. La base anterior e01767b está preservada; ningún cambio
local del equipo se borró. No se integró la PR #53 de HU-30.

## Cambios y fallos reproducidos

Se añadieron orquestación por solicitud, lecturas privadas académicas en Catálogo y
Solicitudes, adaptador HTTP con identidad original y clave de servidor, controlador
Docente y una ruta Gateway. Se abrió el contrato local antes de tocar composición.
Clean Architecture: reglas existentes en Domain, caso/puertos en Application,
HTTP/modelo/configuración en Infrastructure y traducción HTTP en Api. Sin migraciones.

TDD de integración: 15 fallos de Application, 6 de API, 13 de fuentes HTTP, uno
de Gateway, 5 de Solicitudes y 4 de Catálogo observados antes de implementación.
La compilación de Catálogo detectó un método inexistente: se corrigió reutilizando
IUnidadRepository y la proyección existente. Después se reprodujeron tres defectos
adicionales con pruebas rojas: estado numérico aceptado como Aprobada y elementos
nulos en aprobadas/correspondencias causaban NullReferenceException. Ahora se rechazan
como fuente no verificable, sin invocar el modelo. TRX rojos: TestResults/hu18-integracion-red.

La regresión detectó el contador antiguo de rutas en Swagger (20 frente a 21).
Se actualizó para comprobar la ruta nueva y la exclusión de lecturas privadas;
no se retiró ninguna aserción anterior. La composición del cliente del modelo
se prueba con contexto autenticado: no recibe Authorization ni X-Internal-Key.

## Pruebas automatizadas ejecutadas

Windows, .NET SDK 8.0.130, Release. Resultados por suite, sin sumar repeticiones:

| Suite | Correctas | Fallidas finales | Omitidas |
|---|---:|---:|---:|
| Homologacion.Domain.Tests | 71 | 0 | 0 |
| Homologacion.Application.Tests | 60 | 0 | 0 |
| Homologacion.Api.Tests | 106 | 0 | 0 |
| Auth.Application.Tests | 13 | 0 | 0 |
| AuthService.Tests | 64 | 0 | 0 |
| Gateway.Tests | 104 | 0 | 0 |
| Homologacion.Infrastructure.Tests | 33 | 0 | 1 |
| Catalogo.Tests | 94 | 0 | 0 |
| Solicitudes.Tests | 107 | 0 | 3 |
| Total | 652 | 0 | 4 |

Comandos: `dotnet test Homologacion.sln -c Release --no-restore` y `dotnet test`
de los proyectos Homologacion.Infrastructure.Tests, Catalogo.Tests y Solicitudes.Tests.
Gateway se repitió tras corregir su expectativa de Swagger. Infraestructura se repitió
al agregar la comprobación de privacidad. TRX: TestResults/hu18-final-integracion;
los primeros TRX de ese directorio incluyen el fallo de Swagger ya descrito.

Una omisión es OllamaRealTests por falta de HU18_OLLAMA_URL/HU18_OLLAMA_MODEL;
tres son las pruebas SQL existentes de Solicitudes por falta de HU10_TEST_SQL.
La omisión no acredita éxito. CI preparado para estas suites, todavía sin ejecutar
en GitHub. No se modificó frontend: no se acredita navegador ni chequeo de tipos.

## Criterios del Issue y límites

### Docker y SQL reales

Se ejecutó `python tests/security/hu18-smoke.py` con Docker 28.5.1: 34 comprobaciones
correctas, proyecto hu18-check-fa46782434. Construcción/arranque de Auth, Catálogo,
Homologación, Solicitudes y Gateway; migraciones SQL sobre bases recién creadas,
login con JWE real, carreras/asignaturas/programas sintéticos, aprobación y asignación
de dos docentes mediante Auth real. El modelo quedó deshabilitado intencionalmente.

El recorrido comprobó 401 sin JWT, 403 para Coordinador, 404 para docente ajeno,
400 ante cantidades enviadas por cliente, Incompleto sin revisión y Fallido con
resultado nulo sin modelo. Conservó los porcentajes del motor y confirmó que los
contextos privados no son accesibles por Gateway. La prueba usa HTTP local aislado:
no acredita HTTPS, navegador ni inferencia real. No se presenta como cumplimiento de CA5.

La primera ejecución del script terminó durante el arranque por RemoteDisconnected;
se corrigió la espera del propio script y se repitió correctamente. No se modificó
código de producto para ocultar ese fallo. Solo se usaron secretos efímeros generados
por el script, sin leer .env existentes ni tocar bases persistentes. Al terminar se
eliminaron exclusivamente sus contenedores/volumen y archivo temporal; inventario
Docker sin contenedores hu18-check-. Resultado saneado conservado en
[evidencia Docker](evidencias/hu18-20261103/integracion-docker.json).

Se aplicó formato whitespace únicamente a C# nuevos; se revisó el diff de composición.
Las pruebas SQL omitidas de Solicitudes siguen registradas como tales: este recorrido
SQL no las sustituye ni convierte sus omisiones en éxitos.

### Estado de aceptación

- CA1: adaptador configurable y validación JSON comprobados con HTTP simulado;
  ejecución con modelo real pendiente.
- CA2: tres criterios, umbral y regresión del motor conservados; aprobación/antigüedad
  aplicadas con fecha Ecuador. Mínimos globales requieren datos aún no disponibles.
- CA3: evidencias existentes C1/R1 y textos de ambos planes, sin crear coincidencias;
  históricos/incompletitud no invocan modelo y requieren revisión.
- CA4: JSON inválido, timeout, indisponibilidad y modelo deshabilitado producen
  respuesta controlada; resultado nulo salvo Completo.
- CA5/T37: pruebas simuladas ejecutadas; modelo real y conexión HU-20/HU-30 pendientes.

Edwin debe acordar almacenamiento por solicitud/pareja, versión de evidencia y
política de reanálisis; consumir estados/resultado sin convertir null en éxito.
No se implementaron sus módulos. Pablo debe revisar los puntos de composición.
Shirley conserva la aceptación humana pendiente. HU-18 no se declara terminada.

Para su revisión: ejecutar el entorno con un modelo acordado y datos sintéticos,
seleccionar una solicitud interna asignada y una pareja con revisión vigente;
comprobar tres porcentajes, evidencia y advertencias. Repetir sin programa, con
histórico de otra facultad, con revisión obsoleta y con modelo caído. Confirmar
resultado nulo en Incompleto/Fallido y acceso denegado al docente ajeno. Una vez
conectadas HU-20/HU-30, comprobar persistencia y consulta en la vista docente;
registrar la decisión de aceptación de Shirley, aún pendiente.
