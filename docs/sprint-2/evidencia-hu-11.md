# HU-11 — asignaturas aprobadas / Issue #39

Rama `feature/hu-11-asignaturas-aprobadas`, creada desde `2a7d543` de `feature/hu-10-registro-solicitudes` porque HU-10 depende del PR #45, todavía abierto al preparar esta entrega. Incluye T28 (API) y T29 (interfaz). Preparada para publicación y revisión mediante un PR hacia `develop`; sin revisión cruzada ni aceptación de la PO. Antes de integrar se debe sincronizar la rama con `develop` una vez incorporada HU-10.

Alcance contrastado con el Sprint 0: el backlog pide "nota, fecha, créditos, horas y temas" y la entidad incluye asignatura de origen y programa analítico adjunto. T28 del Sprint 2 solo nombraba nota, fecha y matrícula; esta versión recupera lo que puede cubrirse sin carga de PDF.

## Decisiones transmitidas por el usuario el 01/11/2026

Pendientes de confirmación de Shirley como PO; no se presentan como normativa oficial aportada.

- **Matrícula:** son dos datos: número de matrícula e intento (primera, segunda, tercera).
- **Notas FISEI:** de 0 a 10, aprobada desde 7,00; menor de 7, reprobada.
- **Notas externas:** otras universidades pueden usar otra escala y debe calcularse equivalencia.
- **Interno:** la asignatura se toma del catálogo FISEI.
- **HU-08:** se mantiene en Sprint 2 (no incluida en este cambio).

## Contrato

- `POST /api/solicitudes/{id}/asignaturas-aprobadas`, solo Coordinador: 201 con `Location` al detalle; 400, 401, 403, 404 y 503 si el catálogo no responde.
- `GET /api/solicitudes/{id}/asignaturas-origen`, solo Coordinador: asignaturas de la carrera de origen; vacío en cambios externos.
- `GET /api/solicitudes/{id}` añade `asignaturasAprobadas`. El listado no cambia.
- `GET /api/carreras-solicitudes/{id}/asignaturas` en Catálogo, solo Coordinador y de lectura.
- **Interno:** `asignaturaOrigenId`, `nota`, `fechaAprobacion`, `numeroMatricula`, `intentoMatricula`. Código, nombre, créditos y horas se copian del catálogo; datos manuales devuelven 400.
- **Externo:** además, `nombreAsignatura`, `creditos`, `sistemaCreditos`, `horas`, `unidadHoras`, `referenciaPrograma`, `notaMaximaEscala` y `notaMinimaAprobacion`.
- `estado` se deriva de la nota: `Aprobada` o `Reprobada`. No declara elegibilidad; aprobación y antigüedad siguen en HU-15.

## Límites y pendientes

- **Número de matrícula:** texto obligatorio de hasta 50 caracteres, sin validar forma: falta conocer su formato.
- **Conversión de notas:** no implementada. Se conserva la nota original con su escala. RN-10 remite a una tabla de equivalencias (CAU-P-122, Art. 19) que no está en el repositorio y no tiene HU en el backlog; no se inventa una conversión. Que la aprobación externa se determine con el mínimo de origen es una propuesta por confirmar.
- **Contenidos externos:** sin carga de PDF no hay datos estructurados: corresponde "información incompleta". `referenciaPrograma` es texto, no un adjunto.
- **Tercera matrícula:** `intentoMatricula` no resuelve RN-07. La asignatura perdida en tercera no está aprobada; el dato debe estar en la solicitud y se aplica en HU-17.
- **Unidad y sistema en interno:** quedan vacíos; el perfil de comparación del catálogo es su fuente.
- **Asignatura repetida en una solicitud:** se permite; no hay regla acordada. Sirve además para varios intentos, pero no se ha confirmado.
- **Fecha futura:** "hoy" se calcula con UTC-5 (Ecuador continental).
- **Períodos cursados y cambios previos:** los requieren HU-16 y las reglas del agente, y HU-10 no los registra. Fuera de este cambio.

## Verificación del 01/11/2026

Entorno: cambios locales sobre `2a7d543`, stack desechable `compose.sprint1.yaml` + `compose.hu10.yaml` con secretos generados y base vacía, y SDK 8.0.427 en contenedor porque el equipo no tiene runtime .NET 8. Datos sintéticos.

1. TDD: 33 pruebas backend fallaron por las causas esperadas antes de validar y añadir rutas; 6 de interfaz, antes del panel.
2. Con SQL Server real, sin omitidas: Solicitudes 75, Catálogo 77 y Gateway 75 aprobadas. Incluye migración y clave foránea contra asignaturas huérfanas.
3. `npx vitest run`: 108 aprobadas en 14 archivos. `npm run build` correcto.
4. Migración `RegistroAsignaturasAprobadasHu11` regenerada: solo crea la tabla, su clave foránea y su índice.
5. Flujo por Gateway con Catálogo real: 35 comprobaciones aprobadas. Límites 7,00 y 6,99, escala externa, asignatura ajena al origen, datos manuales en interno, rechazos sin escritura, 404, 401 y 403.
6. Persistencia tras reiniciar Solicitudes; rutas en Swagger; sin `fail` en Solicitudes ni Catálogo; sin contraseñas de la sesión en registros.
7. `tests/sprint-1/smoke.py` con `PYTHONUTF8=1`: salida 0, 9 líneas PASS.
8. Navegador como Coordinador: el formulario interno muestra selector, créditos y horas del catálogo; registro con tercera matrícula persistente al reabrir; el formulario externo muestra sus campos. A 375 px no hay desbordamiento de página ni diálogo. Sin errores de consola.

El alta externa por formulario se probó con pruebas de interfaz y por API, no completándola en navegador. No se añade prueba Playwright. Las capturas no se revisaron: se comprobaron contenido y medidas, no el aspecto visual.

La verificación integrada anterior detectó que el Gateway solo reenvía rutas declaradas. Se añadieron dos rutas en `src/gateway/appsettings.json`, archivo compartido cuyo cambio debe coordinarse con Pablo en la revisión; la ruta nueva de Catálogo toca además un área de Leslie.

Las cuentas Coordinador y Docente se insertaron por SQL solo en la base desechable. **No verificado:** ejecución en CI, revisión cruzada y aceptación de la PO.

## Verificación previa a publicación — 03/11/2026

Se repitieron las suites sobre el contenido final de la rama: Solicitudes 75 aprobadas con SQL Server real y ninguna omitida, Gateway 75, Catálogo 77 y frontend 108; TypeScript y compilación Vite correctos. Las pruebas .NET se ejecutaron en SDK 8 sobre copias temporales del código; SQL utilizó una base y red desechables, eliminadas al terminar, sin tocar las bases existentes. TRX local de Solicitudes: `tmp/hu11-publish-20261101/hu11-publish.trx` (el directorio conserva el nombre del script reutilizado, no la fecha de ejecución).

Esta repetición no incluye un nuevo recorrido de navegador contra el stack completo. La CI del futuro PR, la integración previa de HU-10 y la aceptación funcional siguen pendientes. Se excluyen de esta entrega la planificación de HU-08, los ajustes locales de puertos Docker y el selector anterior inconcluso.
