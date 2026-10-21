# HU-06 / Issue #4: edición de unidades

Solicitud de Edwin: corregir unidades y contenidos antes de HU-08.
Extensión aditiva del contrato: PUT /api/asignaturas/{id}/unidades/{unidadId},
Administrador; mismo cuerpo de creación. Conserva ID de unidad y valida que
pertenezca a la asignatura. Número único por asignatura. Contenidos actualizados
por posición; los nuevos reciben UUID y los retirados se eliminan.
La edición completa es atómica. No se añade carga masiva ni se cambia el motor.
Único cambio en Gateway: ruta PUT nueva con AcademicAccess y destino catálogo,
siguiendo la ruta POST existente. No se cambia App.tsx ni Program.cs.

Login de prueba: Auth ya admite un correo como NombreUsuario. Se configurará
AUTH_ADMIN_USERNAME en el .env local con el correo proporcionado por Edwin;
se conserva la contraseña privada. No se añade SSO, no se verifica propiedad del
correo y no se exige dominio a los demás usuarios existentes. El ejemplo público
permanece sintético. La semilla crea una nueva cuenta administradora al reiniciar
Auth, sin renombrar ni eliminar admin.demo.
