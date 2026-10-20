# HU-05 — Gestión de asignaturas / T6, T7, T8 / Issue #3

Fecha local: 19/10/2026, America/Guayaquil. Leslie, con asistencia de Codex. **Implementación y pruebas locales; integración y aceptación pendientes.**

## Base verificada y autorización

Antes de editar se comprobó árbol de trabajo limpio, HU-04 local/remota en `fd0b325`, aprobación de Pablo del PR #19 y fusión de HU-04 en develop (`d6c2246d708a8e1a5fd097b899f4dc040f6612c2`). Se actualizó origin y se verificó que HU-04 era antecesora de develop. Se creó `feature/hu-05-asignaturas` desde `origin/develop`, sin copiar commits ni fusionar ramas por cuenta del agente.

Leslie autorizó continuar HU-05 después de la revisión de Pablo. Durante el desarrollo inicial no se hicieron commits, push ni merge de HU-05, ni se contactó externamente a compañeros. Posteriormente autorizó commits con Conventional Commits en español, push de la rama y solicitud de revisión a Pablo mediante PR hacia develop. La autorización no incluye merge ni aceptación funcional. El Issue #3 se consultó por GitHub y permanece abierto. Se leyeron AGENTS.md, criterios, planificación, contratos, contexto y código de HU-04.

## T6 — Backend y persistencia

- Se reutilizan los cuatro proyectos Catalogo.Domain/Application/Infrastructure/Api y la base del catálogo. Dominio puro, casos de uso/DTO/interfaces en Application, EF/repositorios en Infrastructure y HTTP/Swagger/DI en Api.
- Asignatura con UUID, carreraId, código, nombre, nivel, créditos y horas. Nivel/horas enteros positivos y créditos decimal positivo con hasta dos decimales, sin redondeo silencioso. Máximos técnicos por almacenamiento documentados; no se presentan como normas académicas.
- Se requiere carrera existente. Código único normalizado por carrera como decisión técnica; mismo código en otra carrera permitido. Índice único y FK respaldan las comprobaciones ante concurrencia.
- POST `/api/asignaturas`, GET `/api/asignaturas/{id}`, GET `/api/carreras/{id}/asignaturas`; orden por nivel/código. Swagger, JWT Administrador y ProblemDetails.
- Se sustituye el ancla interna de HU-04 por la entidad real sobre la misma tabla. La migración inicial publicada no cambia. La nueva añade campos/índice/CHECK sin recrear tabla ni cambiar IDs/FK.
- Si hay referencias antiguas sin datos académicos, la migración se detiene antes de cambiar el esquema con THROW 51000; no se rellenan ceros/datos inventados ni se borran filas. Se debe acordar tratamiento de datos antes de migrar ese entorno.
- Las pruebas de HU-04 que insertaban referencias mínimas ahora crean una asignatura completa y mantienen el criterio de eliminación restringida.

## T7 — Frontend existente

En `frontend/src/features/catalogo`: selección de carrera, lista por nivel, registro, validaciones, mensajes de error, reintento, carga y bloqueo durante guardado. Las peticiones anteriores se cancelan al cambiar de carrera y sus respuestas tardías se ignoran. `CatalogoRoute` aporta navegación interna Carreras/Asignaturas y usa la API pública `useAuth().request`.

Inicialmente no se modificó App.tsx ni el panel. Después Leslie autorizó expresamente resolver la integración compartida: se añadió `/admin/carreras` bajo ProtectedRoute Administrador y el enlace desde AdminPanel. CatalogoRoute incorpora retorno al panel y cierre de sesión con la API pública existente. El catálogo ahora forma parte del bundle de la aplicación. No se modificaron la lógica de autenticación, shared, dependencias, configuración común, Gateway ni homologación. Las pruebas de UI usan solicitudes simuladas, no un navegador con SQL Server real.

## T8 — Álgebra Lineal

Fixture `examples/hu-05/algebra-lineal.json`: Álgebra Lineal, nivel 1, 3 créditos, 144 horas. Código **DEMO-ALG-LIN-001 sintético**, sin fingir un código institucional. El script `cargar-algebra.ps1` requiere un UUID de Software existente y un JWT Administrador vía variable privada; no crea carreras ni carga datos automáticamente en el arranque.

Repetir la carga con los mismos valores no duplica. Si encuentra el código con otros valores se detiene sin sobrescribir. Se verificó el comportamiento del script con transporte HTTP simulado y los datos académicos en la prueba HTTP real de ASP.NET con SQLite. **No se ejecutó una carga contra SQL Server/Gateway**. Unidades, temas y resultados de aprendizaje quedan para HU-06/T15 de Edwin.

## Criterios y resultados reales

| Criterio del Issue #3 | Entrada/pasos sintéticos | Esperado | Resultado |
|---|---|---|---|
| Registrar con nivel/horas/créditos positivos y carrera | Crear Software sintética; POST Álgebra Lineal (1, 3, 144); consultar Location y lista | 201, UUID, datos en consultas | Correcto: dominio, servicio, HTTP y formulario |
| Rechazar horas/créditos <= 0 | Casos cero y negativos; también nivel no positivo, campos faltantes y tipos inválidos | 400, mensaje y sin persistir | Correcto: dominio/HTTP y validación del formulario |
| Listar por carrera ordenado por nivel | Registrar nivel 2 antes de nivel 1; dos códigos en nivel 1 y otra carrera | Lista aislada y ordenada por nivel/código | Correcto en casos de uso, HTTP y UI |
| Mantener HU-04 | Registrar asignatura y borrar su carrera; suite de carreras original | 409 y conservación de relación | Correcto en pruebas de regresión |

