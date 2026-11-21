# HU-02 — Gestión de usuarios y roles

**Estado:** propuesta de planificación para conversación del equipo; sin implementación ni aceptación de la Product Owner. Vinculada al [Issue #66](https://github.com/PabloToapanta/ProyectoHomologacion/issues/66). Las fechas y la distribución de puntos por tarea son propuestas, no avance registrado.

## Historia de usuario

| Campo | Definición |
| --- | --- |
| Número de historia | HU-02 |
| Nombre | Gestión de usuarios y roles |
| Usuario | Administrador |
| Descripción | Como administrador, quiero gestionar usuarios y roles (docentes y coordinadores), para habilitar y controlar su acceso al sistema según su función. |
| Puntos asignados | 5 PH, según el Product Backlog citado en el Issue #66 |
| Prioridad | Media, según el Issue #66 |
| Riesgo | Medio, propuesto: afecta identidad, permisos y compatibilidad con el inicio de sesión existente |
| Responsable | Pablo Toapanta |

## Contexto técnico y límites

Auth es dueño de `Usuario` y `Rol`, con roles `Administrador`, `Docente` y `Coordinador`. Ya dispone de nombre de usuario normalizado y único, estado `Activo`, hash de contraseña e inicio de sesión con JWT firmado y cifrado. El Gateway valida el token; los servicios aplican sus permisos. HU-29 depende de docentes activos, pero la administración completa de cuentas quedó fuera del Sprint 2. Se conservará el contrato de login y se evitará compartir la base de Auth con otros servicios.

Esta propuesta cubre crear y consultar cuentas, activar/desactivar, cambiar entre los roles Docente y Coordinador y establecer una contraseña inicial o nueva. El administrador define la contraseña y la entrega cara a cara al usuario, fuera del sistema. No se considera temporal ni se exige cambiarla en el primer inicio de sesión. No incluye autoservicio, recuperación por correo, eliminación física, edición de datos personales ni administración de cuentas Administrador.

## Criterios de aceptación propuestos

1. **CA1 — Alta y consulta.** **Dado** un administrador autenticado y un nombre de usuario disponible, **cuando** crea una cuenta con rol Docente o Coordinador y consulta el listado o detalle, **entonces** la cuenta aparece con su identificador, nombre, rol y estado, sin contraseña ni hash. **Dado** un nombre ya registrado, incluso con diferencias de mayúsculas o espacios externos, **cuando** intenta crearlo de nuevo, **entonces** recibe un error de duplicado y no se crea otra cuenta.
2. **CA2 — Activación.** **Dado** un administrador autenticado y una cuenta Docente o Coordinador existente, **cuando** la desactiva, **entonces** esa cuenta no puede iniciar sesión; **cuando** la reactiva, **entonces** puede volver a iniciar sesión con credenciales válidas. El administrador no puede desactivar su propia cuenta.
3. **CA3 — Cambio de rol.** **Dado** un administrador autenticado y una cuenta Docente o Coordinador, **cuando** cambia su rol a uno de esos dos valores, **entonces** la consulta muestra el nuevo rol y el siguiente inicio de sesión emite el rol actualizado. Los tokens emitidos antes conservan su validez hasta expirar, salvo que se acuerde revocación como alcance adicional.
4. **CA4 — Contraseña inicial o nueva.** **Dado** un administrador autenticado que crea una cuenta o reemplaza su contraseña, **cuando** envía la contraseña elegida por HTTPS, **entonces** Auth guarda únicamente su hash, devuelve una respuesta sin la contraseña ni el hash y permite iniciar sesión con la nueva contraseña. La contraseña no aparece en registros ni en el repositorio. El administrador la entrega cara a cara al usuario; no se exige cambio al primer inicio de sesión.
5. **CA5 — Autorización.** **Dada** una solicitud a la administración de usuarios, **cuando** no hay sesión válida, **entonces** responde 401; **cuando** la sesión es Docente o Coordinador, **entonces** responde 403; **cuando** es Administrador, **entonces** permite solo las operaciones anteriores y valida entradas, roles e identificadores con errores apropiados. Las rutas y respuestas quedan documentadas en OpenAPI.

## Tareas propuestas

Fechas previstas dentro del tramo final de Sprint 3 (5–25 de noviembre de 2026), sujetas a disponibilidad y acuerdo del equipo. La numeración T54/T55 conserva la del Issue #66; los puntos por tarea son una distribución nueva de los 5 PH de la historia, independiente de las horas indicadas allí.

| Número de tarea | Número de historia | Responsable | Fecha de inicio | Fecha de fin | Tipo | Puntos asignados | Nombre de la tarea y descripción |
| --- | --- | --- | --- | --- | --- | ---: | --- |
| T54 | HU-02 | Pablo Toapanta | 23/11/2026 | 24/11/2026 | Backend y pruebas | 3 PH | **Administrar cuentas en Auth.** Definir contrato HTTP y casos de uso; escribir primero pruebas fallidas para alta, consulta, estado, cambio de rol, contraseña inicial o nueva, unicidad y permisos; implementar persistencia, validaciones, OpenAPI y rutas de Gateway; comprobar regresión de login y consulta de docentes activos. Estimación previa del Issue: 5 h. |
| T55 | HU-02 | Pablo Toapanta | 24/11/2026 | 25/11/2026 | Frontend e integración | 2 PH | **Interfaz de usuarios y roles.** Crear listado y formularios administrativos, mostrar errores y estados, integrar por Gateway y verificar los criterios con pruebas de interfaz y recorrido integrado en Docker Compose; preparar evidencia para revisión cruzada de Edwin y aceptación de Shirley. Estimación previa del Issue: 3 h. |
| **Total** | **HU-02** |  |  |  |  | **5 PH** | **3 + 2 = 5 PH** |

## Rutas propuestas para Auth mediante Gateway

Se conserva la ruta existente de login. El Gateway enviará estas rutas a `auth-service`; el servicio comprobará además el rol Administrador en cada operación. Separar `/api/usuarios/docentes` de las rutas administrativas conserva la consulta de HU-29 y su permiso de Coordinador.

| Método | Ruta | Uso | Acceso |
| --- | --- | --- | --- |
| `GET` | `/api/usuarios` | Listar cuentas Docente y Coordinador con identificador, nombre, rol y estado | Administrador |
| `POST` | `/api/usuarios` | Crear cuenta Docente o Coordinador con contraseña inicial enviada por HTTPS | Administrador |
| `GET` | `/api/usuarios/{id}` | Consultar una cuenta | Administrador |
| `PATCH` | `/api/usuarios/{id}/estado` | Activar o desactivar una cuenta | Administrador |
| `PUT` | `/api/usuarios/{id}/rol` | Asignar rol Docente o Coordinador | Administrador |
| `PUT` | `/api/usuarios/{id}/contrasena` | Reemplazar la contraseña con una elegida por el administrador | Administrador |
| `GET` | `/api/usuarios/docentes` | Listar docentes activos (`id`, `nombreUsuario`) para elegir dos en HU-29 | Coordinador; contrato existente de HU-29 |

Las respuestas de administración nunca contienen contraseñas ni hashes. El Gateway debe declarar métodos y rutas concretos; Auth mantiene la autorización final. Evitar que la ruta variable `/api/usuarios/{id}` capture el segmento literal `docentes`.

## Acuerdos pendientes antes de implementar

- Confirmar con Shirley los criterios, la distribución T54/T55, el riesgo y las fechas. Pablo Toapanta es el único responsable de HU-02 y de ambas tareas, según la indicación actual del usuario y el Issue #66 actualizado.
- La entrega de la contraseña se realiza cara a cara por el administrador. Esta historia no implementa mensajería ni cambio obligatorio al primer acceso.
- HU-29 usa `GET /api/usuarios/docentes` para asignar dos docentes. HU-02 conservará sin cambios su respuesta (`id`, `nombreUsuario`), el filtro de cuentas activas con rol Docente y el acceso del Coordinador. Esto es una condición de compatibilidad ya definida, no una decisión pendiente. Si otra rama modifica a la vez la configuración del Gateway, Pablo resolverá el conflicto técnico al integrar las ramas sin cambiar este contrato. La revisión cruzada corresponde a Edwin; la aceptación funcional corresponde a Shirley.

**Definición de hecho aplicable:** PR hacia `develop` revisado, pruebas verificadas, Swagger actualizado, aceptación de la PO y recorrido integrado con Docker Compose. Esta documentación no acredita ninguno de esos hitos.
