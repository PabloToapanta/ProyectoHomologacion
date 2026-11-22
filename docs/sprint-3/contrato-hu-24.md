# Contrato HU-24 — exportar el informe a PDF

Issue [#72 — Exportar el informe a PDF](https://github.com/PabloToapanta/ProyectoHomologacion/issues/72). Rama: `feature/hu-24-informe-pdf`, desde `develop`. HU-24 extiende la consulta del informe fijo de HU-23 (#61); no modifica cómo se calcula ni genera ese informe.

## Decisiones de alcance

- **PDF desde el navegador:** la acción «Exportar informe a PDF» abre el diálogo nativo de impresión. La persona selecciona «Guardar como PDF». No se añade una librería de PDF al servidor ni una dependencia nueva; el estilo de impresión prepara una página A4 horizontal que contiene solo el informe.
- **Coordinación:** Shirley, Product Owner, confirmó el 15/11/2026 que cualquier Coordinador puede consultar y exportar un informe existente de una solicitud FISEI. El registro de solicitud comprueba que la carrera receptora está vigente en el catálogo FISEI. La lectura interna valida que la solicitud exista y entrega solo el contexto académico mínimo, sin Quipux, identificación, nombre ni matrícula.
- **Docentes:** solo los dos docentes asignados pueden generar y consultar el informe; uno no asignado recibe 404. La generación sigue siendo exclusiva de los docentes asignados. Coordinación no puede generar ni modificarlo.
- **No disponible antes de existir:** si el informe no se ha generado, no se presenta el botón de exportación. La respuesta sigue siendo 404 y no se produce un archivo.

## Contenido del PDF

El informe fijo de HU-23 se imprime sin recalcularlo: parejas de asignaturas, estado y sugerencia del agente, porcentajes académico/horas/créditos, posturas/decisiones/observaciones de ambos docentes, sugerencia final, asignaturas sin análisis y advertencias. La cabecera muestra identificador, tipo de cambio, fecha y autor de generación. El documento declara que es técnico y no constituye una resolución oficial.

## Acceso y respuestas

| Caso | Resultado |
| --- | --- |
| Docente asignado o Coordinador autorizado; informe existente | GET 200; habilita impresión/guardar como PDF |
| Sesión ausente | 401 |
| Administrador u otro rol | 403 |
| Docente no asignado, solicitud inexistente o informe aún no generado | 404 |
| POST de generación con rol Coordinador | 403 |

No se añade un endpoint de PDF binario: el endpoint GET de HU-23 entrega la representación fija que el navegador imprime. Swagger documenta quién consulta y quién genera.

