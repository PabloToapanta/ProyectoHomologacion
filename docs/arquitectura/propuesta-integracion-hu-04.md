# Propuesta de integración HU-04 / Issue #2

19/10/2026. Responsable: Leslie. Revisión propuesta: Pablo. **Pendiente de acuerdo; no se ha enviado ningún mensaje ni modificado archivos de Pablo.**

## Contrato implementado del catálogo

- Rutas existentes propuestas: `GET/POST /api/carreras`, `PUT/DELETE /api/carreras/{id}`. Se añade `GET /api/carreras/{id}` para consulta individual y cabecera Location de creación.
- `id`: UUID, generado por el dominio; JSON camelCase.
- Entrada: `{ "nombre": "Carrera sintética", "facultad": "Facultad de prueba", "vigencia": true, "modalidad": null }`.
- Nombre/facultad obligatorios, máximo 200 caracteres cada uno; modalidad opcional máximo 100. Vigencia booleana obligatoria, representa estado vigente/no vigente, sin fechas académicas ni un segundo campo estado redundante.
- Se conserva la escritura del nombre y facultad, normalizando Unicode FormKC y espacios; comparación única por claves en mayúsculas invariantes. Acentos distintos permanecen distintos. Índice SQL Server con collation binaria sobre las claves normalizadas, independiente de la collation predeterminada del servidor.
- Respuesta de listado: arreglo (sin paginación para este incremento). Creación 201 + DTO + Location, edición 200 + DTO, eliminación 204.
- Errores ProblemDetails: 400 validación, 404 inexistente, 409 duplicado/asignaturas relacionadas. `code` para errores de negocio; errores de binding usan ValidationProblemDetails con `errors`.
- Todas las operaciones exigen JWT HS256 válido y rol `Administrador`, siguiendo los claims `role`/`name` de HU-01. El servicio valida JWT directamente además del Gateway.

## Cambios propuestos para Pablo (no aplicados)

1. Agregar una ruta `/admin/carreras` bajo `ProtectedRoute` con rol Administrador e importar `CatalogoRoute` desde `features/catalogo/CatalogoRoute`. El adaptador usa `useAuth().request` de la API pública ya disponible.
2. Agregar un enlace «Carreras» al panel. Conservar la cabecera/cierre de sesión del diseño compartido; `CarrerasPage` aporta el contenido principal.
3. Añadir en YARP el cluster catálogo a `http://catalogo-api:8080` (contenedores) o `http://127.0.0.1:5082` (host), con match `/api/carreras/{**catch-all}` y política Administrador. Verificar también `/api/carreras` sin sufijo.
4. Incorporar los cuatro proyectos y las pruebas a `Homologacion.sln` y a CI cuando se revise el PR. Hasta entonces ejecutar `src/catalogo-service/Catalogo.sln`.
5. Confirmar en conjunto nombres JSON, vigencia booleana, UUID, rol y puerto 5082. Ejecutar recorrido login → carreras → CRUD por Gateway.

Los archivos App.tsx, Auth, estilos compartidos, package.json/lockfile, Vite y Gateway permanecen intactos en este trabajo. No se afirma coordinación externa completada.

## Continuidad para Leslie (HU-05) y Edwin (HU-06)

`Carrera` es la entidad de dominio pública con UUID estable. `CatalogoDbContext` y repositorio permanecen en Infrastructure; los casos de uso dependen de `ICarreraRepository` en Application.

La tabla `Asignaturas` tiene únicamente `Id` y `CarreraId` como ancla de integridad referencial. Su clase de persistencia `AsignaturaReferencia` es interna: **no es la entidad académica de HU-05**, no tiene CRUD, UI ni campos de sílabo. La FK usa NO ACTION/Restrict y evita carreras huérfanas incluso ante inserciones concurrentes con una eliminación. Las pruebas insertan relaciones sintéticas directamente para verificar el criterio sin adelantar HU-05.

Al implementar HU-05, reemplazar esa clase interna por el mapeo de la entidad `Asignatura` sobre la misma tabla, preservando IDs y FK, y generar una migración aditiva con sus campos obligatorios. Si ya existen referencias de prueba en un entorno compartido, acordar limpieza o backfill antes de añadir NOT NULL; nunca completar datos académicos inventados. Edwin añadirá UnidadCurricular y Contenido después del contrato de Asignatura; no se han implementado esas entidades aquí.

## Verificación de integración pendiente

- Revisión cruzada de Pablo y aceptación funcional de Shirley.
- Conexión real SQL Server y `docker compose up` con motor Docker disponible.
- Aplicar migraciones, validar duplicados y FK bajo SQL Server, comprobar concurrencia real.
- Login y navegación por Gateway en navegador de escritorio y móvil tras aprobar la ruta.
