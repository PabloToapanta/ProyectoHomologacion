# Preparación técnica para aceptación — Sprint 1

21/10/2026. Preparado por Pablo. Relacionado con Issues #1–#7 y #14.
Este documento facilita la comprobación de Shirley; **no registra aceptación**.
La cobertura técnica y sus límites están en [verificación final](evidencia-verificacion-final.md).

## Entorno y datos

Demostración local: <http://127.0.0.1:5173>. Credenciales únicamente en el archivo
privado `.env.demo.acceso.txt`; no copiarlas a Issues, capturas ni documentos públicos.
Gateway: puerto 5080. Swagger: Auth 5081, Catálogo 5082, Homologación 5083.

Para HU-06, seleccionar la carrera **Software FISEI — Demostración Sprint 1**,
asignatura **Álgebra Lineal**, código de prueba `DEMO-ALG-LIN-S1`, nivel 1,
3 créditos y 144 horas. Tiene cuatro unidades de 36 horas y ocho contenidos,
procedentes de la transcripción ya publicada por Edwin en `examples/hu-06`.
El código y la carrera son de demostración, no identificadores institucionales.
Shirley/equipo deben cotejar la transcripción con el programa analítico autorizado.

Los registros históricos anteriores se preservan y siguen identificados para
revisión; no se convirtieron ni eliminaron carreras con otra facultad.

## Recorrido de aceptación humana

Usar nombres/códigos claramente sintéticos para nuevas pruebas; no editar datos
reales ni eliminar registros relacionados para forzar un resultado positivo.
Registrar esperado, resultado real, captura/salida, fecha y responsable por criterio.
Las filas siguientes son un recorrido resumido; se deben comprobar también todos
los [criterios vigentes](criterios-aceptacion.md).

| Historia | Comprobación propuesta | Aceptación de Shirley |
|---|---|---|
| HU-01 | Login correcto; incorrecto sin token; logout; acceso protegido sin sesión y API 401 | Pendiente |
| HU-04 | Crear/editar con FISEI fija; duplicado 409; otra facultad por API 400; borrar sin relaciones y rechazar con asignaturas | Pendiente |
| HU-05 | Registrar con nivel/horas/créditos positivos; rechazar cero/negativos; consultar por carrera y orden por nivel | Pendiente |
| HU-06 | Ver cuatro unidades, contenidos y resultados; registrar una unidad sintética; rechazar campos incompletos; corregir por edición y consultar persistencia | Pendiente |
| HU-14 | En `/validaciones`, comprobar positivo, insuficiente e incompleto. Por API, usar el ejemplo publicado con tres criterios al 80 %, límites y múltiples orígenes sin doble conteo | Pendiente |
| HU-15 | Aprobada menos/exactamente diez años, más de diez, reprobada/no cursada; falta de fecha y fecha de evaluación futura con error | Pendiente |
| HU-16 | Interno mínimo 1 período/1 homologable; externo 2/2/sin cambio previo; incumplir cada condición y revisar los motivos | Pendiente |

HU-14 compara contenidos/resultados con evidencia explícita; no genera correspondencias
automáticas. El formulario admite un par y la API varios orígenes. Sin reglas de
agregación/conversión aprobadas, el resultado correspondiente es información incompleta.
`Elegible`, `Homologable` y cumplir mínimos son resultados independientes; ninguno
es una resolución institucional ni autoriza automáticamente el cambio.

## Registro y cierre

- Shirley completa los resultados por criterio y registra su decisión en los Issues.
- El equipo registra las revisiones cruzadas pendientes; una prueba automática
  o un merge no sustituyen la revisión humana.
- Actualizar los Issues históricos de HU-01/HU-14/HU-15/HU-16 para reflejar el código
  y las evidencias vigentes, sin marcar aceptación por cuenta de la PO.
- Registrar Sprint Review y retrospectiva cuando se realicen, con asistentes,
  decisiones y resultados reales. No inventar una fecha o aceptación previa.
- Cerrar historias solo con evidencia de los criterios y definición de terminado.

**Decisión de aceptación:** pendiente. **Responsable:** Shirley.
**Fecha y observaciones:** pendientes de su ejecución.
