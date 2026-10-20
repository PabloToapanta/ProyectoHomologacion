> **Registro histórico sustituido.** La aclaración académica del 19/10/2026 reemplaza las cuatro dimensiones y el bloqueo de profundidad por tres criterios. Consultar el [estado vigente](cambio-hu-14-tres-criterios.md). Las cifras y decisiones anteriores se conservan como evidencia de su ejecución, no como contrato actual.

# HU-14 / Issue #5 — definición de profundidad pendiente del docente

Estado: consulta académica por resolver. No contiene una fórmula aprobada ni constituye normativa.
Esta revisión documenta el bloqueo; no habilita porcentajes aportados por el cliente ni resultados
positivos por API sin evidencia. El sistema produce sugerencias, no decisiones institucionales.

## Información que necesitamos del docente

Para las dos asignaturas (origen y receptora), proporcionar o identificar:

| Información | Qué debe quedar verificable |
|---|---|
| Programa analítico o sílabo autorizado | Fuente, versión de malla, período de vigencia e identificadores de asignatura |
| Resultados de aprendizaje | Texto oficial e identificador; relación con temas o unidades, cuando exista |
| Evidencia de profundidad | Qué características observables permiten juzgar el grado de exigencia, alcance o desempeño de cada resultado |
| Escala o niveles, si se utilizan | Definición de cada nivel y significado de las etiquetas; no basta con números de semestre o nombres de asignatura |
| Correspondencias académicas | Qué resultados/unidades se comparan entre origen y receptora, evidencia de la relación y responsable de validarla |
| Casos de referencia | Entradas completas y resultado esperado explicado por el docente, incluyendo suficiente, insuficiente e incompleto |

Los datos de contenido disponibles hoy (identificadores de temas y correspondencias declaradas)
no aportan por sí solos resultados de aprendizaje ni una medida de profundidad. Más horas, créditos,
nivel de semestre o coincidencia de textos no sustituyen el criterio pendiente.
No se inventan sílabos, resultados, escalas ni correspondencias para completar esta información.

## Decisiones que debe aprobar el docente / Coordinación

Se necesita una respuesta explícita y reproducible a estas preguntas; ninguna alternativa queda
seleccionada por este documento:

1. **Definición operacional:** ¿qué significa profundidad equivalente y qué evidencia permite comprobarla?
   ¿Se compara cada resultado de aprendizaje, unidad u otra unidad académica?
2. **Comparación:** ¿cómo se juzga una correspondencia suficiente, parcial o insuficiente?
   Si existe una escala, ¿qué representa cada valor y cómo se comparan los niveles?
   ¿Cómo se trata una exigencia de origen mayor que la receptora?
3. **Agregación:** ¿qué elementos forman el denominador respecto a la receptora? ¿Existen pesos?
   ¿Cómo se tratan correspondencias uno-a-varios o varios-a-uno sin doble conteo?
4. **Porcentaje:** ¿qué procedimiento exacto transforma la evidencia en un valor entre 0 y 100?
   Definir límites, precisión y cualquier redondeo; el motor actual compara sin redondear previamente.
   No se propone aquí una escala de Bloom, promedio de niveles ni otra fórmula.
5. **Información insuficiente:** ¿cuál es la evidencia mínima? ¿Qué ocurre ante elementos ausentes,
   no comparables o contradictorios? La regla vigente es mantener información incompleta cuando
   no puede calcularse la dimensión; un dato desconocido no se convierte en cero.
6. **Parámetros y aplicabilidad:** confirmar o corregir, mediante acuerdo, la propuesta del equipo:
   umbral inicial de 80 % y cumplimiento individual de las cuatro dimensiones sin compensación.
   Confirmar también la cobertura de contenido por temas receptores distintos y la validez de sus
   correspondencias declaradas. Aclarar si el criterio cambia por carrera, malla o modalidad.
7. **Validación:** proporcionar ejemplos aprobados y su explicación para probar la implementación.
   Incluir límites de 79,9 % y 80 %, y un umbral configurado de 85 %; si se modifica la propuesta,
   acordar los criterios de aceptación antes de cambiar el código.

Registrar responsable, fecha, versión, ámbito de aplicación, documento/fuente y casos esperados.
La aceptación de un criterio para el prototipo **no prueba que sea normativa oficial**. Si se invoca
una disposición institucional, el equipo debe aportar y contrastar su referencia autorizada.
Shirley mantiene la aceptación de la historia; el docente/Coordinación define o valida el criterio académico.

## Punto de extensión mínimo identificado

No se agrega una interfaz vacía mientras no conocemos los datos ni la operación aprobada.
El punto actual está en `Homologacion.Application/Equivalencias/EvaluarEquivalencia.cs`, método
`EvaluarEquivalencia.Ejecutar`: el bloque `decimal? profundidad = null`, inmediatamente antes de
la llamada a `MotorEquivalencia.Evaluar`.

