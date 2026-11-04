# feat(catalogo): cargar programas analíticos con revisión humana

Refs #50. Base: `develop`. Rama: `feature/hu-08-carga-programas`.

## Comportamiento

El Administrador analiza un lote de PDF del formato FISEI y obtiene un resultado por
archivo, sin guardar nada. Revisa y corrige cada borrador antes de confirmar.
La confirmación guarda asignatura, unidades curriculares, temáticas, contenidos y
PDF original en una transacción. El PDF conserva SHA-256, fecha y usuario y se
recupera mediante una ruta autenticada. Los códigos existentes devuelven 409 y no
se sobrescriben.

Incluye migración de Catálogo, tres rutas del Gateway, pantalla de carga y ajuste
del límite de nginx. Límites: 20 archivos, 5 MB por PDF y 25 MB por petición.
No incluye OCR, programas externos ni comparación de asignaturas.

## Correcciones de la revisión

- Confirmar un archivo no confirma otro que tenga el mismo nombre.
- Las unidades nulas en el JSON se rechazan con error de validación, sin guardado.
- La revisión permite añadir/eliminar unidades y temáticas y corregir los números
  de unidad, para completar o separar bloques que el extractor no haya reconocido.
  Los campos académicos nuevos quedan vacíos hasta que el Administrador los complete.

## Contrato y pendientes de aceptación

La evidencia registra que Edwin comunicó aprobación de Shirley del alcance sin agente
obligatorio. El Issue #50 todavía requiere reflejar ese acuerdo y las decisiones
aplicadas: borradores en pantalla, sin persistencia antes de confirmar, y conservación
del PDF confirmado. No cerrar automáticamente la historia.

Requiere revisión cruzada de Leslie (Catálogo) y Pablo (Gateway), aceptación de Shirley,
CI y prueba manual de la pantalla en navegador real. La extracción admite imperfecciones
de separación de bloques que deben corregirse durante la revisión humana.

## Validación

- Catálogo 126/126, Gateway 113/113 y frontend 129/129.
- TypeScript y compilación Vite aprobados; `git diff --check` sin errores.
- Smoke con SQL Server 2022 real: 17/17. Incluye permisos, duplicados,
  recuperación idéntica del PDF tras reinicio y rollback del guardado.
- Pruebas nuevas primero fallidas para los casos corregidos y después en verde.

Consultar `docs/sprint-2/evidencia-hu-08.md` para pruebas, entornos y límites.
No se incluyen PDF reales en el repositorio ni los cambios ajenos de puertos Docker,
SeleccionCatalogo.tsx, herramientas y temporales.
