# feat(homologacion): registrar revisión propia de los docentes HU-31

Refs #59. PR hacia `develop`, dependiente de #56 (HU-19), todavía abierto al
preparar esta rama. **Integrar después de #56.** Hasta entonces la comparación
con `develop` también incluye los commits de HU-19; el incremento propio parte
de `3b2e291` y su implementación principal es `6cc6305`.

## Comportamiento

Cada docente asignado registra su revisión por pareja de asignaturas y consulta
la del otro. «De acuerdo» requiere un análisis automático completo y conserva su
sugerencia; «Ajuste» exige decisión y observación y admite un análisis incompleto
o no ejecutado. La identidad sale del JWT y el servidor comprueba la asignación
en Solicitudes y las asignaturas en sus fuentes.

Guarda el último registro propio, con fecha, docente, estado y sugerencia del
análisis automático que se revisó. Nueva tabla `RevisionesDocentes` con migración,
API GET/PUT, ruta del Gateway y pantalla «Mi análisis». Un fallo de almacenamiento
devuelve 503 sin confirmar el guardado. No incluye informes ni remisión o cierre.

La revisión corrige además un aviso de éxito incorrecto al cambiar de pareja
mientras se guarda: las respuestas de la pareja anterior ya no actualizan la nueva.

## Alcance y aceptación

Historia de Sprint 3 adelantada según autorización de Shirley comunicada por Edwin
en el contrato local. Registrar ese acuerdo en #59; la publicación no acredita
aceptación de la PO. Requiere revisión cruzada de Leslie y Pablo, CI y prueba manual
en navegador. No cerrar #59 automáticamente.

Queda pendiente probar «De acuerdo» de punta a punta con análisis automático completo;
las pruebas de dominio, aplicación, API y frontend cubren esa postura con datos
sintéticos. El smoke integrado cubre el flujo externo de ajuste con SQL Server real.

## Validación

- Backend: 689 pruebas aprobadas y 1 omitida (Ollama real), con .NET 8 y SQL Server
  2022 temporal; incluye API, reglas, persistencia y regresión de Solicitudes.
- Frontend: 132/132, TypeScript y compilación Vite aprobados.
- Smoke integrado: 32/32 con JWT cifrado y SQL Server real; permisos, ambos
  docentes, corrección propia, reinicio y fallo de almacenamiento controlado.
- La prueba de cambio de pareja durante un guardado falló antes de la corrección
  y pasó después. Se corrige también un total aritmético en la evidencia heredada
  de HU-19, conservando su desglose original.

Evidencia y límites: `docs/sprint-3/evidencia-hu-31.md`.
Los ajustes locales de puertos Docker, SeleccionCatalogo.tsx, herramientas y temporales
quedan fuera de este PR. Sin merge ni release.
