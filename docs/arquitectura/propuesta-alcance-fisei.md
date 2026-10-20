# SC-01 — Delimitar el catálogo receptor a FISEI

Fecha: 20/10/2026. Solicitante: Pablo.
Estado: alcance acordado con Shirley según confirmación de Leslie en este chat el 20/10/2026; implementación en `feature/hu-04-alcance-fisei`. Aceptación funcional del incremento pendiente.
Historias afectadas: HU-04 / Issue #2; integración con HU-05 / #3,
HU-06 / #4 y HU-16 / #7. HU-01 y la fórmula de HU-14 se conservan.

## Motivo y aclaración recibida

Pablo aclara que el sistema pertenece a FISEI y que:

- La carrera receptora siempre pertenece a FISEI de la UTA.
- En homologación interna, la carrera de origen también pertenece a FISEI de la UTA.
- En homologación externa, la carrera de origen pertenece a otra universidad del Ecuador.

Aclaración posterior de Pablo del mismo día: al decir «universidades extranjeras»
se refería a universidades externas a la UTA, ubicadas en Ecuador. Para evitar
ambigüedad se utilizará «externa a la UTA»; «extranjera» se reservaría para una
institución de otro país, caso que no forma parte del alcance aquí aclarado.

Según esta aclaración, una carrera de otra facultad de la propia UTA no se debe
clasificar automáticamente como externa: no encaja en ninguno de estos dos casos.
Esto se registra como regla de alcance solicitada por el equipo, no como norma
institucional oficial. Shirley debe registrar su aceptación y cualquier consulta
académica que corresponda.

El catálogo actual pide facultad libre en creación y edición porque HU-04 la
incluye expresamente y define duplicados por nombre/facultad. No es un campo
exclusivo de pruebas. Cambiar los datos sintéticos no elimina esa discrepancia.

## Cambio propuesto para este incremento

Tratar el CRUD actual de carreras como catálogo de carreras receptoras FISEI,
que también pueden utilizarse como origen interno. Mostrar FISEI como dato fijo,
sin pedir al administrador que lo escriba ni permitir seleccionar otra facultad.
Cambiar el texto «carreras de cada facultad» por «carreras de FISEI».

No mezclar carreras externas en ese catálogo. Su procedencia se representará
separadamente cuando exista el contrato acordado para datos de origen. No crear
en esta corrección un CRUD de universidades, un módulo de solicitudes ni el
flujo de asignación de docentes de sprints posteriores.

## Contrato técnico mínimo propuesto para acordar con Leslie

1. Conservar rutas, UUID, roles JWT, estados HTTP, DTO de respuesta y el campo
   JSON `facultad`. No eliminar ahora la columna ni sus claves normalizadas.
2. Conservar inicialmente `facultad` obligatorio en la solicitud HTTP existente;
   el frontend lo enviará automáticamente con valor canónico `FISEI`.
   No es una credencial ni un secreto.
3. Validar la restricción también en el backend al crear y actualizar. Valor
   distinto de FISEI: HTTP 400 con error claro. No confiar en un campo oculto
   del navegador ni sustituir silenciosamente una facultad no permitida.
4. Usar una única política de dominio para validar y canonizar FISEI, admitiendo
   diferencias de mayúsculas y espacios. Confirmar con Leslie el tratamiento
   del nombre completo si ya aparece en datos reales; no admitir alias arbitrarios.
5. Mantener la protección por duplicado existente. Al ser fija la facultad,
   dos carreras con el mismo nombre normalizado colisionan en FISEI.
6. No modificar el cálculo de equivalencia, las validaciones de antigüedad ni
   los mínimos de cambio por este ajuste de catálogo.

Este cambio conserva la forma JSON, pero restringe valores antes admitidos.
Leslie confirmó el acuerdo con Shirley y autorizó su implementación el 20/10/2026.
Esto registra una decisión de alcance del equipo; no equivale a aceptar funcionalmente
el código nuevo ni a una disposición normativa institucional.

Implementación: crear/editar solo admite `FISEI` (caso y espacios externos
normalizados). El nombre completo no se acepta como alias sin un acuerdo específico.
Los registros anteriores se siguen consultando con su facultad original; la UI
los identifica como pendientes de revisión y bloquea su edición para evitar
reasignarlos silenciosamente. No se ejecuta una conversión masiva ni migración SQL.

## Datos y migración

Antes de aplicar la restricción, Leslie debe inventariar las facultades ya
guardadas y distinguir datos sintéticos, nombres equivalentes de FISEI y datos
que realmente pertenecen a otra facultad. No borrar carreras ni reasignarlas
masivamente, pues tienen asignaturas y UUID que deben preservarse.

