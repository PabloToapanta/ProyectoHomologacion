# HU-18 — Análisis interno asistido

Issue #42. Leslie. Base actual develop `fb40d0a`, con HU-36, HTTPS/JWE y HU-29.
Rama `feature/hu-18-analisis-interno`; cambios locales sin commits ni publicación.
La base inicial e01767b se conserva como registro histórico separado. Contrato de
integración abierto antes de modificar la composición, pendiente revisión de
Pablo/Edwin y validación funcional de Shirley.

## Integración actual

POST `/api/homologaciones/solicitudes/{id}/analisis-interno`, solo docente asignado.
Cuerpo: `asignaturaAprobadaId` y `asignaturaReceptoraId`. Rechaza propiedades extra;
el navegador no proporciona cantidades, notas, evidencias ni identidad del docente.
Ver [contrato completo y archivos compartidos](contrato-integracion-hu-18.md).

El flujo consulta Solicitudes y Catálogo por HTTP con el JWT original y la clave
de servidor. Las lecturas privadas no se publican en Gateway. No se consultan bases
ajenas ni se agregan migraciones. Asignación docente, carreras y registros aprobados
se verifican en sus servicios propietarios. Solo el texto académico llega al modelo.

Se aplican aprobación/antigüedad y las tres dimensiones del motor. No se evalúan los
mínimos globales HU-16: faltan períodos cursados y el conjunto completo de asignaturas;
la respuesta lo advierte y no constituye aprobación oficial. Los históricos de otras
facultades se conservan y requieren revisión; nunca se reasignan ni se borran.

Estados compatibles con la propuesta HU-30: Completo, Incompleto o Fallido; resultado
nulo salvo Completo. HU-20 y la conexión de la vista HU-30 siguen pendientes de Edwin.
El modelo está deshabilitado por defecto: el motor sigue disponible y la asistencia
devuelve Fallido, sin éxito ficticio. Habilitar solo con URL/modelo acordados:
`ANALISIS_INTERNO_HABILITADO`, `OLLAMA_URL`, `OLLAMA_MODELO` y timeout 1–300 segundos
en Compose; claves exclusivamente en servidores. No se instala ni descarga un modelo.

Ver [evidencia vigente](evidencia-hu-18-integracion.md). Las secciones siguientes
describen el diseño y la primera etapa del adaptador; sus pendientes se actualizan arriba.

## Primera etapa: adaptador y asistencia

T34/T35 y base de T36: puerto en Application, adaptador Ollama en Infrastructure
y asistencia sobre una evaluación producida por el motor existente. No se cambia
Domain, las tres fórmulas, el umbral ni los contratos HTTP vigentes. No se añaden
endpoints, rutas React, autenticación, persistencia HU-20 ni módulos de HU-19.

El agente explica correspondencias explícitas ya aportadas; no las crea ni modifica
el resultado determinista. Recibe únicamente evaluación y referencias académicas,
sin estudiante, Quipux, contraseñas, JWT ni clave interna. Cada referencia tiene un
identificador local (C1, R1...) y las ubicaciones originales de ambos planes.

Salida propuesta: JSON con `resumen` y `evidenciasCitadas` (IDs de la entrada).
No se admiten propiedades de porcentajes o decisión. JSON inválido, propiedades
extra, referencias desconocidas, servicio caído o timeout producen revisión manual
y no un éxito de asistencia. El resultado original del motor se conserva siempre.
Que una referencia exista no demuestra la verdad semántica del texto: es apoyo
para revisión humana, no validación normativa ni autenticación del programa.

Datos incompletos no invocan el agente. Las reglas HU-15/HU-16 y el conteo global
de asignaturas homologables se incorporarán al flujo cuando estén disponibles sus
datos de HU-11; este paso no declara elegibilidad ni mínimos del cambio cumplidos.

## Adaptador y límites técnicos

