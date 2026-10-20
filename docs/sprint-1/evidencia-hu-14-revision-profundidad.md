> **Registro histórico sustituido.** La aclaración académica del 19/10/2026 reemplaza las cuatro dimensiones y el bloqueo de profundidad por tres criterios. Consultar el [estado vigente](evidencia-hu-14-tres-criterios.md). Las cifras y decisiones anteriores se conservan como evidencia de su ejecución, no como contrato actual.

# HU-14 / Issue #5 — revisión de profundidad, estados y extensibilidad

Fecha: 2026-10-19. Base: `11dbb68`, rama `feature/hu-14-equivalencia`.
Estado: revisión técnica ejecutada, **profundidad pendiente de aprobación académica**.
No se declara completa ni aceptada HU-14. Esta evidencia complementa la ejecución inicial de
192 pruebas y registra una nueva ejecución; no sustituye sus resultados históricos.

## Qué se revisó y cambió

Se revisaron las reglas de AGENTS.md, el contexto, la planificación, los criterios y contratos,
el caso de uso, el motor, los DTO, el mapeo HTTP y las pruebas de arquitectura e integración.
El árbol estaba limpio antes de esta revisión.

- Se añadió la [consulta académica detallada](../arquitectura/profundidad-hu-14-pendiente-academico.md):
  información necesaria, decisiones explícitas del docente/Coordinación, punto mínimo de extensión,
  semántica de los estados y pasos posteriores de integración.
- Se identificó con un comentario el bloque de profundidad en `EvaluarEquivalencia.Ejecutar`,
  previo a `MotorEquivalencia.Evaluar`. No se añadió una interfaz vacía, una fórmula ni dependencia.
- Se añadieron tres pruebas de caracterización de estados en Domain y una prueba en Application
  que localiza el bloqueo y verifica la decisión positiva con profundidad sintética solo en el motor.
- Se enlazó la consulta desde README y se amplió la propuesta de coordinación con Edwin.

No se modificaron fórmulas, umbral, prioridad de estados, contratos HTTP, composición de DI,
referencias de proyectos, Auth, Gateway, frontend o catálogo. No hubo commits ni push en esta revisión.
Las pruebas añadidas comprueban comportamiento preexistente; no se presentan como una nueva regla académica.

## Resultado positivo y diferenciación de estados

| Entrada sintética (contenido, profundidad, horas, créditos), umbral 80 | Esperado | Real |
|---|---|---|
| (80, null, 80, 80) | Información incompleta, false | Aprobado |
| (80, 79,9, 80, 80) | NoHomologable, false | Aprobado |
| (80, 80, 80, 80) | Homologable, true | Aprobado |
| (100, 0, 100, 100) | NoHomologable; cero conocido no es ausencia | Aprobado |
| (79,9, null, 100, 100) | Información incompleta; no clasificar como evaluación completa | Aprobado |

El caso de uso calcula contenido 80 %, horas 100 % y créditos 100 % con la fixture existente,
y conserva `InformacionIncompleta` porque profundidad es null. La nueva prueba suministra 80 %
de profundidad **solo al motor puro**, junto a esos porcentajes calculados: devuelve `Homologable`.
No se inyecta ese valor al caso de uso ni se habilita una entrada HTTP para porcentajes.

El punto académico pendiente está identificado, pero **no se ha habilitado un resultado positivo
por API**. Los tres estados son explícitos en Domain/Application; no se añade una clave `estado`
al contrato JSON compartido. La respuesta actual mantiene null y la justificación de información incompleta.

## Ejecución real de compilación y pruebas

Se ejecutó restore, build Release y test de toda la solución:

| Suite | Aprobadas | Fallidas / omitidas |
|---|---:|---:|
| AuthService.Tests | 43 | 0 / 0 |
| Auth.Application.Tests | 13 | 0 / 0 |
| Gateway.Tests | 59 | 0 / 0 |
| Homologacion.Domain.Tests | 38 | 0 / 0 |
| Homologacion.Application.Tests | 15 | 0 / 0 |
| Homologacion.Api.Tests | 28 | 0 / 0 |
| **Total .NET** | **196** | **0 / 0** |

Build: **0 errores y 0 advertencias**. Las pruebas de referencias de ensamblados mantienen Domain
sin dependencias técnicas y Application dependiendo solo de Domain y el sistema.
Las pruebas de Auth/Gateway pasan completas; las de homologación verifican contrato HTTP y YARP.

Frontend HU-01, sin modificaciones: `npm run build` (incluye `tsc --noEmit`) y `npm test`, ambos
con código 0; **43 pruebas aprobadas en cuatro archivos**. No se ejecutaron Playwright ni Docker
Compose nuevamente; esta revisión no declara una nueva comprobación E2E. La ejecución anterior
con Compose conserva su evidencia en el registro T9–T10.

Salida real de esta revisión: [verificacion.txt](evidencias/hu-14-revision-profundidad/verificacion.txt).
Los TRX quedaron en `/tmp/hu14-revision-results`, fuera de Git.
TestServer sigue advirtiendo que no puede aplicar el límite de cuerpo HTTP; no se considera un fallo
de compilación ni una comprobación del límite de Kestrel.

## Edwin: HU-15 y HU-16

No existe dependencia de `Equivalencias` sobre clases de aprobación, antigüedad o cambio.
Edwin puede añadir módulos hermanos `Antiguedad` y `RequisitosCambio`, con DTO, casos de uso,
controladores y pruebas propios. No necesita cambiar `MotorEquivalencia`, `Porcentaje`,
`EvaluarEquivalencia` ni reutilizar el umbral de equivalencia.
Si HU-16 consume resultados del comparador, solamente `Homologable` cuenta como homologable.

La composición de DI en `Program.cs`, la solución, proyectos o configuración siguen siendo archivos
compartidos: cualquier cambio ahí debe coordinarse con Pablo. No se afirma ausencia de conflictos
ante ediciones simultáneas. No se implementaron reglas ni clases vacías de HU-15/HU-16.

## Pendiente exacto para completar HU-14

Del docente/Coordinación necesitamos **datos y una decisión reproducible**:

1. Programas analíticos autorizados y versionados, resultados de aprendizaje, evidencia observable de
   profundidad y correspondencias académicas verificadas entre origen y receptora.
2. Definición de profundidad y unidad de comparación; escala si aplica y significado de sus niveles;
   reglas para suficiencia, parcialidad, correspondencias múltiples y mayor exigencia en origen.
3. Método exacto para obtener un porcentaje 0–100: denominador, pesos si aplican, agregación,
   precisión/redondeo y tratamiento de evidencia insuficiente, contradictoria o no comparable.
4. Confirmación de la propuesta del equipo: cobertura por temas receptores distintos, validez de las
   correspondencias, umbral inicial de 80 % y cumplimiento individual de las cuatro dimensiones;
   aclarar ámbito de aplicación y cualquier diferencia por carrera/malla/modalidad.
5. Registro de aprobación (responsable, fecha, versión y fuente) y casos de referencia con entradas,
   resultados esperados y explicación, incluidos los límites de aceptación.

Tras esa aprobación faltará trabajo técnico: acordar el contrato de evidencia con el catálogo,
implementar mediante TDD la regla pura, conectarla en Application, ajustar la justificación por estado
sin perder otros motivos de incompletitud, y verificar el caso positivo completo por HTTP.
Revisión cruzada y aceptación de Shirley permanecen pendientes. No se interpreta la aprobación
para el prototipo como normativa oficial ni se altera ninguna regla académica en esta revisión.
