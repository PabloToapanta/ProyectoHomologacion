# HU-14 — Selector de asignaturas del catálogo

**Histórico:** la ampliación posterior solicitada el mismo día sustituye este formulario y sus límites. Ver [comparador simple y catálogo](comparador-simple-catalogo.md) para la implementación vigente, datos insertados y verificación integrada.

Fecha: 21/10/2026. Issue [#5](https://github.com/PabloToapanta/ProyectoHomologacion/issues/5), T9/T10.
Rama local: `feature/hu-14-comparador-catalogo`, creada desde `origin/develop` (`6aba3dd`).

## Comportamiento implementado

- Administrador: selección de carrera y asignatura de origen y receptora mediante las API existentes, con carga de horas, créditos, contenidos y resultados de aprendizaje. Las cantidades cargadas son de solo lectura.
- El usuario confirma aprobación, referencia de plan, unidad horaria y sistema de créditos porque no existen en el DTO del catálogo. No se presume comparabilidad ni evidencia académica.
- Correspondencias explícitas por UUID de contenido y por UUID de unidad para su resultado de aprendizaje. Las listas del catálogo no se concatenan con las temáticas: no se duplican contenidos.
- Sin unidades, se envían inventarios desconocidos (`null`), para que el motor informe incompletitud. No se representa la falta de registro como un inventario conocido vacío.
- Cambiar carrera o materia descarta resultados, confirmaciones y correspondencias anteriores. Peticiones canceladas no reponen datos obsoletos. Hay errores y reintentos de lectura.
- Comparar la misma asignatura en ambos lados se rechaza antes de invocar el motor.
- Tres barras accesibles: académica combinada, horas y créditos. El umbral mostrado es el devuelto por el servidor; el usuario no puede sobrescribirlo mediante el cuerpo HTTP. Los elementos sin cobertura muestran su texto cuando provienen del catálogo.
- La entrada manual sigue disponible. Docente/Coordinador comienzan en ese modo porque el contrato actual de catálogo solo admite Administrador; no se ampliaron permisos.

## Verificación técnica

Se ejecutaron primero dos pruebas del flujo del catálogo contra el formulario anterior: ambas fallaron por ausencia de selectores y de gestión del catálogo. Después de implementar se verificó el envío de las instantáneas y correspondencias, los reintentos, la comparación consigo misma, inventarios desconocidos y conservación del modo manual.

Se ejecutó la comprobación de TypeScript y la compilación de producción con éxito. Se usó el Node incluido en el entorno de Codex porque el lanzador local de npm no encuentra la versión requerida; no se cambió la configuración del proyecto.

Resultado final de `vitest run --reporter=dot --maxWorkers=2`: **12 archivos y 98 pruebas aprobadas**, incluyendo seis pruebas nuevas del selector. `tsc --noEmit`, `vite build` y `git diff --check` también finalizaron correctamente. Una ejecución previa con todos los trabajadores predeterminados agotó el tiempo de una prueba y detectó una consulta ambigua del test; se corrigió la consulta y se redujo la concurrencia para verificar la suite completa.

Las respuestas HTTP de las pruebas de interfaz son simuladas con datos sintéticos. No constituyen evidencia de integración con SQL Server, autenticidad de planes ni corrección semántica de correspondencias.

## Pendientes de entrega

No se ejecutaron Docker Compose, prueba con la base real ni revisión visual en navegador en esta tarea. No se modificaron servicios, esquema SQL ni contratos HTTP; no hay endpoints nuevos que documentar en Swagger. Pendientes: revisión cruzada de Edwin, aceptación de Shirley e integración real antes de declarar la historia terminada. No se hizo push, merge ni actualización del Issue remoto.

Ver [propuesta de contrato y alcance](../arquitectura/propuesta-hu14-selector-catalogo.md).
