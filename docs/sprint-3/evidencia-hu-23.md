# HU-23 — informe de homologación

Responsable: Edwin Caraguay. Rama `feature/hu-23-informe-homologacion` sobre `feature/hu-31-revision-docente`
(`cd034ca`). Fecha: 04/11/2026. Historia del Sprint 3 adelantada con autorización de Shirley, transmitida por
Edwin. Contrato: [contrato-hu-23.md](contrato-hu-23.md). **La regla de combinación de decisiones y el cierre
están sujetos a la consulta de Edwin con su docente.** Seguimiento: [Issue #61](https://github.com/PabloToapanta/ProyectoHomologacion/issues/61).

## Cambios

- **Application:** `GenerarInformeHomologacion` arma y guarda el informe.
  - Toma las parejas con análisis docente. Exige el análisis de los dos docentes en cada pareja; si falta alguno, devuelve 400 con las receptoras pendientes.
  - Copia el último análisis del agente (estado, sugerencia y los tres porcentajes) y el análisis de cada docente.
  - Calcula la sugerencia final: `Convalidar`, `NoConvalidar` o `SinAcuerdo`.
  - Lista las asignaturas aprobadas sin análisis y añade advertencias.
- **Cierre de los análisis:** `RegistrarRevisionDocente` rechaza correcciones cuando ya existe el informe (409).
- **Infrastructure:** tabla `InformesHomologacion` con una fila por solicitud (contenido JSON) y la migración `20261104000400_InformesHomologacionHu23`. La clave primaria garantiza un solo informe aunque se generen dos a la vez; quien llega tarde recibe 409, no un error de SQL.
- **API:** `POST` y `GET /api/homologaciones/solicitudes/{id}/informe`, solo para el rol Docente asignado.
- **Gateway:** ruta con `AcademicAccess`. La prueba de OpenAPI pasa de 29 a 30 rutas.
- **Frontend:** `InformePanel` en el detalle docente.
  - Si el informe no existe, ofrece «Generar informe».
  - Si existe, muestra la tabla por pareja (agente, porcentajes, docentes y sugerencia final), el resumen, las asignaturas sin análisis y las advertencias.
- **Pruebas que se ajustaron por el cambio de alcance:**
  - La prueba de HU-30 que exigía «sin informe» ahora espera el botón «Generar informe» a petición; siguen sin existir la remisión y las escrituras automáticas.
  - Las pruebas de API de HU-31 sustituyen el almacén de informes por uno vacío.

## Criterios propuestos

| Criterio | Evidencia | Estado |
|---|---|---|
| CA1 — generar con agente, docentes y sugerencia final | Application, API y smoke | Verificado |
| CA2 — Convalidar / No convalidar / Sin acuerdo | Application, API, frontend y smoke (una pareja en acuerdo y otra sin acuerdo) | Verificado |
| CA3 — sin parejas o con un docente faltante no se genera | Application, API y smoke (400) | Verificado |
| CA4 — único y fijo; análisis cerrados | Application, API, SQL Server real y smoke (409); falta el intercalado corrección/cierre | Parcial: el cierre no es atómico |
| CA5 — solo docentes asignados | API y smoke (401/403/404) | Verificado |
| CA6 — persistencia y fallo controlado | SQLite y SQL Server real; smoke con reinicio | Ver ejecuciones |

## Ejecuciones del 04/11/2026

TDD: la fase roja se observó en Application, Infrastructure, API (5 pruebas nuevas fallidas) y frontend.

1. `Homologacion.sln`: todas aprobadas (Application 107, API 133).
2. `Homologacion.Infrastructure.Tests` con SQL Server 2022 temporal, 3 ejecuciones: 51 aprobadas y 1 omitida (Ollama real) en cada una.
3. Gateway: 121/121. Frontend: 135/135 y `npm run build`.
4. `tests/security/hu23-smoke.py` sobre una copia limpia de la rama, con Docker Compose, Gateway, JWT cifrado
   y SQL Server real: **47/47**. Corresponden 32 al flujo de HU-19 y HU-31 y 15 al informe.
   - Comprueba que el informe no existe todavía, el 400 si falta un docente, el 403 del Coordinador y el 404 de un docente ajeno.
   - Genera el informe con una pareja «Convalidar» y otra «Sin acuerdo»; el resumen da 1 · 0 · 1. La pareja sin análisis del agente figura como `SinAnalisis`.
   - Verifica que no expone datos personales, el 409 de una segunda generación y de una corrección posterior, y que el informe es idéntico tras reiniciar Homologación.
   - Un primer intento falló por una expectativa mal escrita en el propio smoke: suponía que el agente había analizado ambas parejas. Se corrigió la expectativa, no el código, y se repitió completo.
   - Proyecto aleatorio con volúmenes eliminados. HTTP local: no acredita HTTPS.

**Limitación conocida:** entre la comprobación de «informe existente» y el guardado de un análisis docente no hay
transacción común. Una corrección enviada en el mismo instante en que se genera el informe podría guardarse
después del informe, sin cambiar el contenido del informe ya fijado.

**No verificado:** prueba en navegador, CI en GitHub, revisión cruzada y aceptación de Shirley.

## Revisión para publicación del 04/11/2026

Se creó la issue #61 con criterios y pruebas vinculadas, incluido el caso de
concurrencia que falta. La revisión estática confirma que `RegistrarRevisionDocente`
consulta el informe antes de guardar y que `RevisionesDocentesSql.GuardarAsync`
no vuelve a validar el cierre en una operación atómica con la generación. Una
corrección que superó la consulta previa puede confirmar después del informe.

Las pruebas actuales cubren una corrección que empieza después del cierre y varias
generaciones simultáneas. No cubren el intercalado edición iniciada → informe
confirmado → edición guardada. El informe sigue fijo, pero la revisión cambia después
del cierre prometido. Por ello CA4 no está completo y no se publica PR como lista.

Frontend ejecutado de nuevo: **135/135**, TypeScript y compilación Vite aprobados.
El primer intento de backend no pudo ejecutarse porque Docker estaba apagado;
se inició Docker Desktop para repetir la verificación con SQL Server temporal.

Repetición completada: **706 pruebas backend aprobadas y 1 omitida** (Ollama real).
Desglose: Homologación Domain 71, Application 107, API 133, Infrastructure 51;
Auth Application 13, AuthService 64; Gateway 121; Solicitudes 146. SDK .NET 8 y
SQL Server 2022 en contenedores temporales, eliminados con su red al finalizar.
Smoke independiente en copia aislada: **47/47**, con JWT cifrado y SQL Server real;
proyecto y volúmenes eliminados. HTTP local: no acredita HTTPS. `git diff --check`
sin errores. Se conserva el bloqueo de CA4: estos resultados no incluyen su
intercalado concurrente pendiente. Descripción local de PR en `pr-hu-23.md`, sin
publicar ni subir la rama mientras no se declare lista.
