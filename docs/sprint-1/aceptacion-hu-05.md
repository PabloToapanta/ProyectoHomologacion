# Acta de aceptación HU-05 — Gestión de asignaturas

Issue #3. Responsable de aceptación: **Shirley Amaguaña (Product Owner)**. Fecha y resultado de aceptación: **pendientes de la revisión humana**.

El código fue aprobado por Pablo e integrado mediante PR #20. La revisión de código no sustituye la aceptación de Shirley. Las pruebas deben realizarse con datos de demostración; `DEMO-ALG-LIN-001` no es un código académico institucional.

## Preparación

1. Abrir el entorno local de demostración en `http://127.0.0.1:5173`.
2. Leslie proporciona por un medio privado las credenciales locales de administrador; no copiarlas al Issue ni al acta.
3. Entrar al panel y pulsar **Gestionar carreras y asignaturas**.

## Recorrido de aceptación

| Paso | Acción | Resultado esperado | Resultado de Shirley |
|---|---|---|---|
| 1 | En Carreras, registrar Software en una facultad de demostración diferente de las ya existentes | Carrera visible en la lista | Pendiente |
| 2 | Abrir Asignaturas, seleccionar esa carrera y registrar una asignatura con nivel 2, 3 créditos y 144 horas | Registro confirmado y visible en la carrera seleccionada | Pendiente |
| 3 | Registrar Álgebra Lineal con código DEMO-ALG-LIN-001, nivel 1, 3 créditos y 144 horas | Registro confirmado; aparece antes del nivel 2 | Pendiente |
| 4 | Intentar registrar otra asignatura con créditos 0 y luego -1 | Mensaje de validación, sin crear registro | Pendiente |
| 5 | Repetir con horas 0 y luego -1 | Mensaje de validación, sin crear registro | Pendiente |
| 6 | Cambiar de carrera y regresar a la anterior | Cada carrera muestra únicamente sus asignaturas, ordenadas por nivel | Pendiente |
| 7 | Volver a Carreras e intentar borrar la carrera con asignaturas | Eliminación impedida y datos conservados (regresión HU-04) | Pendiente |
| 8 | Cerrar sesión e intentar acceder directamente a /admin/carreras | Redirección al inicio de sesión | Pendiente |

## Decisión

- [ ] Registrar asignatura con nivel, horas y créditos positivos vinculada a una carrera: aceptado.
- [ ] Rechazar horas o créditos menores o iguales a cero: aceptado.
- [ ] Listar asignaturas de una carrera ordenadas por nivel: aceptado.
- [ ] Se revisaron las evidencias de integración real y las observaciones pendientes.
- [ ] Shirley acepta HU-05.

**Nombre de quien acepta:** pendiente.
**Fecha:** pendiente.
**Observaciones/correcciones solicitadas:** pendiente.

Solo después de registrar esta decisión se completa la aceptación en el Issue #3 y se considera su cierre. No marcar estas casillas automáticamente por pasar pruebas.

## Entrega para Edwin — HU-06

Carrera y Asignatura ya están en develop mediante PR #19/#20. Se conservan UUID y CarreraId; la tabla Asignaturas y el DbContext son del catálogo. La base para unidades y contenidos es la entidad de dominio Asignatura, con casos de uso en Application y persistencia en Infrastructure.

Edwin debe actualizar su base de trabajo sin copiar los commits de Leslie, usar AsignaturaId para sus relaciones y acordar la ampliación del DTO antes de incorporar unidades en GET /api/asignaturas/{id}. Los temas/resultados de aprendizaje necesitan el programa analítico real; no se inventan a partir de la demostración. Ver `docs/arquitectura/propuesta-integracion-hu-05.md`.

Esta sección prepara el traspaso; no afirma que se haya enviado un mensaje externo ni que Edwin haya empezado HU-06.
