# HU-04 — Gestión de carreras, T4/T5, Issue #2

Fecha: 19/10/2026 (America/Guayaquil). Responsable del trabajo: Leslie, con asistencia de Codex. Estado: **implementación local y pruebas automatizadas verificadas; integración y aceptación pendientes**. No declarar la HU terminada según la DoD.

## Inspección y alcance

Se revisaron `AGENTS.md`, contexto, planificación detallada, criterios, decisiones/contratos y arranque de tres desarrolladores. El checkout inicial era `main` con solo README. Se actualizó `origin` y se creó `feature/hu-04-carreras` desde `origin/develop`, que contenía la documentación y frontend HU-01. Durante la implementación inicial no se hicieron commits, pushes ni merges. Posteriormente Leslie autorizó commits atómicos con Conventional Commits en español y el push de esta rama; la autorización no incluye merges ni aceptación funcional.

Se intentó consultar el Issue #2 por página GitHub y API pública. La página no estuvo disponible y la API falló primero por DNS en entorno restringido y luego por transporte EOF fuera de él. **No se pudo leer el Issue**. Se aplicaron los criterios locales y la solicitud de Leslie, sin bloquear la implementación.

T4: cuatro proyectos .NET independientes, SQL Server, EF Core, migración inicial, índice único, FK sin cascada, CRUD, errores ProblemDetails, JWT Administrador, Swagger y pruebas. T5: módulo en el frontend existente, alta/edición/listado/eliminación confirmada, vigencia, modalidad opcional, validaciones, carga, reintentos y mensajes de error.

No se modificaron auth-service, homologacion-service, Gateway, App.tsx, configuración común, dependencias React ni Homologacion.sln. Se agregó una solución local del catálogo y un workflow propio; **GitHub Actions aún no se ha ejecutado**. La tabla Asignaturas es únicamente el ancla relacional necesaria para comprobar la eliminación restringida; no incluye la historia HU-05 ni las entidades de HU-06.

## Criterios comprobados automáticamente

| Criterio | Entrada/pasos sintéticos | Esperado | Resultado real y evidencia |
|---|---|---|---|
| Crear nombre/facultad/vigencia y listar | POST carrera de prueba con vigencia true; GET lista y Location | 201, UUID, DTO en listado | Correcto en `CRUD_HTTP_y_location`, pruebas de servicio y UI |
| Duplicado dentro de facultad | Nombre con caso/espacios diferentes, misma facultad; crear/editar | 409, mensaje claro, sin sobrescribir la carrera existente | Correcto en API, servicio y formulario; mismo nombre en otra facultad permitido |
| Eliminar sin asignaturas | DELETE carrera recién creada | 204, ya no aparece; consulta posterior 404 | Correcto en CRUD HTTP, servicio y UI |
| Impedir eliminar con asignaturas | Insertar referencia sintética directamente en SQLite; DELETE | 409 y conservar carrera | Correcto en API/servicio; prueba relacional verifica además que la FK impide borrado directo |
| Validaciones | Nombre/facultad vacíos, nulos o largos; vigencia ausente/nula/tipo incorrecto; modalidad larga | 400/validación, no persistir | Correcto en dominio, servicio, HTTP y UI |

Las referencias académicas son sintéticas; no se cargaron programas analíticos ni datos institucionales inventados. En la UI las solicitudes están simuladas; en HTTP se usa WebApplicationFactory con SQLite relacional. **Esto no certifica SQL Server real ni el recorrido por Gateway.**

## TDD y ejecuciones reales

