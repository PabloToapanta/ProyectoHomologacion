# HU-06 / Issue #4 — unidades y contenidos

Implementación local en `feature/hu-06-unidades-contenidos`, base `aeb111f`.
No se han realizado commits, push, PR ni aceptación de la Product Owner.

## Contrato e integración

Se amplía el catálogo existente, sus cuatro capas y su propia base. No se cambia
Homologación, Gateway, autenticación ni el arranque de React. La ruta POST ya
estaba prevista en Gateway. Integración concreta con archivos del catálogo:
registro de UnidadService/IUnidadRepository en Program.cs, mapeo EF en el DbContext,
detalle de asignatura ampliado y botón en AsignaturasPage. No se cambian DTO de
registro ni listado de asignaturas y se conservan sus campos de consulta.

- POST `/api/asignaturas/{id}/unidades`: número positivo, nombre (200), horas
  enteras positivas, resultado de aprendizaje (4000), contenidos (1–200 textos,
  máximo 4000 caracteres cada uno). Orden de contenidos según la entrada.
- GET `/api/asignaturas/{id}`: conserva los campos anteriores y añade `unidades`,
  ordenadas por número, con contenidos ordenados y resultado de aprendizaje.
- Administrador obligatorio; 400 para datos incompletos/incorrectos, 404 para
  asignatura inexistente, 409 para número de unidad repetido en una asignatura.
- UUID estables para unidades y contenidos; FK a asignatura y unicidad respaldadas
  por la base. La migración añade tablas sin modificar carreras/asignaturas existentes.
- Se permite cualquier número positivo de unidades; no se impone el ejemplo de
  cuatro unidades a todas las asignaturas. No se inventa una regla de suma de horas.

La pantalla se abre desde Catálogo → Asignaturas → Ver unidades de la asignatura.
Incluye consulta, registro, errores y actualización. La edición se incorporó después
por solicitud de Edwin para corregir errores manuales; eliminación no incluida.

## Ampliación: edición y usuario de prueba

PUT `/api/asignaturas/{id}/unidades/{unidadId}` permite corregir los mismos campos
del registro. Comprueba pertenencia a la asignatura, permisos, campos obligatorios
y número duplicado. Conserva ID de unidad e IDs de contenidos retenidos por posición;
elimina los contenidos retirados e incorpora los nuevos en una única transacción EF.
No requiere otra migración: utiliza las tablas HU-06 ya creadas.
La UI ofrece Editar unidad, Guardar cambios y Cancelar edición; explica que cada
línea representa un contenido. La ruta PUT se incorpora a Gateway y Swagger.

Tras la prueba manual de Edwin, el formulario de edición se trasladó al interior
de la tarjeta seleccionada. Durante la edición se oculta el formulario de alta y
se bloquea editar otras unidades o refrescar el listado para no reemplazar cambios
pendientes. Los errores se muestran en el propio formulario. Cancelar conserva los
datos guardados y no envía peticiones. Prueba de interacción cubre ubicación dentro
de tarjeta, cancelación y guardado PUT. Frontend: 87 pruebas aprobadas.

Prueba previa: PUT válido devolvía 404. Después de implementar, la suite del catálogo
pasó (72 pruebas), incluida edición persistida, validación, duplicados, pertenencia
y permisos. Auth pasó 44 pruebas, incluida cuenta sintética con correo y password
local. La cuenta real de prueba solicitada se configura solo en .env ignorado por Git,
sin publicar el correo ni contraseñas en ejemplos. La contraseña no es la institucional.
La semilla la crea al reiniciar Auth y conserva las cuentas anteriores. No hay SSO
ni validación de propiedad/dominio de correo; esos cambios requieren acuerdo HU-01.

Para verificar en Docker tras abrir Desktop:

```powershell
docker compose --env-file .env -f compose.sprint1.yaml up -d --build
```

Utilizar como usuario el correo configurado en AUTH_ADMIN_USERNAME y como contraseña
el valor privado existente de AUTH_ADMIN_PASSWORD. No se ejecutó este despliegue:
Edwin detiene Docker durante el trabajo. La prueba final en navegador/SQL sigue pendiente.

## Datos de referencia y carga manual (T15)

Fuente local consultada: `PlanesAnaliticos_SW_Jul26_Dec26.zip`, documento
`PLAN ANALÍTICO/PlanAlitico ALGEBRA LINEAL.pdf`, sección III, página 5.
El JSON de `examples/hu-06` transcribe las cuatro unidades de 36 horas, los dos
bloques de contenidos mínimos de cada una y su resultado. El nombre de cada unidad
usa la primera unidad temática de su fila; no se divide cada frase en temas inferidos.
Revisar la transcripción con el programa autorizado antes de publicar los datos.
No se copia el PDF ni datos personales al repositorio.

Con el stack actualizado y un JWT administrador en CATALOGO_DEMO_TOKEN:

```powershell
./src/catalogo-service/examples/hu-06/cargar-unidades.ps1 -AsignaturaId UUID-ALGEBRA -Archivo ./src/catalogo-service/examples/hu-06/algebra-lineal-unidades.json
```

La carga usa la API; omite unidades idénticas y se detiene ante conflictos, sin
sobrescribir. No es una importación automática de PDF ni se ejecuta al arrancar.
Si falla a mitad, las unidades ya creadas permanecen; se puede volver a ejecutar.

## Verificación efectivamente ejecutada

- Catálogo: 72 pruebas aprobadas (71 existentes y un escenario integrado HU-06).
  Se comprueban registro, consulta anidada, contenidos/resultado, duplicado,
  datos incompletos, asignatura inexistente y protección 401/403.
- Frontend: 86 pruebas aprobadas, incluida consulta/registro desde el panel nuevo.
- TypeScript y build Vite correctos.
- Migración EF Core 8 generada junto con snapshot y script SQL idempotente.
- Pruebas HTTP del catálogo con SQLite, no con SQL Server.

La primera ejecución intentó utilizar .NET 10 y falló por incompatibilidad del
TestHost (PipeWriter); no constituye evidencia de TDD fallida del comportamiento.
Se descargó ASP.NET Runtime 8.0.32 en una carpeta temporal y se ejecutó la suite
con ese runtime. EF Core 8 generó la migración usando roll-forward para su herramienta.

Pendientes: aplicación de migración en SQL Server, carga T15 real, recorrido de
navegador contra el stack actualizado, revisión cruzada y aceptación de Shirley.
No se declara cumplida la definición de hecho por las pruebas locales.
