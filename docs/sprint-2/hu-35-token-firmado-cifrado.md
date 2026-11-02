# HU-35 — Token de sesión firmado y cifrado

**Issue:** [#36 — HU-35](https://github.com/PabloToapanta/ProyectoHomologacion/issues/36)

**Sprint:** 2 · **Estado de esta ficha:** planificación previa a la implementación

**Fuentes del producto:** Issue #36 y [planificación detallada](planificacion-detallada.md). Los escenarios siguientes expresan resultados esperados; no acreditan código, pruebas ejecutadas ni aceptación.

## Historia de usuario

| Campo | Valor |
| --- | --- |
| Número | HU-35 |
| Nombre | Token de sesión firmado y cifrado |
| Usuario | Usuario registrado: Administrador, Docente o Coordinador |
| Prioridad de negocio | Alta |
| Riesgo de desarrollo | Alto |
| Puntos estimados | 5 PH para la historia |
| Iteraciones asignadas | 2, según planificación |
| Programadores responsables | Pablo Toapanta y Leslie Coello; reparto individual por coordinar |

**Descripción.** Como usuario registrado, quiero que mi token de sesión esté firmado y cifrado, para impedir que su contenido se lea o se modifique sin las claves autorizadas.

### Criterios de aceptación

**CA1 — Claims no legibles sin la clave de cifrado**

- **GIVEN** que un usuario válido inicia sesión y Auth emite su token,
- **WHEN** alguien intenta decodificarlo sin la clave de cifrado,
- **THEN** no puede obtener del token los claims de identidad, nombre o rol. El servicio autorizado sí puede descifrarlo y comprobar su firma.

**CA2 — Rechazo de tokens no válidos**

- **GIVEN** una ruta protegida del Gateway o de un servicio académico,
- **WHEN** recibe un token alterado, un JWT solo firmado y sin cifrar, o un token cifrado con otra clave,
- **THEN** responde HTTP 401 y no concede acceso ni revela el contenido del token. Una firma inválida, una caducidad vencida, un emisor o una audiencia incorrectos también se rechazan con 401.

**CA3 — Clave de cifrado obligatoria**

- **GIVEN** que Auth o un validador de tokens arranca con la configuración de seguridad,
- **WHEN** falta la clave de cifrado o no tiene la longitud requerida por el algoritmo acordado,
- **THEN** el componente no inicia correctamente y el error identifica la configuración inválida sin mostrar el valor de la clave.

**CA4 — Contrato de login y continuidad de roles**

- **GIVEN** credenciales válidas de Administrador, Docente o Coordinador,
- **WHEN** el usuario inicia sesión y navega a una ruta protegida,
- **THEN** el login devuelve token, nombre, rol y expiración; la interfaz usa esos campos sin decodificar el token; el Gateway y el servicio autorizan según el rol existente. Un token válido con rol insuficiente conserva HTTP 403.

### Contrato técnico propuesto para T18–T20

- Emitir un **JWS firmado dentro de un JWE cifrado**. Mantener la firma HS256 actual y evaluar `A256KW` + `A256GCM` para la envoltura, con una clave de cifrado aleatoria e independiente de la clave de firma. La selección se confirmará con una prueba de compatibilidad de la versión `System.IdentityModel.Tokens.Jwt` usada por el repositorio antes de fijar el código. Las API de IdentityModel documentan la creación de JWE a partir de credenciales de firma y cifrado y su validación con clave de descifrado ([creación](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.jsonwebtokens.jsonwebtokenhandler.createtoken), [validación](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters)).
- Auth emite; Gateway, Catálogo y Homologación descifran y validan firma, emisor, audiencia y vigencia antes de aplicar las políticas de rol. Rechazar explícitamente el formato anterior solo firmado, aunque tenga firma válida. YARP puede transportar el Bearer al destino después de la validación del Gateway; cada servicio conserva su propia validación.
- Añadir la clave de cifrado a la configuración privada de cada componente que valida el token. Validar presencia y longitud al arrancar; no colocar claves en Git, React, variables `VITE_`, mensajes de error ni logs. Mantener separadas las claves de firma y cifrado. La distribución y rotación de secretos se acordarán antes del despliegue fuera del entorno local.
- Ampliar `POST /api/auth/login` para devolver `accessToken`, `tokenType`, `name`, `role` y `expiresAtUtc`. El nombre de usuario y el rol proceden de la cuenta autenticada. El frontend mantiene el token solo en memoria y usa los metadatos de la respuesta; deja de separar o decodificar segmentos JWT. Actualizar OpenAPI y los tests de contrato.
- HU-34 protege el transporte público por HTTPS. HU-36 trata la clave de aplicación entre Gateway y servicios; ambos alcances siguen separados de esta historia.

La selección de algoritmos y el formato de la clave son **propuesta de implementación**, no una norma institucional ni una decisión ya verificada. T18 debe demostrar que la biblioteca y el stack actual admiten la combinación elegida antes de continuar con los demás componentes.

## Tareas de la historia

Las 12 horas son estimaciones de la planificación. No se han registrado fechas reales ni puntos por tarea.

| Campo | T18 | T19 | T20 | T21 |
| --- | --- | --- | --- | --- |
| Número de historia | HU-35 | HU-35 | HU-35 | HU-35 |
| Nombre de tarea | Emitir token firmado y cifrado | Validar token en Gateway y servicios | Adaptar login y frontend | Comprobar rechazos de seguridad |
| Tipo | Desarrollo | Desarrollo | Desarrollo | Pruebas |
| Estimación | 4 h | 3 h | 3 h | 2 h |
| Fecha de inicio | Por registrar | Por registrar | Por registrar | Por registrar |
| Fecha de fin | Por registrar | Por registrar | Por registrar | Por registrar |
| Programador responsable | Pablo/Leslie; reparto pendiente | Pablo/Leslie; reparto pendiente | Pablo/Leslie; reparto pendiente | Pablo/Leslie; reparto pendiente |
| Descripción | Auth firma y cifra el token; valida la clave de cifrado al arrancar. | Gateway, Catálogo y Homologación descifran, validan firma/claims y rechazan tokens no cifrados. | Login entrega nombre, rol y expiración; React deja de leer el JWT y se actualizan contratos y pruebas. | Cubrir manipulación, formato anterior, otra clave, firma inválida y configuración incompleta. |

## Mockup

**No aplica.** HU-35 cambia el token y el contrato de la respuesta de login, pero no añade una pantalla ni altera el flujo visual acordado. La interfaz existente debe seguir mostrando el nombre y el rol con los metadatos recibidos, sin inspeccionar el token.

## Pruebas de la historia — plan, aún sin ejecutar

| ID y tipo | Criterio | Escenario y resultado esperado | Evidencia a registrar |
| --- | --- | --- | --- |
| PU1 · unitaria | CA3 | Configuración sin clave de cifrado o con longitud inválida falla al validar; el mensaje no contiene el secreto. La clave de firma se comprueba por separado. | Pruebas xUnit, entrada sintética, error saneado. |
| PU2 · unitaria | CA4 | El contrato de sesión del frontend acepta nombre, rol y expiración válidos y rechaza metadatos ausentes, rol desconocido o expiración inválida sin decodificar el token. | Pruebas del frontend y casos de datos sintéticos. |
| PI1 · integración | CA1 | Auth inicia sesión con credenciales de prueba y emite un JWE; una herramienta sin la clave no obtiene los claims. El validador con ambas claves recupera los claims y verifica la firma. No basta contar segmentos. | Resultado de Auth, validación criptográfica y formato saneado; no publicar JWT completo. |
| PI2 · integración | CA2 | Gateway y cada servicio académico devuelven 401 ante token alterado, solo firmado, cifrado con otra clave, firma inválida, vencido o con emisor/audiencia incorrectos. No se ejecuta la operación protegida. | Matriz de casos y códigos HTTP por componente. |
| PI3 · integración | CA3 | Arranque con clave ausente o corta falla en Auth y validadores; no aparecen valores secretos en la salida. Arranque con configuración válida continúa. | Comandos, estados de proceso y extracto saneado de logs. |
| PI4 · integración | CA4 | Login real devuelve `accessToken`, `tokenType`, `name`, `role`, `expiresAtUtc`; una ruta permitida funciona para cada uno de los tres roles, y una ruta prohibida responde 403. React navega y caduca la sesión usando la respuesta, sin decodificar JWT. | Contrato HTTP, matriz de roles, pruebas de frontend y recorrido Docker Compose. |
| PA1 · aceptación | CA1–CA2 | Shirley inicia sesión en el entorno integrado, comprueba que el contenido del token no se lee sin clave y que los tokens manipulados/no cifrados/con otra clave no acceden a una ruta protegida. | Pasos, resultado real, fecha y decisión de la PO; token y claves ocultos. |
| PA2 · aceptación | CA3–CA4 | Shirley comprueba un arranque con clave inválida sin fuga de secreto y el login/navegación de Administrador, Docente y Coordinador; la interfaz muestra nombre y rol, y respeta la expiración recibida. | Matriz de roles, evidencia saneada, fecha y decisión de la PO. |

**Cobertura:** CA1 → PI1/PA1; CA2 → PI2/PA1; CA3 → PU1/PI3/PA2; CA4 → PU2/PI4/PA2. En T18–T21 se aplicará TDD a las reglas y validaciones: prueba fallida → implementación → refactorización. La prueba de aceptación queda documentada para ejecutarse después de la integración, con Shirley.

**Registro de resultados:** pendiente. Para cada prueba ejecutada se anotarán entrada o pasos, esperado, obtenido, fecha, responsable y enlace a salida/captura. Esta ficha no declara HU-35 terminada ni aceptada.
