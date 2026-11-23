# HU-07 — Versiones de malla por carrera

Issue [#68](https://github.com/PabloToapanta/ProyectoHomologacion/issues/68) · T58 y T59 · Responsable: Leslie · Revisión cruzada: Edwin · Aceptación: Shirley.

## Estado y base de trabajo

Propuesta técnica preparada desde `origin/develop` en `843bf4f`. Rama local `feature/hu-07-versiones-malla`. Leslie confirmó en este chat que Shirley aprobó los CA1–CA4 publicados; el Issue todavía los rotula como propuestos. Se implementaron Domain, Application, Infrastructure, endpoints y panel T59 en el catálogo. Pablo confirmó el contrato de trazabilidad y se implementó la instantánea en análisis, agrupaciones e informe, junto con su consumo en HU-22; ver la evidencia CA3. La revisión cruzada y la aceptación funcional siguen pendientes. Este documento no acredita aceptación funcional ni horas ejecutadas.

Leslie transmitió el acuerdo técnico de Pablo sobre modelo, migración, HTTP, compatibilidad, trazabilidad y pruebas, y autorizó continuar Application, Infrastructure y T59. Se conectaron DI en Program.cs y las tres rutas de Gateway necesarias para el catálogo. No hizo falta editar App.tsx, navegación ni permisos comunes de React: el panel se monta dentro de AsignaturasPage. Se preservan las reglas del motor; los cambios de metadata en análisis/informe/HU-22 requieren revisión conjunta previa a integrar. No se enviaron comunicaciones externas.

La base inicial de develop tenía `Carrera` y `Asignatura.CarreraId`, sin entidad Malla. La unicidad del código es por carrera. Unidades, contenidos, perfiles, revisiones y PDF dependen del identificador de asignatura. `Carrera.Vigencia` no representa la vigencia de una versión de malla. La leyenda de `AsignaturasPage.tsx` mezclaba ambos conceptos; se corrigió en T59.

## Criterios confirmados por Leslie en nombre del acuerdo con la PO

Se reproducen en forma resumida los CA1–CA4 del Issue #68:

1. El Administrador registra versiones por carrera y marca la vigente; admite varias no vigentes.
2. Cada asignatura pertenece a una versión; las existentes quedan en la vigente después de migrar.
3. El análisis identifica las versiones de las asignaturas comparadas.
4. Solo Administrador modifica versiones: 401 sin sesión y 403 para otros roles.

Propuesta técnica para concretarlos: como máximo una versión vigente por carrera; activar otra desactiva la anterior en una sola transacción. La versión inicial de la migración se crea vigente. No se traslada ni copia automáticamente ninguna asignatura al cambiar la vigente. El código de asignatura será único dentro de cada versión; versiones diferentes podrán conservar códigos iguales con identificadores diferentes. Estas precisiones están implementadas y se incluyen en la revisión del contrato antes de la integración. Las carreras nuevas reciben una malla técnica Inicial dentro de la misma transacción de creación; la migración usa Inicial migrada.

## Modelo y reglas implementadas en Catálogo

- `Malla`: `Id` UUID, `CarreraId` UUID, `Nombre` obligatorio normalizado (máximo técnico 100 caracteres), `NombreNormalizado`, `Vigente` booleano.
- Nombre único por carrera después de normalizar espacios, Unicode y mayúsculas. Carrera obligatoria y existente. Se preserva la restricción FISEI del catálogo receptor y la lectura de registros históricos de otras facultades.
- `Asignatura.MallaId` obligatorio. La malla y la asignatura deben pertenecer a la misma carrera, mediante validación de Application y restricción referencial de Infrastructure.
- Índice único filtrado por carrera cuando `Vigente = 1`; transición transaccional con concurrencia controlada. Dos activaciones simultáneas nunca dejan dos versiones vigentes ni un cambio parcial.
- No se incluye borrado de mallas, clonación, traslado de asignaturas existentes ni modificación de informes históricos. No se cambian umbrales, fórmulas, antigüedad ni decisiones docentes.

## Migración sin pérdida de datos

Antes de actualizar una base persistente: el responsable de esa base debe obtener un respaldo SQL Server y comprobar su restauración en una instancia aislada. Este trabajo no ha leído `.env`, respaldado ni migrado bases persistentes.

Una migración nueva, posterior a las existentes, deberá:

1. Crear `Mallas` y sus índices; no modificar migraciones históricas.
2. Crear una versión `Inicial migrada` vigente para cada carrera existente, incluidas las históricas; es una etiqueta técnica de migración, no un año o plan académico inventado.
3. Agregar `MallaId` inicialmente nullable y asociar cada asignatura a la versión inicial de su propia carrera, sin cambiar `Asignatura.Id`, `CarreraId` ni datos académicos.
4. Verificar que no queden asociaciones nulas o inconsistentes y hacer la columna obligatoria. Sustituir la unicidad `(CarreraId, CodigoNormalizado)` por la unicidad `(MallaId, CodigoNormalizado)`.
5. Preservar todas las filas y claves de unidades, contenidos, perfiles, revisiones y programas PDF. No borrar ni reasignar carreras históricas de otras facultades.

Ensayar tanto base vacía como actualización desde las migraciones de develop con datos sintéticos representativos; comparar claves, cantidades, valores y hashes de PDF antes/después. Probar fallo transaccional y concurrencia con SQL Server aislado. No usar la migración inversa para descartar versiones nuevas: si el esquema antiguo no puede representar los datos nuevos, rechazar la reversión claramente y recurrir a un plan de restauración revisado.

## Contratos HTTP implementados para revisión, todavía no publicados

| Método | Ruta | Rol y resultado |
| --- | --- | --- |
| GET | `/api/carreras/{carreraId}/mallas` | Administrador: versiones y vigencia; 404 carrera inexistente |
| POST | `/api/carreras/{carreraId}/mallas` | Administrador: `{nombre, vigente}`; 201 con Location; 400 validación, 409 duplicado/concurrencia |
| GET | `/api/mallas/{mallaId}` | Administrador: detalle usado por Location; 404 inexistente |
| PUT | `/api/mallas/{mallaId}/vigencia` | Administrador: `{vigente:true}` activa atómicamente la seleccionada; 404 inexistente, 409 conflicto |

La lista y los DTO de asignaturas añaden metadatos de malla de forma compatible: filtro opcional `mallaId` en el listado existente y `mallaId` opcional en creación, resuelto por el servidor a la vigente cuando no se envíe. El listado sin filtro conservará todas las asignaturas y su orden por nivel/código; no ocultará silenciosamente datos históricos. Crear sin indicar malla cuando no existe una vigente devolverá conflicto con explicación.

HU-08 usa la misma resolución de malla para evitar inserciones sin relación; no se cambiará la extracción de PDF ni se inventarán versiones por su contenido. Las lecturas académicas para Docente/Coordinador se propondrán mediante contratos específicos; no se abrirá el CRUD administrativo. Las rutas están documentadas en OpenAPI; se conservan JWT y clave interna. El Gateway usa la política existente UserAdministration, y el catálogo exige de nuevo Administrador. Las lecturas internas de contexto mantienen sus permisos existentes.

## Integración y archivos a coordinar

| Área | Trabajo de Leslie | Coordinación necesaria antes de editar archivos compartidos |
| --- | --- | --- |
| Domain/Application/Infrastructure del Catálogo | Malla, casos de uso, repositorio, migración; asignaturas y carga HU-08 resuelven malla | Pablo revisa compatibilidad con consumidores y HU-08 |
| UI `frontend/src/features/catalogo` | Panel de versiones, selección de malla y leyendas correctas; reutilizar estilos/componentes | Pablo: montaje, `App.tsx`, navegación y permisos comunes |
| Arranque y Gateway | DI y rutas nuevas con OpenAPI | Pablo: `Program.cs` y configuración de Gateway |
| HU-18/19/21 e informe | Metadatos aditivos en contexto e instantánea de cada análisis nuevo | Pablo: motor/orquestación; Edwin: informe y HU-22; no reescribir resultados históricos |
| HU-22 | Ofrecer id, nombre y vigencia de la malla de origen | Edwin: consumir la malla real; no sustituirla por `Carrera.Vigencia` |

La versión debe quedar guardada con cada análisis nuevo para que activar otra malla no cambie su trazabilidad. Los análisis anteriores que no tengan ese dato lo indicarán como no registrado; no se les atribuirá retroactivamente una versión académica. El mecanismo se acordó en la propuesta de integración: propiedad mallas nullable, con orígenes/receptoras y vigenteAlAnalizar; está implementado con pruebas de persistencia y compatibilidad histórica.

## Pruebas previstas con TDD

| Criterio | Prueba antes de implementar | Integración |
| --- | --- | --- |
| CA1 | Nombre vacío/duplicado; versiones por carrera; activación desactiva anterior | Dos contextos SQL activan simultáneamente: una sola vigente |
| CA2 | Malla ajena rechazada; código repetido en misma versión; permitido en otra | Migración vacía y desde develop; comparación íntegra de filas, relaciones y PDF |
| CA3 | DTO/contexto con versión correcta; instantánea no cambia al activar otra | Análisis nuevo identifica versiones; histórico permanece intacto |
| CA4 | API 401/403 y Administrador autorizado | Gateway real conserva restricciones; acceso directo exige clave interna |
| T59 | Crear/listar/activar, filtros, errores y estados de carga | Navegador con datos sintéticos y catálogo histórico visible |

## Resultado real de este primer incremento

- Restauración NuGet: correcta tras solicitar ejecución fuera del sandbox. La primera ejecución dentro del sandbox se interrumpió sin finalizar; un intento sin restaurar mostró assets desactualizados y no pudo resolver `UglyToad`. Tras restaurar no fue necesario cambiar dependencias ni código de extracción PDF.
- TDD: `MallaTests` falló primero en compilación por ausencia de `Malla` (`CS0246`); después se implementó la entidad. Un primer intento encontró un problema en el fixture de Unicode: un surrogate aislado dentro de `InlineData` no se conserva en los metadatos del atributo. Se construyó la misma entrada inválida en ejecución y se mantuvo la aserción de rechazo.
- `dotnet test tests/Catalogo.Tests/Catalogo.Tests.csproj --no-restore --nologo --logger "trx;LogFileName=catalogo-hu07.trx" --results-directory TestResults/hu07-final`: **135 correctas, 0 fallidas, 0 omitidas**, incluidas las **9** nuevas de Malla. Compiló Domain, Application, Infrastructure, Api y pruebas para .NET 8.
- `dotnet format whitespace tests/Catalogo.Tests/Catalogo.Tests.csproj --no-restore --verify-no-changes --include src/catalogo-service/Catalogo.Domain/Malla.cs tests/Catalogo.Tests/MallaTests.cs`: correcto, salida 0. Revisión de los cuatro archivos nuevos sin cambios a archivos existentes.
- La entidad comprueba carrera no vacía, nombre requerido, normalización Unicode/caso/espacios, límite técnico, caracteres inválidos y conservación de identidad al cambiar su indicador de vigencia. Todavía no comprueba unicidad entre versiones ni concurrencia en base: corresponden al repositorio pendiente.
- No se ha migrado ni accedido a una base persistente. No se ejecutó SQL Server ni navegador para HU-07. El frontend no cambió; no se atribuyen a esta entrega pruebas de UI ni chequeo de tipos.

El primer incremento anterior queda como evidencia histórica. Para el incremento actual, consultar [evidencia T58/T59](evidencia-hu-07-t58-t59.md). Actualización: el contrato de instantáneas fue aprobado e implementado. Ver [evidencia CA3](evidencia-hu-07-ca3.md) para los resultados posteriores a incorporar develop 1542986. Pendientes revisión cruzada del incremento, respaldo/restauración antes de una base persistente y aceptación de Shirley. No se declara HU-07 aceptada.
