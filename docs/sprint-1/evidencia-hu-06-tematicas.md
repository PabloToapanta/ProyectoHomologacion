# HU-06 — unidades temáticas

Aclaración del docente transmitida por Edwin el 21/10/2026. Issue #4.
La unidad curricular contiene temáticas con sus contenidos mínimos. Las horas y
el resultado de aprendizaje se guardan una sola vez en la curricular.

## Implementación

- Nueva tabla UnidadesTematicas, orden y nombre, FK a unidad curricular.
- Contenidos conserva UUID, orden y texto; añade FK temática nullable.
- Migración aditiva sin UPDATE, DELETE ni clasificación supuesta de registros anteriores.
- API POST/PUT existentes aceptan tematicas: [{nombre, contenidos: [texto]}].
  GET conserva contenidos plano y añade tematicas y contenidosSinClasificar.
- Cuerpo anterior contenidos[] sigue admitido para consumidores históricos. No se
  permite mezclar los dos formatos ni editar una unidad ya clasificada con formato antiguo.
- UI permite añadir/quitar temáticas y editar su nombre/contenidos. Al editar registros
  anteriores se muestran los textos y nombre temático vacío para clasificación humana.
- Álgebra Lineal: referencia JSON actualizada con las dos unidades temáticas por
  curricular transcritas de la página 5 del programa ya consultado. El cargador no
  reclasifica ni sobrescribe registros existentes; exige revisión manual ante diferencias.
- IDs temáticos y de contenidos se conservan por posición durante edición. Esta edición
  no mantiene historial/versionado de contenidos; no reutilizar sus IDs como prueba de
  inmutabilidad del plan académico en futuros módulos.

## Verificación

Prueba previa HTTP: 400 al enviar temáticas. Después: suite catálogo 74 aprobadas,
incluye registro/consulta jerárquica, edición, reducción y ampliación de grupos,
rechazo de datos incompletos y conservación de contenidos anteriores sin inferir grupo.
SQLite comprueba persistencia y relaciones, no ejecución de migración SQL Server.
Frontend: formularios de varias temáticas, registro, edición y cancelación.
EF Core genera migración y script SQL. Revisar/aplicar en SQL Server y comprobar en
Docker antes de aceptar HU-06. No se altera el volumen ni se despliega automáticamente.

Antes de actualizar una base existente, conservar respaldo y verificar tras migrar
que los contenidos previos permanecen con UnidadTematicaId NULL. Clasificarlos desde
Editar unidad usando el plan autorizado; no eliminar datos para forzar la carga demo.
Aceptación de Shirley y revisión cruzada pendientes. No se cierra el Issue #4.