## Ejecuciones y TDD

| Ejecución | Resultado real |
|---|---|
| Red backend: `dotnet test ... --filter FullyQualifiedName~Asignatura` antes de implementar | **16 nuevas fallidas y 2 de HU-04 correctas** (el filtro incluye sus nombres); fallos por NotImplementedException. [TRX](evidencias/hu-05/backend-red.trx) |
| Red frontend: pruebas de página/cliente antes de implementar | **14 fallidas, 4 correctas** (las cuatro verificaban rechazo genérico; luego se reforzaron para comprobar mensajes específicos). [JSON](evidencias/hu-05/frontend-red.json) |
| `dotnet restore src/catalogo-service/Catalogo.sln` | Correcta, dependencias existentes actualizadas |
| Primera suite backend | 61 correctas; después se añadió comprobación del script SQL |
| Primera suite frontend y compilación | 77 correctas; TypeScript detectó un tipo demasiado amplio en un helper de prueba, corregido sin alterar configuración compartida |
| Pruebas de carga demo | Primer intento detectó un problema de alcance de variables en el doble HTTP de prueba; corregido usando estado compartido explícito |
| `dotnet build src/catalogo-service/Catalogo.sln --no-restore -c Release` | **Correcta, 0 advertencias y 0 errores** |
| `dotnet test src/catalogo-service/Catalogo.sln --no-restore --no-build -c Release` | **62 correctas, 0 fallidas, 0 omitidas**: 28 de HU-04 y 34 nuevas. [TRX](evidencias/hu-05/backend-green.trx) |
| `npm test` (suite completa, con navegación integrada) | **84 correctas, 0 fallidas**: 59 previas, 19 de HU-05 y 6 de navegación. [JSON](evidencias/hu-05/frontend-green.json) |
| `npm run build` | **TypeScript y Vite correctos** |
| `./tests/catalogo-service/hu05-demo.tests.ps1` | **6 correctas**, transporte HTTP simulado: primera carga, repetición, conflicto sin sobrescritura, carrera incorrecta, origen inválido y sesión requerida. [Salida](evidencias/hu-05/demo-tests.txt) |
| EF Core `migrations add AmpliarAsignaturasHu05` y `migrations script --idempotent` | Generadas migración, snapshot y script SQL; ninguna aplicada a base real |
| EF Core `migrations has-pending-model-changes --no-build` | Modelo y migración coinciden, sin cambios pendientes |
| `docker info` | Motor Linux no disponible (pipe dockerDesktopLinuxEngine inexistente); no se ejecutó Compose ni SQL Server |
| `git diff --check` | Sin errores tras retirar la línea vacía final del SQL generado |

Entorno: SDK .NET 8.0.130, EF Core 8.0.30, Node 24.21.0 y dependencias frontend fijadas por Pablo. No se agregaron paquetes ni se modificó el lockfile. .NET requirió ejecución autorizada fuera del entorno restringido para compilación y cachés.

## Límites y siguientes pasos

1. Revisar con Pablo [contratos e integración HU-05](../arquitectura/propuesta-integracion-hu-05.md), cambios compartidos ya autorizados e incorporados localmente, destino/red de Gateway, equivalencia de valores JWT y solución compartida. Las rutas HU-05 ya figuran en Gateway; evitar duplicarlas.
2. Arrancar SQL Server/Compose, aplicar ambas migraciones y comprobar registros, restricciones y concurrencia reales. Si hay referencias HU-04, acordar su tratamiento antes de migrar.
3. Ejecutar carga T8 real sobre una carrera Software de demostración y recorrido de navegador de escritorio/móvil. No se han verificado esos pasos.
4. Revisar los cambios de HU-05 con Pablo, ejecutar CI remoto tras publicación autorizada y solicitar aceptación a Shirley. Ninguna aceptación de HU-04 se extiende automáticamente a HU-05.
5. Entregar a Edwin el contrato estable de Asignatura para HU-06, sin implementar unidades/contenidos en esta historia.

El workflow del catálogo se amplió para ejecutar también las pruebas de carga demo en PowerShell. En la verificación local anterior a la publicación no se había ejecutado esa versión en GitHub. Los resultados remotos deben consultarse en el PR de HU-05. No se cierra el Issue #3 ni se declara cumplida la DoD.

## Integración de navegación autorizada posteriormente

Leslie solicitó resolver el montaje del catálogo y autorizó tomar las decisiones sobre esos archivos compartidos. Se trabajó en la misma rama y se conservaron los cambios locales de HU-05.

- Antes de implementar se añadieron seis pruebas con App/AuthProvider/ProtectedRoute reales, router en memoria y servicios HTTP simulados: cinco fallaron por ausencia del enlace/ruta; una ya pasaba al redirigir anónimos al login. [Red](evidencias/hu-05/navegacion-red.json).
- Tras montar la ruta, las seis verifican panel → carreras → asignaturas → registro con JWT → retorno, cierre de sesión, rechazo de Docente/Coordinador, acceso anónimo y cierre ante 401. No se sustituye ni simula el contexto de autenticación.
- Las 84 pruebas frontend pasaron. TypeScript detectó una opción `exact` no admitida en un selector de pruebas; se corrigió usando un nombre expresado con regex y se volvió a verificar la compilación.
- No se cambiaron servicios backend ni se ejecutaron de nuevo las pruebas .NET por este cambio exclusivamente de frontend. Sus 62 resultados anteriores permanecen vigentes para el código sin cambios.
