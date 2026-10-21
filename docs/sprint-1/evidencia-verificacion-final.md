# Verificación técnica final disponible — Sprint 1

Fecha: 21/10/2026 (America/Guayaquil). Responsable: Pablo.
Rama: `feature/hu-06-verificacion-final`, desde `origin/develop` `34bcc67`.
Issues relacionados: HU-01/04/05/06/14/15/16 (#1–#7), entorno #14 y CI #15.
Estado al finalizar la verificación: corrección local pendiente de publicación,
revisión y aceptación de Shirley. Durante esa verificación no se hicieron commits,
push, merge ni cambios externos en Issues/reviews. Pablo autorizó posteriormente
publicar la rama y abrir el PR con revisión de Edwin; la aceptación sigue pendiente.

## Corrección del smoke

HU-06 conserva el DTO de listado y amplía el detalle con `unidades`. La igualdad
completa entre esos DTO en el smoke fallaba aun con HTTP 200. Se extrajo la
comprobación conservando primero la comparación anterior y se escribieron siete
pruebas de regresión: tres fallaron antes de corregir (detalle con unidades vacías,
detalle con unidades registradas y ausencia de la colección).

Ahora se comparan todos los campos presentes en el listado y se exige una lista
`unidades` en el detalle. Cambios de identidad, datos académicos o campos comunes
ausentes siguen produciendo un fallo; no se silencian discrepancias para que pase
la prueba. Las siete pruebas pasan después de la corrección.

El smoke informa cantidades de asignaturas/unidades leídas y advierte cuando el
entorno no permite demostrar unidades registradas. No escribe datos académicos.
Su verificación de lecturas no sustituye registro/edición ni validación del programa.
El workflow de homologación incorpora estas pruebas Python con biblioteca estándar
y se activa también ante cambios en `tests/sprint-1` y el Compose integrado.
El cambio de workflow está preparado; todavía no se afirma ejecución de CI remoto
de esta rama sin publicar.

## Resultados reales

| Comprobación | Resultado |
|---|---|
| Restore y build Release de solución principal y catálogo | Correctos, ambos builds con 0 advertencias y 0 errores |
| Solución principal .NET | 308 aprobadas |
| Catálogo .NET | 72 aprobadas |
| Total .NET | **380 aprobadas, 0 fallidas y 0 omitidas** |
| TypeScript y Vite | Correctos |
| Frontend Vitest | **89 aprobadas**, 11 archivos |
| Regresión Python del smoke | **7 aprobadas**, sin dependencias nuevas |
| E2E Auth, escritorio y móvil | **2 aprobadas** |
| Comprobaciones adicionales de navegador HU-06 y pantallas HU-14/15/16 | **2 aprobadas** tras ajustar el código de asignatura usado por el diagnóstico |
| Smoke real final por Gateway | Correcto: 3 carreras, 2 asignaturas, 4 unidades consultadas |
| Swagger desplegado | Incluye unidades y validaciones |
| Compose | Seis servicios en ejecución, SQL saludable |

Las dos comprobaciones adicionales de navegador usan un archivo temporal y no
se incorporan como suite nueva a este PR. La primera ejecución de HU-06 usaba el
código fijo de asignatura de la auditoría anterior y agotó el tiempo al buscarlo.
Se corrigió el selector para usar el código de la demo recién preparada y pasó;
no se cambió el producto para resolver ese fallo del diagnóstico. La comprobación
de pantallas fue correcta en esa primera ejecución. Se conservan ambos logs.

El smoke real comprueba login, error de credenciales, JWT ausente/inválido, listado
y detalle de carreras/asignaturas, rechazo de otra facultad, cinco casos HU-15,
siete casos HU-16, errores 400 y equivalencia con evidencia sintética. HU-14 conserva
Homologable con tres criterios al 80 % e InformacionIncompleta con múltiples orígenes
sin regla de agregación de horas/créditos. La UI adicional comprobó el resultado
positivo de equivalencia, antigüedad y requisitos sin mocks.

## Actualización de la demostración y datos

Se conservó el proyecto `fisei-uta-demo`, el volumen `fisei-uta-demo_auth-data`,
las bases `HomologacionAuth` y `HomologacionCatalogoDemo` y las credenciales privadas.
Antes de la migración se realizaron respaldos SQL con CHECKSUM y RESTORE VERIFYONLY
de ambas bases. Las copias locales están en `/tmp/fisei-respaldo-cierre-20261021`,
con directorio 700 y archivos 600. No se versionan ni se afirma un ensayo de restore.

Se registró el inventario previo y se compararon los datos tras la migración:
**las 2 carreras y 1 asignatura previas permanecieron idénticas**; las tablas nuevas
de unidades estaban inicialmente vacías. No se reasignaron facultades ni se
eliminaron relaciones. El primer build quedó bloqueado por conexión al registro
de Microsoft; el reintento completó la actualización de los seis servicios. El
cliente del intento bloqueado se finalizó sin detener los contenedores actualizados.

Después se prepararon exclusivamente nuevos datos de demostración, sin sobrescribir:

- Carrera `Software FISEI — Demostración Sprint 1`, facultad FISEI.
- Asignatura Álgebra Lineal, código sintético `DEMO-ALG-LIN-S1`, nivel 1,
  3 créditos, 144 horas.
- Cuatro unidades de 36 horas, con resultado de aprendizaje y dos contenidos cada una,
  usando la transcripción publicada por Edwin en `examples/hu-06`.

Resultado final: **3 carreras, 2 asignaturas y 4 unidades**. Se comprobó que los
registros previos conservan todos sus campos y que repetir la preparación no crea
duplicados ni sobrescribe datos distintos. La UI consultó las cuatro unidades y
guardó una edición con los mismos datos en la nueva asignatura de demostración.
La exactitud de la transcripción frente al PDF fuente debe revisarla el equipo/PO;
no se inventaron temas ni se realizó carga automática de PDF.

La demo queda activa en <http://127.0.0.1:5173> con las mismas credenciales. Los datos
históricos con otra facultad siguen pendientes de revisión explícita, sin conversión.

## Documentación y pendientes humanos

Se actualizaron README, guía de ejecución, comentario del Compose y referencias
obsoletas de AGENTS sobre HU-14/T3. Solo se sincronizan aclaraciones e implementación
ya acordadas; no se cambian reglas académicas, contratos JSON, arquitectura ni
las clases de Edwin/Leslie.

El [recorrido de aceptación preparado](preparacion-aceptacion-sprint-1.md) deja todas
las decisiones de Shirley pendientes. Falta:

1. Publicar y revisar estos cambios antes de integrarlos a develop.
2. Registrar las revisiones cruzadas pendientes de PR #27/#29 o enlazar la evidencia
   real si se realizaron fuera de GitHub; no crear aprobaciones por cuenta ajena.
3. Shirley valida los criterios y el programa analítico, registra aceptación y
   actualiza/cierra los Issues cuando corresponda. Los Issues históricos de HU-01
   y HU-14 requieren sincronización con T3 y tres criterios, respectivamente.
4. Registrar Sprint Review, retrospectiva y estado final del backlog cuando ocurran.

No se declara cerrado el Sprint por estas pruebas. Las agregaciones/conversiones
académicas sin regla aprobada siguen devolviendo información incompleta, como exige
el contrato; no se añadieron reglas, IA, PDF ni funcionalidades de otros sprints.

Evidencias: [verificacion-final](evidencias/verificacion-final/), con resultados,
TDD rojo/verde, smoke, navegador, capturas e inventario resumido. Los respaldos,
inventario detallado y credenciales permanecen fuera de Git.