Después de la aprobación, la incorporación mínima será:

1. Definir los objetos de valor/evidencia y la **regla pura de profundidad en Homologacion.Domain/Equivalencias**
   según el procedimiento aprobado. Probarla primero con los casos del docente. No incorporar HTTP,
   EF, configuración técnica ni dependencias hacia Application/Infrastructure dentro de Domain.
2. Acordar los datos de entrada que faltan con Leslie/Edwin y el responsable del contrato antes de
   ampliar los DTO y contratos HTTP. Actualmente no se reciben resultados ni evidencia de profundidad.
3. Invocar esa regla desde el bloque identificado en Application, usando la evidencia validada.
   Retirar el motivo pendiente de profundidad solamente cuando exista un porcentaje calculado válido;
   conservar los motivos de otras ausencias. No simular evidencia con un porcentaje de la solicitud.
4. Mantener `MotorEquivalencia.Evaluar` como punto de decisión para los cuatro porcentajes. Ya acepta
   profundidad disponible; no necesita conocer el algoritmo que la produjo. El caso de uso deberá
   preservar información incompleta si aún falta evidencia requerida y generar una justificación
   coherente con el estado resultante, en lugar del texto actualmente fijo de información incompleta.
5. Verificar el caso positivo por HTTP y la regresión de datos incompletos/insuficientes después
   de integrar la regla. Mantener las claves JSON acordadas; cualquier extensión se coordina.

Una regla matemática pura puede llamarse directamente desde Application: no necesita un puerto técnico.
Solo si el diseño aprobado requiere obtener evidencia de un sistema externo se definirá el puerto
mínimo en Application y su adaptador en Infrastructure, una vez acordado ese contrato.
No se crea ahora un cliente HTTP del catálogo, una selección de fórmulas ni un modo de aprobación manual.

## Estados y prueba del resultado positivo

| Estado del dominio | Condición | Homologable |
|---|---|---|
| `InformacionIncompleta` (Información incompleta) | Alguna dimensión desconocida o con porcentaje inválido | false |
| `NoHomologable` | Cuatro porcentajes válidos y al menos uno inferior al umbral | false |
| `Homologable` | Cuatro porcentajes válidos y todos alcanzan el umbral | true |

Con el umbral 80, las entradas **sintéticas** `(80, null, 80, 80)`, `(80, 79.9, 80, 80)` y
`(80, 80, 80, 80)` producen respectivamente los tres estados de la tabla. Un cero conocido es
insuficiencia, no ausencia; una dimensión ausente conserva el estado incompleto aunque otra sea
insuficiente. Esto caracteriza el comportamiento existente, sin aprobar un criterio académico nuevo.

`EstadosEquivalenciaTests` comprueba la distinción. La prueba de aplicación
`Profundidad_pendiente_es_el_bloqueo_y_no_una_prohibicion_del_motor` confirma que los otros
porcentajes calculados admiten una decisión positiva al proporcionar profundidad sintética **solo al
motor puro**. El caso de uso y la API permanecen incompletos; no se instala una sustitución de pruebas
ni se aceptan porcentajes de cliente en producción.

Los DTO internos ya conservan `Estado`. No se añade una clave nueva a la respuesta HTTP compartida:
la API actual comunica información incompleta con `profundidadPct: null`, `homologable: false`
y `justificacion`. Actualmente no produce los otros dos estados; los porcentajes sintéticos en
las pruebas no prueban un cálculo de profundidad ni un resultado HTTP positivo.

## Convivencia con HU-15 y HU-16

Edwin puede añadir `Antiguedad` y `RequisitosCambio` como módulos hermanos de `Equivalencias`
en Domain/Application, y controladores y contratos propios. No requiere alterar `MotorEquivalencia`,
`Porcentaje`, `EvaluarEquivalencia`, sus DTO ni `IUmbralEquivalencia`.
La aprobación/antigüedad y los mínimos de cambio no son dimensiones de equivalencia: no insertar
esas reglas en este punto de extensión. Si HU-16 consume equivalencias, solo debe contar resultados
`Homologable`; no contar `InformacionIncompleta` como una materia homologable.

Los posibles conflictos están en la composición (`Program.cs`), registros de DI, `.csproj`, solución
y configuración compartida, no en la lógica independiente. Coordinar cualquier edición de esos
archivos con Pablo. Usar archivos y namespaces propios y pruebas separadas por funcionalidad.
No se implementan ni se crean clases vacías de HU-15/HU-16 en esta revisión.

Ver [propuesta de coordinación](coordinacion-homologacion-service.md) y
[evidencia de esta revisión](../sprint-1/evidencia-hu-14-revision-profundidad.md).
