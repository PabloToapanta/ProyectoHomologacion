# HU-14 — Comparación con selección del catálogo

## Ampliación solicitada el 21/10/2026

El usuario autoriza trasladar metadatos y correspondencias al catálogo y cargar una demostración sintética para cinco carreras. Propuesta de cambio sobre propiedad de Leslie/Edwin: módulos nuevos de perfil y revisión en las cuatro capas de Catálogo, dos tablas nuevas con migración aditiva, integración mínima en DbContext/Program y dos rutas nuevas de Gateway. No alterar datos históricos ni borrar carreras anteriores. Frontend del catálogo permite editar el perfil y revisar correspondencias por par una sola vez. HU-14 conserva el motor y su API.

GET/PUT `/api/asignaturas/{id}/perfil-comparacion`: planAnalitico, unidadHoras, sistemaCreditos, esSintetico. GET/PUT `/api/asignaturas/{id}/correspondencias/{receptoraId}`: listas explícitas de vínculos de contenidos/resultados con evidencia. Una revisión almacena huellas de los programas; cambiar inventarios o perfiles la invalida hasta revisión. Sin revisión no se infiere cero ni coincidencias por texto. El catálogo sigue restringido a Administrador, conforme al contrato actual.

La pantalla simple compara programas bajo el escenario de origen aprobado; no registra ni acredita aprobación de un estudiante. Se comunica esta condición y se conservan HU-15/HU-16 independientes. Las asignaturas DEMO, sus planes y correspondencias son enteramente sintéticos, autorizados como datos de prueba; no son sílabos ni equivalencias oficiales. El cargador es explícito e idempotente y no actualiza datos históricos ajenos a la demostración.

Solicitud del usuario del 21/10/2026. Issue: https://github.com/PabloToapanta/ProyectoHomologacion/issues/5 (T9/T10).

Propuesta concreta para revisión de Pablo y coordinación con Leslie/Edwin: consumir desde el comparador los GET existentes de carreras, asignaturas por carrera y detalle con unidades. No modificar archivos del catálogo, Gateway, composición ni modelos compartidos. El motor sigue recibiendo instantáneas por el contrato vigente, sin acceder a la base de otro servicio.

La selección carga UUID, horas, créditos, contenidos por UUID y resultados por UUID de unidad curricular. Cada resultado corresponde a una unidad curricular, aunque su texto coincida con otro. `contenidos` del DTO de unidad ya contiene todas las temáticas: no concatenar otra vez sus contenidos. Sin unidades, los inventarios se consideran desconocidos, no vacíos acreditados.

El catálogo no contiene referencia al plan, unidad horaria, sistema de créditos ni aprobación del estudiante. Se mantienen como confirmaciones explícitas del usuario. No inferirlas por pertenecer a FISEI. Conservar entrada manual para orígenes externos sin catálogo y consumidores Docente/Coordinador sin permiso de lectura del catálogo actual (GET restringidos a Administrador). No ampliar permisos en esta tarea.

Criterios de esta adaptación: seleccionar carrera y materia para cada extremo; cargar cantidades e inventarios sin reescritura; invalidar datos y correspondencias al cambiar selección; impedir comparar una materia consigo misma; errores de carga recuperables; mantener evidencia explícita y los tres criterios actuales con barras accesibles. El umbral mostrado es el devuelto por el servidor, no un valor editable que la API ignora.

La imagen aportada es una referencia de interfaz histórica con cuatro filas; rige la aclaración local de tres criterios. No se cambian las reglas ni se añaden coincidencias automáticas.

La implementación y las pruebas técnicas no constituyen aceptación de Shirley ni revisión cruzada. La verificación integrada con datos reales y Docker Compose se registrará solo si se ejecuta.
