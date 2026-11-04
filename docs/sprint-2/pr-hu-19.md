# feat(homologacion): implementar análisis externo HU-19

Refs #43. Base: `develop`.

## Comportamiento

El Coordinador registra períodos cursados y cambios previos para solicitudes externas.
El docente asignado puede analizar una pareja, recuperar el resultado persistido y
consultar RN-05. Homologación conserva los datos en `AnalisisExternos` mediante una
nueva migración; Solicitudes añade dos columnas anulables mediante su propia migración.
La lectura interna requiere JWT, clave interna y asignación; no expone datos personales.

Se comparan horas y créditos únicamente cuando las unidades coinciden tras recortar
espacios y sin distinguir mayúsculas. Sin contenidos académicos externos registrados,
el análisis queda `Incompleto`, sin sugerencia ni invocación del agente.

## Alcance y revisión pendientes

- La evidencia registra aprobación comunicada por Edwin de D1–D4 y coordinación con
  Leslie. El Issue #43 todavía conserva los criterios originales de institución
  extranjera, apostilla y agente común: requiere registrar el acuerdo y ajustar
  criterios antes de aceptar la historia. No cerrar #43 automáticamente.
- Con datos reales actuales, RN-05 no puede llegar a «Cumple» porque ningún análisis
  externo puede ser completo. Las pruebas de ese estado usan resultados sintéticos.
- La edición de datos se bloquea desde la asignación, incluso antes del primer análisis.
  Shirley lo confirmó (comunicado por Edwin el 04/11/2026); registrarlo en #43.
- Concurrencia corregida: `Estado` es token de concurrencia, por lo que una edición
  iniciada antes de asignar y guardada después recibe 409 sin modificar datos.
  Probado con intercalado determinista (SQLite y SQL Server real) y ejecuciones en
  paralelo con SQL Server real.
- Pendientes CI, revisión cruzada de Leslie y aceptación de Shirley.

## Validación

- Nueva revisión: 755 pruebas backend aprobadas y 1 omitida (Ollama real),
  incluyendo Solicitudes e infraestructura con SQL Server 2022 aislado.
- Frontend: 128/128, comprobación TypeScript y compilación Vite aprobadas.
- `git diff --check` sin errores.
- Tras la corrección de concurrencia: Solicitudes con SQL Server real 146/146 y
  `hu19-smoke.py` 46/46 sobre copia limpia.

Consultar [evidencia-hu-19.md](evidencia-hu-19.md) para ejecuciones previas y la nueva
revisión. Esta preparación conserva fuera del PR los puertos locales de Docker,
`SeleccionCatalogo.tsx`, `tmp/` y `tools/`.
