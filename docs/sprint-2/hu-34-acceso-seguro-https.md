# HU-34 — Acceso seguro mediante HTTPS

**Issue:** [#35 — HU-34](https://github.com/PabloToapanta/ProyectoHomologacion/issues/35)

**Sprint:** 2 · **Estado de esta ficha:** documentación previa a la implementación
**Fuente:** Issue #35 y [planificación detallada](planificacion-detallada.md). Los escenarios y pruebas siguientes describen lo que se comprobará; no acreditan código, pruebas ejecutadas ni aceptación.

## Historia de usuario

| Campo | Valor |
| --- | --- |
| Número | HU-34 |
| Nombre | Acceso seguro mediante HTTPS |
| Usuario | Usuario registrado: Administrador, Docente o Coordinador |
| Prioridad de negocio | Alta |
| Riesgo de desarrollo | Medio |
| Puntos estimados | 3 PH para la historia |
| Iteraciones asignadas | 2, según planificación |
| Programadores responsables | Pablo Toapanta y Leslie Coello; reparto individual pendiente de coordinación |

**Descripción.** Como usuario registrado, quiero que mis credenciales y mi sesión viajen cifradas entre el navegador y el sistema, para protegerlas durante su transmisión.

### Criterios de aceptación

**CA1 — Redirección del acceso público a HTTPS**

- **GIVEN** que un usuario abre la dirección pública del sistema mediante HTTP y aún no ha iniciado sesión,
- **WHEN** el navegador solicita el acceso público,
- **THEN** el Gateway responde con una redirección a la dirección HTTPS correspondiente antes de mostrar el formulario de inicio de sesión.

**CA2 — Credenciales y sesión por HTTPS**

- **GIVEN** que el usuario accede al formulario de inicio de sesión y dispone de una sesión válida,
- **WHEN** envía sus credenciales o realiza una petición autenticada desde el navegador,
- **THEN** esas peticiones usan HTTPS en el acceso público y no se envían mediante HTTP ni generan contenido mixto.

**CA3 — Cabecera de transporte seguro**

- **GIVEN** que el entorno previsto para comprobar la historia tiene HTTPS correctamente configurado,
- **WHEN** el usuario recibe una respuesta del Gateway por HTTPS,
- **THEN** la respuesta incluye la cabecera `Strict-Transport-Security` conforme a la configuración acordada para ese entorno.

**Decisiones previas a la implementación.** Acordar certificado, nombre o dirección pública, puertos, terminación TLS y proxy del frontend con el equipo. La verificación de CA3 se hará en el entorno acordado para HSTS; no se asumirá que una respuesta HTTP o un entorno de desarrollo sin TLS deba llevar esa cabecera. Coordinar la configuración con HU-35 y HU-36 sin alterar sus criterios.

## Tareas de la historia

Las 5 horas son estimaciones de la planificación. No se han registrado fechas reales de inicio o fin ni puntos por tarea.

| Campo | T16 | T17 |
| --- | --- | --- |
| Número de historia | HU-34 | HU-34 |
| Nombre de tarea | Configurar HTTPS en el Gateway | Usar HTTPS desde el frontend y comprobar el flujo |
| Tipo | Desarrollo | Desarrollo y pruebas |
| Estimación | 3 h; puntos individuales no asignados | 2 h; puntos individuales no asignados |
| Fecha de inicio | Por registrar al comenzar | Por registrar al comenzar |
| Fecha de fin | Por registrar al terminar | Por registrar al terminar |
| Programador responsable | Pablo/Leslie; reparto pendiente | Pablo/Leslie; reparto pendiente |
| Descripción | Preparar certificado de desarrollo, HTTPS y HSTS del Gateway, y variables necesarias en Docker Compose. | Hacer que el acceso del navegador use el Gateway por HTTPS, redirigir desde HTTP y añadir las comprobaciones correspondientes. |

## Mockup

**No aplica.** HU-34 cambia el transporte y la configuración del acceso público; no añade una pantalla. La evidencia visual prevista es el navegador conectado por HTTPS, junto con la redirección y las cabeceras observadas sin mostrar credenciales.

## Pruebas de la historia — plan, aún sin ejecutar

| ID y tipo | Criterio | Escenario y resultado esperado | Evidencia a registrar |
| --- | --- | --- | --- |
| PU1 · unitaria | CA1 | Con la configuración pública HTTPS acordada, la construcción del destino de redirección conserva ruta y consulta y usa esquema/host/puerto esperados; no produce un destino HTTP. Solo si esa lógica queda aislada como unidad al implementar. | Resultado de xUnit y configuración sintética usada. |
| PU2 · unitaria | CA3 | La configuración de HSTS del entorno previsto produce una política válida y no expone valores de certificados o secretos. Solo si se introduce configuración propia comprobable como unidad. | Resultado de xUnit y valores no sensibles. |
| PI1 · integración | CA1 | Un cliente HTTP real, sin seguir redirecciones, recibe 307 o 308 y una cabecera `Location` HTTPS correcta al acceder a la ruta pública antes del login. | Comando, código, `Location`, entorno y fecha. |
| PI2 · integración | CA2 | Frontend, Gateway y Auth ejecutados con TLS: el login y una llamada autenticada llegan por HTTPS; el navegador no registra peticiones HTTP con contraseña/token ni contenido mixto. | Traza de red saneada y resultado E2E; nunca publicar contraseña ni token. |
| PI3 · integración | CA3 | Una respuesta HTTPS del Gateway contiene `Strict-Transport-Security` en el entorno acordado. La prueba valida el certificado; no desactiva la verificación TLS para darla por aprobada. | Cabecera saneada, cadena de confianza del certificado y resultado. |
| PA1 · aceptación | CA1–CA2 | Shirley abre la URL HTTP pública, observa el salto a HTTPS antes del formulario, inicia sesión con una cuenta de prueba y navega una ruta protegida sin aviso de contenido mixto. | Pasos, resultado real, captura sin datos sensibles, fecha y decisión de la PO. |
| PA2 · aceptación | CA3 | Shirley comprueba en el entorno acordado una respuesta HTTPS del Gateway con HSTS y confirma que la sesión sigue funcionando para el rol de prueba. | Cabecera, entorno, fecha y decisión de la PO. |

**Cobertura:** CA1 → PU1/PI1/PA1; CA2 → PI2/PA1; CA3 → PU2/PI3/PA2. Las pruebas unitarias PU1/PU2 son condicionales a que exista lógica o configuración propia aislable; la cobertura obligatoria del comportamiento HTTP es PI1–PI3 y la aceptación PA1–PA2. Se registrarán fallo inicial, corrección y ejecución final cuando corresponda aplicar TDD.

**Registro de resultados:** pendiente. Para cada prueba ejecutada se anotarán entrada o pasos, esperado, obtenido, fecha, responsable y enlace a salida/captura. La aceptación funcional corresponde a Shirley después de ejecutar los escenarios de aceptación; este documento no la anticipa.
