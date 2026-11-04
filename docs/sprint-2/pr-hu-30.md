# feat(solicitudes): consulta docente HU-30 e integración con JWT cifrado

Relacionada con #41 y #36. Preparación local para un PR hacia `develop`, recomendado como borrador: la consulta de asignaciones está implementada, pero CA3 y parte de CA4 dependen de resultados reales de HU-18/HU-20. No cerrar #41 por publicar este avance.

## Comportamiento

- El Docente inicia sesión y accede a «Mis solicitudes asignadas». La API obtiene su identidad del `sub` del token, sin aceptar un identificador de docente desde el cliente.
- Lista solo sus asignaciones y permite consultar el detalle. Solicitud inexistente o no asignada devuelve el mismo 404; otros roles reciben 403 y una sesión ausente o inválida, 401.
- Presenta análisis pendiente con resultado nulo. El estado completo con porcentajes, sugerencia, justificación y evidencias está preparado en la interfaz y probado con datos simulados; todavía no obtiene resultados persistidos reales.
- Es una vista de solo lectura; revisión y remisión corresponden a otras historias.
- Solicitudes se adapta al JWT firmado y cifrado de HU-35: exige clave de cifrado válida y rechaza tokens sin cifrar, manipulados o cifrados con otra clave. Sin este ajuste, los logins reales no podían consumir el servicio.
- Se declaran las rutas HTTPS del frontend para `/solicitudes` y `/docente/solicitudes`, conservando los permisos de las API. Dos pruebas reprodujeron el 401 al recargar antes de añadirlas.
- Se corrige una espera de la prueba de enrutamiento docente para verificar la petición después de que se ejecute el efecto asíncrono.

## Verificación

Verificación repetida el 03/11/2026: Solicitudes 105 aprobadas con SQL Server temporal y ninguna omitida; Gateway 105; frontend 120; TypeScript y compilación Vite correctos. Total: 330. Las pruebas de Solicitudes usan SDK .NET 8; la base y la red se eliminan al finalizar, sin usar las existentes. Dos pruebas reprodujeron el fallo HTTPS antes de corregirlo; la suite completa pasó después. Se corrigió también una aserción frontend que debía esperar el efecto asíncrono. Detalle: [evidencia-hu-30.md](evidencia-hu-30.md).

Estas pruebas no equivalen a una nueva aceptación funcional de la PO ni a la integración real con resultados del agente. La CI debe ejecutarse al publicar el PR.

## Alcance y pendientes

- Los commits de integración JWT y HU-30 están separados para facilitar la revisión. Coordinar los puntos compartidos de Auth/Gateway/React con Pablo y revisión cruzada de Leslie.
- Acordar el contrato definitivo de análisis con HU-18/HU-20 y verificar CA3 y los estados fallido/incompleto con resultados persistidos.
- Pendientes revisión y aceptación de Shirley con evidencias por criterio.
- Excluidos los ajustes locales de puertos Compose, el selector antiguo sin uso, los artefactos temporales y las herramientas de evaluación HU-18. Estas últimas se comprobaron en modo simulado; no se acredita evaluación de modelos reales.

Este archivo prepara la descripción; no acredita la existencia de un PR publicado.
