# HU-09 · Consulta de materias homologables entre carreras FISEI — evidencia

Issue [#69](https://github.com/PabloToapanta/ProyectoHomologacion/issues/69). Rama `feature/hu-09-consulta-homologables`, desde `develop` (`3df758a`).

- **Responsables planificados:** Pablo y Leslie.
- **Quién la implementó:** Edwin, el 24/11/2026, con la aprobación de la PO (Shirley) transmitida por Edwin.
- **Revisión cruzada:** Pablo o Leslie. Hay que revisar sobre todo los archivos compartidos: `App.tsx`, `ProtectedRoute.tsx`, `WorkspaceLayout.tsx` y el Gateway.
- **Pendiente:** la aceptación de la PO.

## Alcance

Implementa CA1–CA4 del issue.

- **Pares que se muestran:** solo los que tienen una revisión de correspondencias registrada en Catálogo. Los demás no se infieren ni aparecen.
  - Si un programa cambió después de revisarse, o le falta el perfil o las unidades, el par aparece como «Información incompleta», con lo que falta (CA2).
- **Evaluación:** se usa el motor determinista de HU-14 con los parámetros vigentes de HU-03. Las horas y los créditos salen del catálogo, no de un estudiante.
  - Por cada par se muestran los tres porcentajes, el umbral y el veredicto (CA1).
- **Solo lectura (CA3):** no crea ni modifica correspondencias, no guarda resultados y no llama al agente.
- **Mallas (HU-07):** en el origen entran todas las versiones, porque el estudiante pudo cursar una anterior. En la receptora, solo la vigente.
- **Carreras:** solo FISEI. Una carrera de otra facultad responde 400.

## API

| Servicio | Ruta | Acceso |
| --- | --- | --- |
| Homologación (publicada en el Gateway con `AcademicAccess`) | `GET /api/homologaciones/consulta-homologables?origen=&receptora=` | Docente y Coordinador. Sin sesión 401; Administrador 403. Responde 400 con carreras vacías, iguales o no FISEI; 404 si una carrera no existe; 503 si Catálogo no responde |
| Homologación (publicada) | `GET /api/homologaciones/consulta-homologables/carreras` | Igual |
| Catálogo (interna, **no** publicada) | `GET /api/consulta-homologables/carreras` y `GET /api/consulta-homologables/carreras/{origen}/{receptora}/contexto` | Docente y Coordinador. JWT reenviado más clave interna |

## Interfaz

- **Pantalla nueva «Homologables FISEI»** (`/consulta-homologables`), en el menú lateral del docente y del coordinador.
- Se eligen dos carreras y se ve la tabla de pares con porcentajes, resultado del motor, umbral y lo que falta.
- **Cambio en `ProtectedRoute`:** ahora admite varios roles. La página necesita Docente y Coordinador, y dos guardas de un solo rol se bloquearían entre sí.

## Verificación del 24/11/2026

| Suite | Resultado |
| --- | --- |
| Catálogo (7 de HU-09) | 151 superadas, 4 omitidas (SQL Server real) |
| Homologación Application (7 de HU-09) | 257 superadas |
| Homologación Api (5 de HU-09) | 166 superadas |
| Homologación Infrastructure | 91 superadas, 18 omitidas |
| Gateway | 156 superadas |
| Frontend (3 de HU-09) | 219 superadas; `tsc` y `vite build` correctos |

Las pruebas se escribieron antes que el código, pero no se ejecutó la ronda en rojo antes de implementar. No se ejecutó Compose ni el navegador real.