| Paso | Comando/actividad | Resultado |
|---|---|---|
| Restauración inicial restringida | `dotnet test tests/Catalogo.Tests/Catalogo.Tests.csproj` | Bloqueada por DNS NuGet; se reintentó con permiso fuera del entorno restringido |
| Red backend | Pruebas de dominio/aplicación contra métodos todavía sin implementar | **15 fallidas / 0 correctas**, `NotImplementedException`; [TRX](evidencias/hu-04/tdd-domain-red.trx) |
| Green dominio/aplicación | `dotnet test tests/Catalogo.Tests/Catalogo.Tests.csproj --no-restore` | **15 correctas**; [TRX](evidencias/hu-04/domain-application-green.trx) |
| Instalación frontend | `npm ci` con Node 24.21.0 instalado | Primer intento restringido EPERM; reintento autorizado: 111 paquetes instalados; package.json/lockfile intactos |
| Red frontend | `npm test -- src/features/catalogo` con cliente/página todavía sin implementar | **13 fallidas / 0 correctas**; [JSON](evidencias/hu-04/frontend-red.json) |
| Pruebas HTTP iniciales | Se agregan casos HTTP, seguridad, Swagger y restricciones relacionales | **22 correctas / 6 fallidas**: contenido de validación `application/json`; se retiró `Produces` global para permitir `application/problem+json`; [TRX](evidencias/hu-04/http-first-run.trx) |
| Restauración final | `dotnet restore tests/Catalogo.Tests/Catalogo.Tests.csproj` (incluye las cuatro capas) | Correcta |
| Green backend | `dotnet test src/catalogo-service/Catalogo.sln --no-restore` | **28 correctas / 0 fallidas / 0 omitidas**; [TRX](evidencias/hu-04/catalogo-green.trx) |
| Compilación backend | `dotnet build src/catalogo-service/Catalogo.sln --no-restore -c Release` | Correcta, **0 advertencias y 0 errores** |
| Green frontend | `npm test` (suite completa) | **59 correctas / 0 fallidas**: 16 de catálogo + 43 existentes; [JSON](evidencias/hu-04/frontend-green.json) |
| Compilación frontend | `npm run build` | TypeScript correcto; Vite 8.3.4 compiló sin errores |
| Migración | EF Core 8.0.30: `migrations add InitialCatalogo`, `migrations script --idempotent` | Generadas migración, snapshot y `src/catalogo-service/Migrations.sql`; no aplicadas a una base real |
| Modelo/migración | `dotnet ef migrations has-pending-model-changes --project Catalogo.Infrastructure --no-build` | Sin cambios de modelo pendientes |
| Configuración Compose | `docker compose ... config --quiet` con variables sintéticas temporales | Configuración válida; no arranca contenedores ni contacta SQL Server |
| Docker | `docker info` | Motor Linux no disponible: pipe `dockerDesktopLinuxEngine` inexistente; no se ejecutó `compose up` |
| Revisión de cambios | `git diff --check` | Sin errores de whitespace en cambios rastreados |

Las ejecuciones .NET necesitaron permisos fuera del entorno restringido para cachés/restauración/compilación. SDK disponible: 8.0.130. No se instalaron versiones globales de Node ni se modificó su configuración; se utilizó el ejecutable existente de Node 24.21.0. EF se fijó mediante manifiesto local del servicio.

## Cobertura y límites

- Dominio/aplicación: CRUD, validación, normalización Unicode/espacios/caso, exclusión del propio ID al editar, duplicado en creación/edición, otra facultad, vigencia false, no encontrado y bloqueo por asignaturas.
- HTTP/persistencia: status codes, Location, binding JSON, ProblemDetails, FK e índice únicos usando SQLite, 401, 403, JWT vencido, Swagger y referencias entre capas.
- Frontend: listado, alta, edición y vigencia, cancelar edición/eliminación, eliminar permitido/restringido, duplicado con conservación de datos, validación de espacios, carga, reintento, bloqueo mientras guarda y cliente de API.
- El índice único y FK de SQL Server están generados; las traducciones de errores SQL 2601/2627/547 y carreras de concurrencia reales **no se han ejecutado contra SQL Server**.
- El build de Vite compila la aplicación base; TypeScript comprueba también el módulo de catálogo. El bundle principal no contiene todavía la pantalla porque su ruta está pendiente de incorporación por Pablo.
- No se ejecutaron pruebas de navegador ni inspección visual de escritorio/móvil para catálogo. El recorrido integrado y accesibilidad visual requieren la ruta acordada.

## Pendientes de integración y aceptación

1. Pablo revisa [propuesta de integración](../arquitectura/propuesta-integracion-hu-04.md): UUID/JSON, vigencia, ruta `/admin/carreras`, menú, rol Administrador, puerto 5082 y cluster Gateway. No se presume acuerdo ni contacto externo realizado.
2. Arrancar Docker/SQL Server, aplicar migración, ejecutar CRUD/duplicados/FK/concurrencia reales; comprobar el recorrido autenticado por Gateway y navegador.
3. Incorporar proyectos a Homologacion.sln y ejecutar CI con revisión compartida. El catálogo ya puede compilarse y probarse con Catalogo.sln.
4. Revisión cruzada de Pablo, pruebas manuales y aceptación de Shirley; PR pendiente. Leslie autorizó los commits y el push de la rama en su solicitud posterior a la implementación.
5. HU-05 ampliará el ancla Asignaturas y después Edwin podrá incorporar HU-06; no se implementaron esas historias en esta entrega.
