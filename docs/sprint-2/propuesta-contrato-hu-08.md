# Propuesta de contrato HU-08 — carga masiva de programas analíticos

Estado: propuesta para acordar antes de codificar. Issue [#50](https://github.com/PabloToapanta/ProyectoHomologacion/issues/50), sin implementación y sin aceptación de la PO. Decisión de mantener HU-08 en Sprint 2 transmitida por el usuario el 01/11/2026; falta confirmación de Shirley.

Historia del Sprint 0: "Como administrador, quiero cargar de forma masiva los programas analíticos (PDF) de las carreras de la FISEI." Prioridad media, 5 PH. Los programas "siguen el mismo formato, por lo que se importan con extracción de texto y revisión por el agente".

## Alcance propuesto

Incluye la carga de PDF con texto de programas **FISEI** hacia el catálogo, con revisión humana antes de guardar.

Fuera de HU-08:

- PDF de universidades externas: pertenecen a la solicitud y se relacionan con HU-12 y HU-19.
- OCR de documentos escaneados.
- Comparación de asignaturas y veredicto.
- Publicar programas reales en el repositorio: es público, por lo que las pruebas usan PDF sintéticos.

## Flujo

1. El Administrador selecciona la carrera y uno o varios PDF.
2. El servicio valida cada archivo y extrae el texto con código determinista.
3. Un analizador del formato FISEI genera un borrador por archivo: código, nombre, nivel, créditos, horas, unidades, horas de unidad, resultados de aprendizaje y contenidos.
4. El Administrador revisa y corrige el borrador.
5. Al confirmar, los datos se guardan con las reglas existentes de HU-05 y HU-06.

Nada entra al catálogo sin confirmación humana. Un dato que no se encuentre queda vacío y marcado; no se completa por suposición.

## Decisión recomendada: el agente es opcional en la primera versión

El Sprint 0 prevé revisión por agente, pero HU-18 aún no existe y define el adaptador de Ollama en `homologacion-service`. Hacer que HU-08 dependa de él une los riesgos de ambas historias.

Propuesta: primera versión con extractor y analizador deterministas más revisión humana, detrás de un puerto `IExtractorPrograma`. Si el adaptador de HU-18 llega a tiempo, el agente puede sugerir correcciones al borrador con JSON validado, citando el texto. La revisión por agente no sería criterio de aceptación de esta versión, lo que **reduce la historia original** y debe aprobarlo Shirley.

## Contrato inicial en catalogo-service

Todas las rutas son solo para Administrador y requieren declararse en el Gateway.

| Método | Ruta | Resultado |
| --- | --- | --- |
| POST | `/api/importaciones-programas` | Multipart con `carreraId` y archivos: 201 con un borrador o error por archivo |
| GET | `/api/importaciones-programas/{id}` | Lote con el estado de cada archivo |
| PUT | `/api/importaciones-programas/{id}/borradores/{borradorId}` | Guarda correcciones |
| POST | `/api/importaciones-programas/{id}/borradores/{borradorId}/confirmacion` | Guarda en el catálogo; 409 si el código ya existe en la carrera |

Estados de cada archivo: `Borrador`, `SinTexto`, `Invalido`, `Confirmado`. Un archivo fallido no detiene el resto del lote. Los borradores son propiedad de Catálogo, en su propia base.

## Criterios de aceptación propuestos

Para validación de Shirley.

- **CA1.** Un Administrador carga varios PDF de una carrera y obtiene un resultado por archivo.
- **CA2.** Un PDF con el formato FISEI genera un borrador con asignatura, unidades, resultados y contenidos, sin guardar aún en el catálogo.
- **CA3.** El Administrador corrige el borrador y, al confirmar, los datos se consultan en el catálogo con las validaciones de HU-05 y HU-06.
- **CA4.** Un archivo que no es PDF, excede el tamaño acordado, no tiene texto o no sigue el formato se informa sin afectar a los demás; nada se inventa.
- **CA5.** Un código ya registrado en la carrera no se sobrescribe.
- **CA6.** Coordinador y Docente reciben 403; sin sesión, 401. El servidor no confía en el nombre ni en el tipo declarado del archivo.

## Tareas propuestas

Estimación pendiente de Planning. HU-08 solo tenía puntos, sin horas.

| Tarea | Actividad | Horas |
| --- | --- | --- |
| T40 | Carga, validación de archivos, extracción de texto y analizador con PDF sintéticos | 5 |
| T41 | Borradores, corrección y confirmación hacia el catálogo | 3 |
| T42 | Interfaz de carga y revisión | 3 |
| T43 | Gateway, pruebas de integración y evidencia | 1 |

Total propuesto: 12 h.

## Por decidir antes de codificar

1. Responsable y revisor cruzado. Catálogo es de Leslie, unidades de Edwin y Gateway de Pablo.
2. Aprobación de Shirley para el alcance sin agente obligatorio.
3. Biblioteca de extracción de texto y revisión de su licencia.
4. Tamaño máximo por archivo y por lote, y si el PDF original se conserva o se descarta tras extraer el texto.
5. Un programa analítico real, solo para desarrollo local, para fijar el formato. El Sprint 0 afirma que es común, pero no se ha comprobado aquí.
6. Qué ocurre con la asignatura ya registrada: rechazar o actualizar. Esta propuesta la rechaza.
7. Cómo incorporar HU-35 y HU-36 si cambian la autenticación de estas rutas.

## Riesgo de calendario

Queda una jornada planificada, el 4 de noviembre, con 9,6 h efectivas si los cuatro integrantes confirman 3 h. El plan no programa historias nuevas ese día. Con HU-08, el compromiso sube a 45 PH y 81 h propuestas frente a 76,8 h de capacidad efectiva, sin descontar el trabajo aún no iniciado. La PO debe ajustar alcance o disponibilidad.
