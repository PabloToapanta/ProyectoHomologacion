## Historia de usuario

Como docente asignado, quiero generar un informe con el análisis automático, el de
ambos docentes y la sugerencia final por pareja, para documentar la revisión de la
solicitud de homologación.

## Planificación y dependencias

- Historia del Sprint 3; el contrato local recoge prioridad alta y 5 PH del Sprint 0.
- Adelanto autorizado por Shirley según comunicación de Edwin registrada en el
  contrato local; pendiente de constancia en esta issue y de aceptación funcional.
- Responsable: Edwin. Revisión cruzada: Leslie (Homologación) y Pablo (Gateway).
- Rama `feature/hu-23-informe-homologacion`, sobre HU-31 (`cd034ca`). Integrar después
  de #56 (HU-19) y #60 / #59 (HU-31). Relacionada con #42, #44 y #41.
- Contrato: `docs/sprint-3/contrato-hu-23.md`; evidencia: `docs/sprint-3/evidencia-hu-23.md`.

## Contrato propuesto para aceptación

Cualquiera de los dos docentes asignados genera un único informe fijo por solicitud.
Incluye las parejas con revisión docente; cada pareja requiere las revisiones de los
dos docentes. Coincidencia positiva: `Convalidar`; negativa: `NoConvalidar`;
discrepancia: `SinAcuerdo`, conservando ambas decisiones. Lista las asignaturas
aprobadas sin análisis y conserva la fotografía del análisis automático y docente.

La combinación de decisiones y el cierre están sujetos a la consulta de Edwin con
su docente de la asignatura; estas reglas son propuestas del equipo, no normativa
oficial de la UTA. Si cambia el acuerdo, actualizar contrato, pruebas e implementación.

## Criterios de aceptación propuestos

- [ ] **CA1.** Un docente asignado genera el informe con agente, ambos docentes,
  receptoras identificadas y sugerencia final; las asignaturas sin análisis no reciben sugerencia.
- [ ] **CA2.** Las coincidencias producen `Convalidar`/`NoConvalidar`; las
  discrepancias producen `SinAcuerdo` y muestran las dos decisiones.
- [ ] **CA3.** Sin parejas o con una revisión docente faltante se responde 400,
  indicando lo pendiente, sin guardar un informe parcial.
- [ ] **CA4.** El informe es único e inmutable. Una segunda generación responde 409.
  Tras generarlo, no se guardan correcciones docentes, tampoco si comenzaron antes
  del cierre y terminan después por concurrencia.
- [ ] **CA5.** Solo docentes asignados generan o consultan: 401 sin sesión, 403
  para otros roles y 404 para docente ajeno; identidad tomada del JWT.
- [ ] **CA6.** El informe se conserva idéntico tras reiniciar; un fallo de
  almacenamiento se controla sin anunciar éxito ni reemplazar datos persistidos.

## Tareas y pruebas

- [ ] **PA1 → CA1/CA2:** reglas y aplicación para acuerdos positivos, negativos y
  discrepancias; fotografía del análisis automático, porcentajes y pendientes.
- [ ] **PA2 → CA3:** ausencia de parejas y falta de uno de los docentes, sin escritura.
- [ ] **PA3 → CA4:** SQL Server real con generaciones simultáneas: exactamente un
  informe y conflictos controlados para las demás.
- [ ] **PA4 → CA4:** intercalado determinista de corrección y cierre: la edición
  lee «sin informe», se confirma el informe y luego intenta guardarse; debe dar
  409 y conservar la revisión previa. Cubrir también el orden contrario.
- [ ] **PA5 → CA5:** API/Gateway con JWT y autorización por asignación en cada petición.
- [ ] **PA6 → CA6:** lectura con conexión nueva, reinicio real y fallo SQL forzado
  sin éxito ficticio ni informe parcial.
- [ ] **PA7 → CA1/CA3:** interfaz para generar, mostrar resumen/parejas y errores;
  verificación manual en navegador real.
- [ ] Migración propia de `InformesHomologacion`, endpoints GET/POST y OpenAPI.
- [ ] Revisión cruzada, CI y aceptación de Shirley con evidencia verificable.

## Hallazgo de revisión que bloquea declarar la implementación lista

El cierre se comprueba en `RegistrarRevisionDocente.RegistrarAsync` mediante una
lectura de `IInformesHomologacion`, separada del posterior `GuardarAsync` de la
revisión. El repositorio de revisiones no comprueba el cierre en su transacción.
Por ello una edición iniciada antes del informe puede guardarse después de él.
La limitación está reconocida en la evidencia local, pero contradice CA4. La prueba
de generaciones simultáneas no cubre este intercalado: falta PA4 y una protección
atómica compartida entre generación y corrección antes de declarar lista HU-23.

## Fuera de alcance

PDF (HU-24), remisión (HU-32), finalización (HU-33), aceptación automática o merge.
Las pruebas locales en verde no equivalen a aceptación de la PO.

## Verificación independiente del 04/11/2026

- 706 pruebas backend aprobadas y 1 omitida (Ollama real), con .NET 8 y SQL Server
  2022 aislado; 135 frontend, TypeScript y compilación Vite aprobados.
- Smoke integrado: 47/47 con JWT cifrado y SQL Server reales, incluido reinicio.
- **CA4 sigue parcial:** estos tests no cubren el intercalado PA4 ni hacen atómico
  el cierre. No hay PR publicado: la apertura queda pendiente de resolver el bloqueo.
