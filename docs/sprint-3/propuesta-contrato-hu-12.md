# Propuesta de contrato — HU-12 (Issue #70)

Estado: propuesta local para revisión cruzada de Edwin antes de integrar; la PO confirmó CA1–CA5 y fijó el límite técnico en 20 MiB por PDF el 15/11/2026.

## Contrato propuesto

- `POST /api/solicitudes/{id}/adjuntos`: solo Coordinador; multipart con `tipo` (`Historial academico` o `Silabo`) y `archivo`; valida extensión, MIME y firma `%PDF-`; máximo 20 MiB por archivo. Guarda PDF y metadatos en SQL Server del servicio Solicitudes.
- `GET /api/solicitudes/{id}/adjuntos`: Coordinador consulta el expediente; Docente solo si está asignado, en otro caso 404.
- `GET /api/solicitudes/{id}/adjuntos/{adjuntoId}`: Coordinador y docentes asignados descargan el PDF. La identidad procede de `sub` del JWT.
- Metadatos: nombre saneado, tamaño en bytes, SHA-256 hexadecimal, tipo, `sub` del usuario que subió y fecha UTC del servidor.
- Un archivo que no sea PDF o exceda 20 MiB devuelve 400 con ProblemDetails legible; solicitud inexistente 404; operación tras estado `Finalizada` devuelve 409.
- La escritura serializa la comprobación del estado y el guardado para evitar aceptar adjuntos después de que HU-33 finalice la solicitud.

## Coordinación pendiente

HU-33 (#64) incorpora el estado `Finalizada` y restringe cambios posteriores. Antes de integrar, Edwin debe revisar el contrato de bloqueo concurrente y verificar que su implementación de finalización utiliza el mismo límite transaccional en `SolicitudesDbContext`. La revisión cruzada indicada por el sprint corresponde a Leslie.
