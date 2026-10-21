# HU-14 — Comparador simple y datos de demostración

21/10/2026 · Issue [#5](https://github.com/PabloToapanta/ProyectoHomologacion/issues/5) · rama `feature/hu-14-comparador-catalogo`.
Esta entrega sustituye el formulario mixto documentado en `evidencia-hu14-selector-catalogo.md`.

## Uso

En Validaciones académicas → Equivalencia de asignaturas: elegir origen y receptora y pulsar **Comparar**. Los dos selectores agrupan las materias por carrera. No se solicitan planes, unidades de horas, sistemas de créditos, inventarios ni correspondencias en esta pantalla. Los resultados muestran tres barras y el umbral efectivo del servidor; el fundamento se puede desplegar.

En Catálogo → Asignaturas → **Perfil y equivalencias de [asignatura]**: guardar referencia al plan, unidad horaria, sistema de créditos y condición de dato sintético. En **Revisar correspondencias**: elegir el par, seleccionar elementos de origen para los receptores y acreditar su ubicación en ambos planes. Guardar una revisión sin vínculos significa cobertura revisada cero. Una revisión inexistente significa información incompleta. Las unidades y contenidos siguen en **Ver unidades**.

Las correspondencias son direccionales y se reutilizan. Una huella SHA-256 de cada perfil e inventario detecta cambios de programa; una revisión obsoleta no se usa hasta revisión humana. No se comparan nombres de temas por similitud ni se generan vínculos con IA.

Se compara compatibilidad de programas bajo un supuesto explícito de origen aprobado. No se almacena en el catálogo una aprobación del estudiante ni se acredita su historial. HU-15 y HU-16 conservan sus pantallas y reglas independientes. Tampoco se cambió el motor ni su contrato de tres criterios. La pantalla de selección depende del permiso Administrador existente del catálogo; no se ampliaron permisos a otros roles.

## Persistencia y contratos

Migración aditiva `20261021175243_PerfilesYCorrespondenciasHu14`: tablas `PerfilesComparacion` y `RevisionesComparacion` en la base de Catálogo; claves foráneas a asignaturas, sin modificar registros históricos.

- GET/PUT `/api/asignaturas/{id}/perfil-comparacion`. Perfil ausente: 204. Campos `planAnalitico`, `unidadHoras`, `sistemaCreditos`, `esSintetico`.
- GET/PUT `/api/asignaturas/{id}/correspondencias/{receptoraId}`. GET devuelve `registrada`, `vigente`, `contenidos`, `resultados`. PUT recibe las dos listas explícitas. Cada vínculo contiene `elementoOrigen`, `elementoReceptor`, `ubicacionOrigen`, `ubicacionReceptora`.
- Perfiles incompletos, vínculos inexistentes/duplicados, evidencia ausente y comparación consigo misma: 400. Asignatura inexistente: 404. JWT requerido: 401; rol no autorizado: 403.

Endpoints documentados por Swagger y publicados mediante dos rutas específicas de Gateway. Interfaces y casos de uso en Application, validación del perfil en Domain, repositorio/EF en Infrastructure y controladores en Api.

## Datos sintéticos insertados

Se ejecutó `scripts/cargar-demo-equivalencias.py --env-file .env.hu05-validacion` contra el Gateway local. Repetirlo no duplica registros; si un registro DEMO fue modificado, se conserva y se solicita revisión en vez de sobrescribirlo. El archivo privado solo se lee para autenticación y no se imprime.

Cinco carreras: Ingeniería de Software, Ingeniería en Tecnologías de la Información, Ingeniería Industrial, Ingeniería en Robótica y Automatización e Ingeniería en Telecomunicaciones. Cada una tiene Álgebra Lineal, Fundamentos de Programación y una materia de especialidad. Todas las materias llevan `[DEMO]` y código `DEMO-...`.

Total de la demostración: **15 asignaturas, 30 unidades, 90 contenidos, 30 resultados y 209 revisiones direccionales sintéticas**. Un par se deja sin revisar para probar incompletitud. Los planes y revisiones son fixtures explícitas de prueba, inventadas por autorización del usuario; no son sílabos reales, equivalencias oficiales ni datos verificados de UTA. Las revisiones vacías son escenarios sintéticos de cobertura cero, no juicios académicos sobre materias reales.

Se conservaron las 17 carreras y 22 asignaturas anteriores. Después de la carga, la base local contiene 22 carreras, 37 asignaturas y 30 unidades. No se eliminó ni reclasificó el catálogo anterior. La segunda ejecución del cargador confirmó idempotencia.

Ejemplos comprobados en navegador con servicios reales:

| Origen | Receptora | Académica | Horas | Créditos | Estado |
|---|---|---|---|---|---|
| DEMO-SW-ALG | DEMO-ROB-ALG | 100 % | 100 % | 100 % | Homologable |
| DEMO-SW-ALG | DEMO-TI-ALG | 75 % | 80 % | 100 % | No homologable |
| DEMO-ROB-ESP | DEMO-SW-PROG | Sin determinar | 100 % | 100 % | Información incompleta; revisión pendiente |

## Verificación ejecutada

- TDD: prueba API nueva falló inicialmente con 404; cinco pruebas de interfaz fallaron contra el formulario anterior; después de implementar pasaron.
- Catálogo: 75 pruebas aprobadas, incluyendo persistencia de metadatos/vínculos, evidencia obligatoria, rechazo de duplicados, revisión ausente frente a vacía, invalidación del plan modificado, autorización y Swagger.
- Gateway: 63 pruebas aprobadas. Se actualizó el número esperado de rutas Swagger de 11 a 13, con comprobación de las rutas nuevas.
- Frontend: suite completa de 97 pruebas aprobadas. TypeScript y compilación de producción correctos.
- Smoke integrado de Sprint 1 aprobado contra Auth, Catálogo, Gateway, Homologación y frontend, con 30 unidades consultadas.
- Prueba Playwright contra la instancia real: escenarios suficiente/insuficiente/incompleto, dos selectores, ausencia de campos técnicos, tres barras y adaptación móvil. La prueba detectó y permitió corregir el nombre accesible de selectores y el ancho mínimo heredado de la tabla.

## Ejecución local y pendientes

La instancia `hu05-validacion` sigue disponible en `http://127.0.0.1:5173`. Se publicaron localmente Catálogo y Gateway y se montaron sus salidas de compilación, además del `frontend/dist`, con el overlay privado `.env.hu14-runtime.compose.yaml`; se evitó depender de una descarga de imágenes que anteriormente quedó detenida. Las bases y volúmenes originales se conservaron. La migración aditiva se aplicó al arrancar Catálogo.

Para reproducir desde una instalación nueva se puede construir el Compose versionado con las fuentes actuales y cargar después el script DEMO. Para conservar esta instancia al recrearla deben mantenerse el mismo nombre de proyecto, archivo privado y el overlay local, o reconstruir las imágenes desde estas fuentes. Los directorios `.env.hu14-runtime` y los archivos privados están ignorados por Git.

Pendientes de DoD: revisión cruzada de Edwin, coordinación/revisión del cambio de catálogo por Leslie y aceptación de Shirley. La publicación de esta rama y su PR fue solicitada expresamente por el usuario; no autoriza merge ni cierre del Issue. Las pruebas técnicas y los ejemplos sintéticos no constituyen aceptación funcional ni normativa.