Se usa POST `/api/chat`, `stream=false` y `format` con JSON Schema, según
[API de Ollama](https://docs.ollama.com/api/chat) y
[salidas estructuradas](https://docs.ollama.com/capabilities/structured-outputs).
La respuesta se vuelve a validar localmente. Temperatura 0 ayuda a reducir
variación; no vuelve determinista al modelo ni al texto generado.

Modelo y URL deben configurarse explícitamente. No se ha elegido, descargado ni
instalado un modelo por cuenta del equipo. Timeout configurable, sin reintentos
automáticos. Entrada/salida acotadas. No se siguen enlaces de evidencia ni se envían
herramientas al modelo. La cancelación del usuario se propaga; los fallos operativos
conocidos se representan sin publicar cuerpo de respuesta ni datos sensibles.

## Dependencias para terminar la historia

- Modelo/servidor Ollama y ejecución real: configuración solicitada a Leslie.
- HU-11: datos aprobados y evidencia académica de la solicitud.
- HU-29/HU-30: comprobar en servidor la asignación docente antes de exponer análisis.
- HU-20: identificador de solicitud, estados y contrato de almacenamiento/reanálisis.
- Pablo: acordar integración en Program.cs/Gateway cuando se defina el endpoint.

No se declarará HU-18 terminada por aprobar mocks. Las pruebas simuladas y la prueba
real se documentarán por separado; la aceptación corresponde a Shirley.

## Código disponible y contrato de composición

- `IAgenteAnalisisInterno` y los DTO están en Application. `AsistirEvaluacionInterna`
  ejecuta primero `EvaluarEquivalencia`, construye referencias C1/R1 y solicita la
  explicación. `Completada` describe solo la asistencia técnicamente validada; no
  cambia estados de Solicitud ni equivale a aprobación académica.
- `OllamaAgenteInterno` está en Infrastructure. Usa `HttpClient` inyectado y
  `OllamaAnalisisOptions` con URL, modelo y timeout positivos (máximo cinco minutos).
  La futura composición debe usar `HttpClientHandler.AllowAutoRedirect=false` y
  `HttpClient.Timeout=Timeout.InfiniteTimeSpan`; el adaptador controla su timeout.
  URL sin credenciales/query/fragmento, terminada en `/`. No hay modelo por defecto.
- Máximo 200 referencias y 64 KiB de contexto o respuesta HTTP; resumen de hasta
  4000 caracteres. Los IDs citados deben existir y no repetirse. No basta enviar el
  esquema a Ollama: el adaptador y el caso de uso validan localmente la salida.
- El texto sigue siendo no confiable. HU-30 debe mostrarlo como asistencia,
  separado de los porcentajes/estado del motor y como texto, sin ejecutar HTML.
  La existencia de un ID no verifica semánticamente lo redactado por el modelo.
- El proyecto nuevo de pruebas de Infrastructure se ejecuta explícitamente:
  no se modificó Homologacion.sln ni Program.cs de Pablo. El workflow específico
  incluye tanto la solución existente como ese proyecto.

## Reproducción

```text
dotnet test Homologacion.sln -c Release
dotnet test tests/Homologacion.Infrastructure.Tests/Homologacion.Infrastructure.Tests.csproj -c Release
```

Para la prueba real, configurar en el proceso local `HU18_OLLAMA_URL` (URL del
servidor aprobado por el equipo), `HU18_OLLAMA_MODEL` (modelo instalado) y,
opcionalmente, `HU18_EVIDENCE_DIR` (directorio local de evidencia). No se leen
archivos .env ni se descargan modelos. Ejecutar:

```text
dotnet test tests/Homologacion.Infrastructure.Tests/Homologacion.Infrastructure.Tests.csproj -c Release --filter FullyQualifiedName~OllamaRealTests
```

Esta prueba combina motor real y adaptador real, usa datos sintéticos identificados
y exige asistencia válida. Una respuesta errónea o indisponibilidad falla la prueba;
no se compara el texto literal ni se reemplaza el modelo por un mock. Si no se han
configurado URL/modelo queda omitida, explícitamente pendiente.

Ver [resultados reales del incremento](evidencia-hu-18-base.md).
