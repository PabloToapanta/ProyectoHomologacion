# Propuesta de contrato HU-19 — análisis asistido de cambio externo

> Propuesta histórica previa a la implementación. El estado aplicado y sus diferencias
> se describen en [evidencia-hu-19.md](evidencia-hu-19.md). La aprobación reportada por
> Edwin sigue pendiente de registro en el Issue #43; este documento no la sustituye.

Issue #43. Responsables: Edwin Caraguay y Shirley Amaguaña. Propuesta del 04/11/2026
sobre `develop` `cbe9ac1` (HU-10, HU-11, HU-29, HU-30, HU-34/35/36, HU-18 y la
persistencia de HU-20 integradas). **No hay código de HU-19.** Según AGENTS.md, los
contratos se precisan por HU antes de codificar: este documento requiere la decisión
de Shirley (PO) y la revisión de Leslie, dueña del módulo de análisis de HU-18.

## Por qué no se codifica todavía

Al contrastar los criterios de #43 con lo integrado aparecen tres bloqueos materiales:

1. **Alcance de «externo».** SC-01 (20/10/2026, `docs/contexto-proyecto.md` y
   `docs/arquitectura/propuesta-alcance-fisei.md`) define el origen externo como
   *otra universidad del Ecuador*; las instituciones de otro país y la apostilla
   (RN-06) quedaron como referencia histórica fuera de alcance. CA1 («nacional o
   extranjera») y CA3 (apostilla) de #43 las vuelven a introducir.
2. **Sin contenidos del programa externo.** HU-11 guarda de la asignatura externa
   nombre, horas y unidad, créditos y sistema, `referenciaPrograma` (texto),
   nota con su escala y fecha. No hay contenidos, resultados de aprendizaje ni
   correspondencias revisadas con la receptora; HU-08 se limita a programas FISEI.
   Sin ellos el criterio académico combinado no puede calcularse.
3. **Sin datos para RN-05.** El validador de HU-16 necesita períodos cursados y
   cambios previos de carrera; Solicitudes no los registra. HU-18 ya dejó constancia
   de que no pueden inferirse.

## Decisiones solicitadas a Shirley

| # | Decisión | Propuesta |
|---|---|---|
| D1 | Alcance de CA1/CA3 | Mantener SC-01: todo origen externo es una universidad ecuatoriana. No se registra tipo nacional/extranjera ni se evalúa apostilla; una institución de otro país queda fuera del sistema. Ajustar CA1/CA3 de #43 en consecuencia. |
| D2 | Fuente de contenidos externos | **Esta versión:** el criterio académico de un origen externo se informa siempre como *información incompleta*, sin invocar al agente. **Sprint 3:** historia nueva para registrar contenidos/resultados del programa externo con su ubicación y correspondencias revisadas por un humano, igual que el catálogo interno. |
| D3 | Unidades comparables (CA4) | Horas y créditos solo se comparan si la unidad horaria y el sistema de créditos de la asignatura externa coinciden exactamente (tras recortar espacios y sin distinguir mayúsculas) con los de la receptora del catálogo. Cualquier diferencia o ausencia produce *información incompleta* con el motivo «sin conversión aprobada». No se inventan factores. |
| D4 | Datos de RN-05 | Añadir a la solicitud externa `periodosCursados` y `cambiosPreviosCarrera`, declarados por el Coordinador desde el expediente Quipux (HU-10). Valores ausentes = *información incompleta*, nunca «cumple» ni «incumple». |
| D5 | Estimación | 3 h de #43 no alcanzan para D4 + endpoint + vista. Propuesta: T38a datos RN-05 en Solicitudes (3 h), T38b análisis externo en Homologación (4 h), T38c vista docente (2 h). Planificar en Sprint 3 salvo otra indicación. |

## Contrato propuesto (si se aprueban D1–D4)

### Solicitudes (Edwin)

- Solicitud externa: `periodosCursados` (entero ≥ 0, opcional) y `cambiosPreviosCarrera`
  (entero ≥ 0, opcional). Solo para `Externo`; en `Interno` se rechazan. Migración propia.
  Se editan mientras la solicitud no tenga análisis; el detalle del Coordinador y del
  docente los muestra como «sin dato» cuando faltan.
- Lectura interna `GET /api/solicitudes/{id}/contexto-analisis-externo` (Docente asignado,
  `sub` del JWT, clave interna HU-36), simétrica a la de HU-18: sin Quipux, identificación
  ni nombre del estudiante ni matrícula. Devuelve institución, carrera externa, los dos
  datos RN-05 y las asignaturas externas con horas/unidad, créditos/sistema, referencia
  de programa, estado de aprobación y fecha. No se publica en el Gateway.

### Homologación (módulo de Leslie; cambio a acordar con ella)

- `POST /api/homologaciones/solicitudes/{id}/analisis-externo` (Docente), cuerpo
  `{ asignaturaAprobadaId, asignaturaReceptoraId }`, mismo rechazo de identidad,
  horas, créditos o porcentajes enviados por el navegador que HU-18.
- Flujo: contexto de Solicitudes → receptora del Catálogo (carrera de la solicitud)
  → aprobación y antigüedad (HU-15, fecha de Ecuador) → D3 para horas/créditos →
  criterio académico *información incompleta* (D2) → estado `Incompleto` con todos
  los motivos. El motor conserva el veredicto; el agente común de HU-18 solo se
  invoca cuando existan correspondencias revisadas (Sprint 3).
- `GET` en la misma ruta: últimos análisis por pareja, con nueva comprobación de
  asignación. `GET .../requisitos-externos`: resultado RN-05 con el validador de HU-16.
  `asignaturasHomologables` = asignaturas aprobadas distintas cuyo último análisis
  guardado es `Completo` y `Homologable`. Si faltan datos o no hay análisis completos,
  el resultado es `Incompleto` y lista lo que falta; solo un dato declarado por debajo
  del mínimo (p. ej. un período, un cambio previo) se informa como incumplimiento.
- Persistencia: tabla propia `AnalisisExternos` con la misma clave de pareja y la
  misma política de HU-20 (último intento reemplaza, 503 sin confirmar si falla SQL),
  mediante una migración nueva. No se altera `AnalisisInternos`.

### Gateway y vista (HU-30)

- Rutas `analisis-externo` (GET/POST) y `requisitos-externos` (GET), `AcademicAccess`.
- La vista docente elige el panel según `tipoCambio`. Un análisis externo muestra
  «Información incompleta» con sus motivos y nunca una sugerencia mientras rija D2.

## Pruebas previstas (TDD, tras aprobar el contrato)

- **PA1 → CA1–CA2:** reglas RN-05 externas ya cubiertas por HU-16; nuevas pruebas del
  cómputo de homologables a partir de análisis guardados y de datos ausentes que dan
  `Incompleto`, no «cumple» ni «incumple».
- **PA2 → CA3 (según D1):** una solicitud externa no exige apostilla y no existe
  registro de institución extranjera.
- **PA3 → CA4:** unidades distintas, ausentes o con distinta capitalización/espacios;
  ninguna conversión automática.
- **PA4 → CA5:** con SQL Server real, guardar y recuperar un análisis externo tras
  reiniciar; docente no asignado recibe 404; la vista no muestra sugerencia.

## Límites

No incluye carga de programas externos (D2, Sprint 3), conversión de notas o unidades
(RN-10), revisión docente HU-31, informes ni remisión. No se presenta SC-01 ni esta
propuesta como normativa oficial de la UTA.
