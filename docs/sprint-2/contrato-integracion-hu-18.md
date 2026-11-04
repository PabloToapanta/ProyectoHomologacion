# Contrato de integración HU-18

## Decisión vigente de Leslie para esta entrega

Leslie autorizó completar persistencia/vista, elegir el contrato, usar qwen3:4b,
omitir las pruebas con Ollama real y publicar commits/PR. Indicó que Shirley no
realizará revisión ni aceptación en esta entrega. No se registra una aceptación
humana que no ocurrió ni se acredita inferencia real. No se modifica AGENTS.md
ni el rol de PO del proyecto. Esta decisión sustituye los pendientes de coordinación
de la propuesta histórica siguiente para el alcance concreto de esta rama.

Base actual: develop 95e604d, que ya contiene HU-30. Se reutiliza su página sin
crear rutas React ni modificar autenticación. Homologación es dueño del resultado:
base lógica HomologacionAnalisis, EF Core SQL Server y migración
20261104000100_AnalisisInternosHu20. No hay FK a bases de otros servicios.

- Identidad de pareja: solicitudId + asignaturaAprobadaId + asignaturaReceptoraId,
  clave primaria compuesta. El último intento guardado reemplaza el de esa pareja;
  otras parejas/solicitudes se conservan. Sin historial/auditoría avanzada.
- POST en la ruta ya propuesta analiza y guarda antes de responder 200. Si falla
  SQL, devuelve 503 y no confirma guardado. Concurrencia de primera inserción:
  la clave única evita duplicados; un conflicto devuelve error controlado, sin
  reintento automático del modelo. Actualizaciones: último guardado confirmado.
- GET en la misma ruta devuelve una lista `{fecha, analisis}` por pareja, más
  recientes primero, y vuelve a comprobar asignación docente en Solicitudes.
  Lista vacía significa que no hay análisis guardado; no significa equivalencia.
- GET en `/opciones` devuelve las receptoras de la carrera de esa solicitud,
  ordenadas por nivel/código. La lectura de Catálogo
  `/api/carreras/{id}/analisis-interno/asignaturas` sigue siendo privada.
- La vista HU-30 recupera los guardados al abrir el detalle. El docente puede
  seleccionar una pareja y ejecutar/guardar HU-18, sin aprobar, remitir ni modificar
  Solicitudes. Los mínimos globales continúan sin inferirse: falta su información.
- Estado Completo/Incompleto/Fallido conservado; resultado nulo salvo Completo.
  La respuesta determinista, advertencias y referencias se guardan como snapshot
  JSON propio; no se recalculan al leer ni se convierten datos históricos.
- qwen3:4b habilitado por configuración; URL local 127.0.0.1:11434 en API y
  host.docker.internal:11434 en Compose. URL/modelo/timeout sobrescribibles solo en
  servidor. think=false, JSON validado; el motor conserva el veredicto.

Ver [entrega y resultados vigentes](evidencia-hu-18-entrega.md). La propuesta del
03/11 que sigue se conserva como registro de las decisiones anteriores.

Propuesta local del 03/11/2026, Issue #42. Base develop fb40d0a, con HU-11/HU-29
y HU-36 integradas. No incorpora PR #53 (HU-30), pero conserva su contrato propuesto
de estados Pendiente/Completo/Incompleto/Fallido y resultado nulo salvo Completo.
Se abre esta propuesta antes de modificar los puntos de composición compartidos.
Pendiente revisión de Pablo/Edwin y aceptación de Shirley; sin comunicación externa.

## Flujo acotado

POST `/api/homologaciones/solicitudes/{id}/analisis-interno`, solo Docente con `sub`
GUID válido. Cuerpo: `asignaturaAprobadaId` y `asignaturaReceptoraId`. No acepta
identidad de docente, notas, aprobación, horas, créditos, porcentajes, umbral ni
correspondencias desde el navegador. Analiza un origen contra una receptora por
petición; no inventa agregaciones para múltiples orígenes.

1. Consultar contexto mínimo de Solicitudes con el Bearer original y la clave de
   servidor. Solicitudes comprueba el docente asignado usando el sub del JWT y HU-29.
   Solicitud ausente o ajena: mismo 404; no se usa una cuenta Coordinador como bypass.
2. Tomar únicamente una asignatura registrada en esa solicitud interna. Obtener
   programa y correspondencias del Catálogo. Verificar carreras FISEI y relación
   origen/receptora. No consultar directamente bases ajenas ni añadir migraciones.
3. Aplicar aprobación/antigüedad con fecha de Ecuador desde TimeProvider. Si requiere
   validación de conocimientos, falta programa, no hay revisión vigente o difieren
   las cantidades registradas respecto del catálogo actual, informar incompletitud
   y no invocar el agente. No corregir históricos ni reasignar datos.
4. El motor calcula las tres compatibilidades. El agente solo explica evidencia
   explícita previamente revisada, incluyendo textos académicos de ambos programas.
5. Devolver estado y resultado compatibles con el contrato propuesto HU-30.
   Completo significa evaluación de esa pareja, no aceptación de una solicitud ni
   aprobación oficial. Fallo del agente: Fallido/revisión manual y resultado nulo.

## Lecturas internas nuevas

- Solicitudes: GET `/api/solicitudes/{id}/contexto-analisis-interno` (Docente), sin
  Quipux, identificación/nombre del estudiante ni matrícula. Usa ISolicitudRepository
  y la asignación existente; no modifica el módulo ni las rutas propias de HU-30.
- Catálogo: GET `/api/asignaturas/{id}/analisis-interno/contexto` y GET
  `/api/asignaturas/{id}/analisis-interno/correspondencias/{receptoraId}` (Docente,
  solo lectura). No concede CRUD ni modifica los permisos de endpoints anteriores.
- Estas lecturas no se publican por Gateway: las consume Homologación desde la
  red interna con clave y JWT. Se añaden controladores/proyecciones nuevos,
  reutilizando servicios registrados para no editar Program.cs de Solicitudes/Catálogo.

## Archivos compartidos y límites

Program.cs de Homologación: una llamada explícita de registro del nuevo flujo.
Gateway appsettings.json: una ruta POST al cluster homologacion, sin modificar las
rutas de HU-30. Compose: URLs internas y configuración opcional del agente. No se
modifican App.tsx, autenticación, fórmulas ni archivos de persistencia HU-20.

La asistencia queda deshabilitada por defecto; sin modelo configurado devuelve
fallo controlado para revisión manual, sin bloquear el arranque del motor. Si se
habilita, URL/modelo/timeout deben ser válidos. Nunca recibe claves ni datos personales.

HU-16 no puede inferirse de nivel o matrícula: los períodos cursados no están en
el contrato de Solicitud. Se informa explícitamente que los mínimos globales del
cambio quedan pendientes; no se declara que este análisis por asignatura los cumpla.
La persistencia HU-20 y la conexión de la vista HU-30 continúan bajo sus responsables.
