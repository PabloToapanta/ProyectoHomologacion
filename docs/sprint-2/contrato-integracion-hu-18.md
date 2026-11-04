# Contrato de integración HU-18

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