La demostración actual contiene una carrera etiquetada «Software DEMO» con
facultad sintética. Su ajuste requiere un paso explícito y controlado para
alinearla con FISEI; este documento no modifica ese registro. No se necesita
una migración de esquema si se conserva el modelo actual, pero sí un plan de
corrección de datos acordado y respaldado antes de ejecutar cambios sobre datos
reales. Una futura restricción SQL debe planificarse con los datos existentes.

## Archivos afectados y coordinación

| Responsable | Archivos o módulos previstos |
|---|---|
| Leslie | `Catalogo.Domain/Carrera.cs`, casos de uso y validaciones HTTP del catálogo; pruebas de HU-04 |
| Leslie | `frontend/src/features/catalogo/CarrerasPage.tsx`, textos, formulario y pruebas; E2E de catálogo |
| Edwin, coordinado con Leslie | Comprobar que HU-06 conserva las relaciones de asignatura/unidad; acordar cómo HU-16 recibe procedencia verificable, sin cambiar sus mínimos por esta propuesta |
| Pablo | Revisar compatibilidad con Gateway y consumidores; ejecutar pruebas de integración y actualizar la demostración tras integrar el cambio |
| Shirley | Registrar la decisión de alcance, actualizar criterios y verificar la aceptación |

No se prevé modificar `App.tsx`, `Program.cs`, la solución, YARP ni agregar
dependencias. Si la implementación lo necesita, coordinar antes de editar.

## Pruebas y criterios de aceptación propuestos

Escribir primero pruebas fallidas para las nuevas restricciones y después
implementar el cambio:

- Crear una carrera sin escribir facultad en la UI: se guarda con FISEI.
- Editar una carrera: la facultad se muestra fija y no se puede cambiar.
- Enviar otra facultad directamente por API: HTTP 400; no se persisten cambios.
- Crear o editar con FISEI: conserva los estados HTTP y contratos vigentes.
- Nombre duplicado normalizado en FISEI: HTTP 409, sin duplicación.
- Intentar eliminar una carrera con asignaturas: continúa prohibido.
- Login, rutas protegidas, consulta de asignaturas y HU-14 no tienen regresiones.
- Los registros existentes se preservan y cualquier corrección queda registrada.

En pruebas futuras de procedencia: interna exige origen FISEI-UTA y externa
exige otra universidad ecuatoriana; datos ausentes no permiten inferir el tipo.
Estas pruebas requieren acordar primero el contrato de procedencia. No se
presentan como endpoints ya implementados ni se añaden a HU-16 sin coordinación.

## Discrepancia documental y alcance aclarado

`docs/contexto-proyecto.md` todavía contempla instituciones extranjeras.
La aclaración expresa de Pablo limita el origen externo a otras universidades
del Ecuador; la ambigüedad de su solicitud queda resuelta. No se requiere una
nueva decisión de Pablo sobre el significado de «externa».

Queda coordinar con Shirley y Edwin la actualización de la documentación
histórica sobre instituciones de otros países y apostilla. Esas referencias
no deben activar requisitos para universidades ecuatorianas por el solo hecho
de ser externas a la UTA. Este documento no cambia todavía los archivos
compartidos ni las validaciones de Edwin, ni presenta esta definición de alcance
como normativa oficial.

## Texto listo para compartir con el equipo

> Solicito ajustar HU-04 / Issue #2 para que el catálogo administrado represente
> exclusivamente carreras receptoras de FISEI-UTA. La facultad debe mostrarse
> fija y validarse en backend; se propone conservar el campo JSON para evitar
> una ruptura estructural del contrato. El origen interno también será FISEI;
> el externo corresponderá a otra universidad ecuatoriana y no se mezclará en
> este catálogo. Leslie: revisar implementación, compatibilidad y datos
> existentes. Edwin: revisar impacto de procedencia en sus módulos sin modificar
> sus mínimos académicos. Shirley: registrar el alcance aclarado por Pablo
> (externa significa otra universidad del Ecuador), conciliar las referencias
> anteriores a instituciones de otros países y actualizar criterios. Pablo verificará
> integración y demostración. No borrar datos ni cambiar contratos antes del
> acuerdo. Criterios y pruebas detallados en SC-01.

La propuesta original fue publicada por autorización de Pablo. La confirmación
posterior de Leslie permite implementar este ajuste del catálogo y actualizar
sus criterios. La procedencia en HU-16 y los archivos de Edwin siguen sujetos
a coordinación. No se han enviado mensajes personales a los integrantes.
