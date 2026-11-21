# test(homologacion): verificar persistencia HU-20 y proponer contrato HU-19

Base: `develop`. Rama: `feature/hu-20-persistencia-analisis`.

## Cambios

La persistencia de análisis internos ya está integrada mediante HU-18. Este PR
añade cobertura con SQL Server real para guardar y recuperar resultados desde
conexiones nuevas, conservar estados incompletos/fallidos, reemplazar la misma
pareja sin duplicados y controlar errores en primeras inserciones concurrentes.
Incluye un workflow independiente para ejecutar esa cobertura en GitHub Actions.

Refs #44. La evidencia y los límites están en [evidencia-hu-20.md](evidencia-hu-20.md).
La política de último resultado por pareja requiere confirmación de Shirley.

## HU-19: propuesta, sin implementación

Refs #43. [propuesta-contrato-hu-19.md](propuesta-contrato-hu-19.md) identifica las
decisiones pendientes sobre alcance nacional/extranjero, programas externos,
comparabilidad de unidades y datos de RN-05. No acredita HU-19 terminada ni cambia
sus criterios. Requiere decisión de Shirley y coordinación con Leslie antes de
codificar. No cerrar #43 con este PR.

## Validación y revisión

Consultar la evidencia HU-20 para las ejecuciones registradas. La nueva ejecución
de CI, la revisión cruzada de Leslie y la aceptación de Shirley siguen pendientes.
No incluye cambios de producto ni los ajustes locales de puertos de Docker,
SeleccionCatalogo.tsx, herramientas o archivos temporales.
