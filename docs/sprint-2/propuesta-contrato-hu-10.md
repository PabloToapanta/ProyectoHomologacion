# Propuesta de contrato HU-10 — Issue #38

Estado: contrato de implementación HU-10; aceptación PO y coordinación de integración pendientes. Base: develop d01ece3.

- Nuevo solicitudes-service con Domain, Application, Infrastructure y Api; base SQL Server exclusiva.
- POST /api/solicitudes: solo Coordinador. Registro manual de número de trámite Quipux, identificación del estudiante, tipo Interno/Externo, carrera origen y carrera receptora. ID generado por servidor, estado inicial Registrada.
- GET /api/solicitudes y GET /api/solicitudes/{id}: consulta restringida a Coordinador en HU-10; acceso docente se incorpora por HU-30 con asignaciones HU-29.
- Receptora y origen interno referencian catálogo FISEI; origen externo se representa separado (institución y carrera), sin crear carreras externas en el catálogo FISEI. No acceder a bases ajenas.
- Errores: 400 datos inválidos, 401 sin sesión, 403 rol no autorizado, 404 referencia/solicitud inexistente según contrato definitivo. No guardar registros parciales.
- Decisión de Edwin (28/10/2026): cada número de trámite Quipux identifica una única solicitud. Unicidad global, normalizada con Unicode FormKC, Trim y mayúsculas, protegida por índice único SQL; duplicado devuelve 409. Un estudiante puede tener trámites distintos, sin restricción adicional no acordada.
- Pendiente de coordinación técnica con Pablo/Leslie: validación de referencias del catálogo y cambios de rutas Gateway/React, JWT cifrado HU-35 y clave interna HU-36.
- TDD: obligatorios/tipo inválido; persistencia y consulta; autorización; aislamiento por solicitud. Ver evidencia de implementación en docs/sprint-2/evidencia-hu-10.md.

Fuera: integración automática Quipux, PDF, aprobadas HU-11, asignaciones HU-29 e informes.

## Integración propuesta en archivos compartidos
Consulta nueva GET /api/carreras-solicitudes, solo Coordinador, devuelve id/nombre de carreras FISEI vigentes; no modifica los permisos CRUD existentes. Solicitudes reenvía el JWT al catálogo mediante HTTP y falla con 503 si no puede validarlo. Gateway añade un cluster/ruta para /api/solicitudes; React añade /solicitudes y lo usa como destino del Coordinador al iniciar sesión. No se modifica el emisor JWT ni se adelanta HU-35/36; sus responsables deben incorporar el nuevo servicio al cambiar el contrato de seguridad.
