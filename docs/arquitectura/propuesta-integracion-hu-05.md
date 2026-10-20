# Integración HU-05 / Issue #3 — propuesta para Pablo y Edwin

19/10/2026, Leslie. Se verificó en GitHub que Pablo aprobó el PR #19 y que HU-04 está fusionada en develop (`d6c2246`). HU-05 se inició desde ese develop actualizado, sin copiar ni duplicar los commits de HU-04. No se han enviado mensajes externos ni se afirma acuerdo sobre los nuevos contratos.

## Catálogo y contratos implementados

- Mismo `catalogo-service`, cuatro capas, base exclusiva y DbContext; ninguna dependencia hacia auth-service u homologacion-service.
- `Asignatura`: UUID, carreraId, codigo, nombre, nivel entero positivo, creditos decimal positivo de hasta dos decimales, horas enteras positivas. Límites de almacenamiento: código 50, nombre 200, créditos decimal(9,2), nivel/horas int. No son límites normativos.
- Código único por carrera, normalización FormKC/espacios/mayúsculas invariantes e índice único. No se impone unicidad por nombre. Se puede repetir código en otra carrera. No se añade una prohibición de carreras no vigentes que los criterios no establecen.
- POST `/api/asignaturas`: 201 + DTO con id + Location; 400 validación, 404 carrera inexistente, 409 código repetido.
- GET `/api/asignaturas/{id}`: 200/404.
- GET `/api/carreras/{id}/asignaturas`: 200 con arreglo ordenado por nivel/código; [] cuando existe la carrera sin asignaturas; 404 si no existe.
- Rol Administrador en las tres operaciones. JSON camelCase y ProblemDetails; el JWT comparte emisor/audiencia/clave con HU-01 vía configuración, sin secretos en Git.
- No se implementa Malla versionada, edición/borrado de asignaturas ni HU-06. El listado por carrera satisface la consulta por nivel de HU-05.

## Migración desde HU-04

`AsignaturaReferencia` deja de existir como clase interna y su tabla pasa a estar mapeada por `Catalogo.Domain.Asignatura`. La migración inicial publicada se conserva. La nueva añade campos, índice y restricciones CHECK, sin borrar/recrear la tabla ni cambiar IDs o FK.

Las filas antiguas solo tenían Id/CarreraId. Si hay alguna, la migración se detiene con THROW 51000 antes de modificar el esquema. Se requiere acordar una migración de datos con información real o el tratamiento explícito de referencias sintéticas. No se introducen nombres/códigos/horas inventados ni se eliminan filas automáticamente. El rollback de la migración elimina las nuevas columnas: no ejecutarlo en entornos con datos académicos sin una estrategia de respaldo acordada.

Las pruebas de eliminación restringida de HU-04 ahora crean una asignatura completa y mantienen las mismas expectativas. `CarreraRepository.TieneAsignaturasAsync` consulta el DbSet real.

## Pablo: cambios compartidos pendientes

Después de la implementación inicial, Leslie autorizó explícitamente realizar la integración compartida y tomar estas decisiones. Se añadió `/admin/carreras` en App.tsx bajo el ProtectedRoute Administrador existente y un enlace «Gestionar carreras y asignaturas» en AdminPanel. CatalogoRoute incluye navegación Carreras/Asignaturas, cabecera compartida, retorno al panel y cierre de sesión mediante la API pública de Auth. No se modificaron la lógica de autenticación, estilos compartidos ni configuración común. No se presume aprobación posterior de Pablo ni aceptación de Shirley de estos cambios nuevos.

Las tres rutas HU-05 ya figuran en el Gateway existente: verificar su destino antes de crear reglas duplicadas. Sigue pendiente GET `/api/carreras/{id}` de HU-04; la carga de demostración lo utiliza. Acordar la política AcademicAccess del Gateway frente al requisito Administrador de la API (esta última sigue rechazando otros roles).

Compose del catálogo tiene servicio `catalogo-api`, puerto host 5082 y variables `JWT_*`; Auth/Gateway usan `AUTH_JWT_*` y el ejemplo de destino `catalogo-service`. Revisar nombre DNS/red compartida, valores JWT equivalentes y composición integrada; no se modificaron esos servicios. Mantener los proyectos añadidos por HU-14 cuando se incorporen los del catálogo a Homologacion.sln.

## Edwin: base para HU-06

Usar `Asignatura.Id` como FK de UnidadCurricular y añadir Contenido con su propia relación, sin cambiar UUID ni recrear Asignaturas. Sus casos de uso e interfaces van en Application, reglas en Domain y mapeos en Infrastructure. Acordar el DTO ampliado antes de añadir unidades a GET `/api/asignaturas/{id}`.

El dato T8 es Álgebra Lineal, nivel 1, 3 créditos, 144 horas, con código DEMO sintético. Requiere Software existente. Las cuatro unidades de 36 horas y sus temas/resultados corresponden a T15: el contenido debe proceder del programa analítico del equipo.

## Pendientes de aceptación

Pruebas reales SQL Server/migraciones/concurrencia, Compose integrado, recorrido de navegador, revisión cruzada de Pablo, CI remoto de HU-05 y aceptación de Shirley. No se cierra el Issue #3 por tener código o pruebas locales.
