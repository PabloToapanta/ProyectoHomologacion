# HU-06 / Issue #4 — aclaración del docente

Edwin confirma el 21/10/2026 que el docente exige la estructura asignatura → unidad
curricular → unidades temáticas → contenidos mínimos. Horas y resultado corresponden
a la curricular; no se distribuyen ni duplican por temática. Microservicios permanecen
activos y se invocan según necesidad; no se requiere arranque automático.

Se amplía catálogo y UI con tematicas[{nombre,contenidos[]}]. Se mantienen el cuerpo
antiguo contenidos[] y la respuesta plana para consumidores existentes; se rechaza
mezclar ambas entradas. El DTO nuevo añade tematicas y contenidosSinClasificar.
Migración aditiva con FK nullable: los contenidos anteriores conservan IDs y texto,
sin asignación automática inventada. UI los identifica pendientes de clasificación
y permite repartirlos manualmente al editar. No se modifica Gateway ni el motor.
Cambios compartidos: DTO y mapeo EF del catálogo y su panel; revisión Leslie/Pablo
antes del merge. Cambios locales de puertos Docker se conservan fuera de este trabajo.
