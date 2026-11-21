# Sprint 3 — respuestas del ingeniero y decisiones para implementar

Fecha de registro: 20/11/2026. Relacionado con [Issue #8](https://github.com/PabloToapanta/ProyectoHomologacion/issues/8) y con la [planificación del Sprint 3](planificacion-detallada.md#6-decisiones-previas-a-codificar).

## Naturaleza de las respuestas

El ingeniero respondió las preguntas de validación de HU-21, HU-22, HU-17, HU-03, HU-32/33 y HU-37 con una lectura de fuentes públicas:

- Reglamento de Régimen Académico del CES (RRA), arts. 74, 79 y 82.
- Instructivo UTA RES CAU-P-005-2023 (cambios de carrera, de universidad y de plan de estudios; reingreso; terceras matrículas).

Él mismo advierte que sus respuestas **no sustituyen el pronunciamiento de Asesoría Jurídica ni de la autoridad académica**. El equipo no abrió ni contrastó esas fuentes. Por eso:

- Toda regla de este documento es una **regla de análisis del proyecto** y queda pendiente de confirmación institucional (Coordinación / UTA).
- El sistema produce **sugerencias**; la decisión es de la autoridad académica.
- Donde la respuesta no define una regla, no se codifica una propia: se usa el alcance más acotado o se registra «información incompleta».

Indicación del equipo (20/11/2026): **mantener todas las historias** del Sprint 3 y continuar a la implementación con estos criterios.

## Hallazgos que afectan lo ya entregado

| Hallazgo | Situación actual | Decisión |
| --- | --- | --- |
| **Antigüedad: 10 o 5 años.** El RRA del CES señala hasta 10 años; el instructivo UTA de 2023 menciona 5 años para el análisis comparativo. | HU-15 usa 10 años fijos en `ValidadorAntiguedad`, según la aclaración del 19/10. | Se mantiene **10 años como valor provisional**. HU-03 lo convierte en parámetro (`AniosMaximosAprobacion`) sin cambiar el comportamiento. La documentación y el informe lo marcan como pendiente de confirmación de la UTA. No se fija como regla definitiva. |
| **Criterios del 80 %.** El CES exige al menos 80 % en contenido, profundidad y carga horaria. | El motor (HU-14) mide compatibilidad académica combinada, horas y créditos, cada uno ≥ 80 %. | Se conserva el motor. El informe y la documentación lo describen como **regla de análisis institucional del proyecto** (aclaración del 19/10), no como transcripción de la norma. Queda pendiente que la Coordinación valide los créditos como criterio propio. |

## Decisiones por historia

### HU-21 · Agrupación N:M (CE-3)

La norma permite comparar «una o más» asignaturas en ambos lados, pero no fija un algoritmo, un máximo ni reglas contra el doble conteo. Alcance implementable:

1. **Solo varias asignaturas de origen → una receptora (N:1).** Los casos 1:N y N:M generales quedan fuera porque requieren reglas de no duplicación que nadie ha definido. Si se piden, la API los rechaza con un mensaje explícito.
2. **Contenido:** un elemento de la receptora (contenido o resultado) queda cubierto si al menos una asignatura del grupo tiene una correspondencia revisada y vigente hacia él. Cada elemento receptor se cuenta una sola vez, así que no hay doble conteo.
3. **Horas y créditos:** se suman los de las asignaturas de origen **aprobadas** y se comparan con el total de la receptora, con un tope del 100 % y trazabilidad por asignatura. Si las asignaturas del grupo usan unidades o sistemas de créditos distintos, el resultado es **información incompleta**: no se convierte.
4. **Aprobación y antigüedad por cada origen** (HU-15). Una asignatura no elegible no forma parte del grupo y se informa el motivo.
5. **Sin máximo de asignaturas**, porque no hay norma que lo fije; no se inventa un límite.
6. **Una asignatura de origen se usa una sola vez por solicitud**, sea en un grupo o en una pareja individual.
7. Se mantienen los tres criterios con el umbral configurable (80 %), igual que en una pareja individual.
8. El análisis y el informe se marcan como «homologación por agrupación — regla de análisis pendiente de validación institucional».

### HU-22 · Carreras muy diferentes y ruta alternativa (CE-2)

No hay una definición normativa de «muy diferentes». El sistema **no clasifica carreras como «muy diferentes» y no bloquea** solicitudes. Alcance implementable, solo informativo:

1. **Sin el mínimo de HU-16:** el informe muestra una alerta con estos datos:
   - el mínimo requerido y el alcanzado;
   - que el cambio se sujeta al proceso de admisión y cambio de carrera vigente de la UTA (RRA, art. 79; instructivo UTA citado);
   - que la decisión corresponde al proceso institucional.
2. **Malla de origen no vigente (usa HU-07):** el informe indica que la malla de origen no está vigente. La ruta de cambio de plan de estudios, con su tabla de homologación e informe académico, **la confirma la carrera** para el caso concreto. No se compara automáticamente contra otra malla.
3. La alerta no modifica los veredictos del motor.

### HU-17 · Tercera matrícula (RN-07)

1. Una asignatura de origen aprobada en **tercera matrícula queda excluida** de la homologación. **La solicitud no se rechaza entera**: las demás asignaturas se evalúan normalmente. Esto sustituye la regla propuesta antes, «cambio no procedente».
2. Como la exclusión es de la propia asignatura, ya no hace falta decidir cómo «considera» la malla receptora la asignatura. Ninguna pareja que use esa asignatura de origen produce «Homologable».
3. Las reprobadas en tercera matrícula ya quedan fuera por HU-15 (no aprobadas).
4. **Evidencia:** el número de matrícula se registra junto con la asignatura de origen y una referencia al historial o certificado adjunto (HU-12). Sin esa referencia, el dato queda como **información incompleta**; no basta la declaración del estudiante.
5. Aplica igual al cambio interno y al externo.
6. Análisis e informe muestran el motivo: «Excluida por tercera matrícula (RN-07)».

### HU-03 · Parámetros

1. **Parámetros:**
   - umbral de equivalencia, 80 %;
   - años máximos desde la aprobación, 10 años (provisional, ver hallazgos);
   - mínimos de cambio interno y externo, los vigentes en HU-16.

   Los valores iniciales son idénticos a los actuales, así que las pruebas existentes no cambian.
2. **Quién los cambia:** ninguna fuente designa un rol. La propuesta es el rol académico **Coordinador**, no el Administrador técnico. La **PO confirma el rol** antes de cerrar T56. Hasta entonces el permiso queda en una sola política, fácil de cambiar.
3. **Historial obligatorio:** cada cambio registra actor, fecha, valor anterior y nuevo, motivo y la referencia al acto o autorización que lo respalda.
4. **Instantánea:** cada análisis guarda los parámetros con los que se calculó. Un cambio posterior no altera análisis ya emitidos. Recalcular es una acción explícita y queda registrada.

### HU-32 / HU-33 · Remisión y finalización

La doble revisión docente es una regla del flujo del proyecto, no de la norma.

1. **Remitir** solo cuando todas las parejas tienen los análisis de sus dos docentes; las discrepancias aparecen como «Sin acuerdo».
2. **«Sin acuerdo»:** el coordinador registra una **decisión académica propia, motivada y trazable** (homologa / no homologa, justificación, fecha y autor). No elige automáticamente entre los dos docentes. Si la decisión corresponde a otro órgano, el texto lo indica como propuesta.
3. **Finalizar** bloquea la edición ordinaria de la solicitud, los análisis y el informe.
4. **Fuera del Sprint 3** (candidatas para el Product Backlog, a decisión de la PO):
   - devolver el informe a los docentes con una observación;
   - reabrir una solicitud finalizada con autorización, motivo y auditoría.

### HU-37 · Reutilizar el análisis del agente

Ninguna norma lo regula; es una decisión técnica y de gobernanza.

1. **La clave de reutilización** combina:
   - el par de asignaturas;
   - las firmas SHA-256 de ambos programas y de las correspondencias revisadas;
   - el umbral y los parámetros de HU-03;
   - el modelo;
   - la **versión de la plantilla del prompt**.

   Con cualquier cambio en la clave, el análisis guardado no se reutiliza.
2. **No caduca por tiempo**: no hay ninguna política definida que lo pida.
3. Solo se guardan y reutilizan análisis **completos**.
4. **Forzar un análisis nuevo:** solo el docente asignado, con un motivo registrado. El análisis anterior se conserva en el historial y no se borra.
5. La interfaz muestra «Reutilizado», con la fecha y el identificador del análisis original.

### HU-24 · Informe en PDF

No hubo respuesta sobre un formato oficial. El PDF reproduce el contenido del informe y lleva la leyenda «Formato del sistema; no sustituye el formato oficial de la institución». La PR abierta [#83](https://github.com/PabloToapanta/ProyectoHomologacion/pull/83) se revisa con este criterio.

### Programa analítico de la universidad de origen (D2 de HU-19)

Sin respuesta. Sigue fuera del Sprint 3; el análisis externo continúa en «información incompleta» cuando falta ese programa.

## Pendientes de confirmación

| Pendiente | Responsable | Afecta |
| --- | --- | --- |
| Antigüedad vigente: 10 o 5 años | Coordinación / UTA | HU-15, HU-03 |
| Créditos como criterio propio y los tres criterios del motor | Coordinación | HU-14, informe |
| Alcance N:1 de la agrupación, suma de horas y créditos, y uso único del origen | Coordinación | HU-21 |
| Rol que cambia los parámetros | PO | HU-03 |
| Atribución del coordinador para decidir las parejas «Sin acuerdo» | Coordinación | HU-33 |
| Devolución y reapertura como historias nuevas | PO | Product Backlog |
