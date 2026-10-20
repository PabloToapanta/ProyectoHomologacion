# Integración de Pablo — HU-15, HU-16 y entorno disponible del Sprint 1

Fecha: 20/10/2026. Verificación final: 16:59, America/Guayaquil. Responsable: Pablo.
Rama: `feature/hu-15-integracion-sprint-1`, creada desde `origin/develop` `2f74fb9`
y actualizada por avance directo a `e0caaa8`, conservando los cambios pendientes.
Issues relacionados: #6 (HU-15), #7 (HU-16), #14 (entorno) y regresiones #1, #2, #3 y #5.
Estado al finalizar la verificación: pendiente de publicación, revisión cruzada
y aceptación de Shirley. Durante esa verificación no se realizaron commits propios,
pushes ni commits de merge; la actualización de base fue un avance directo autorizado
a los commits ya publicados. Pablo autorizó posteriormente publicar estos cambios
en commits atómicos y abrir un PR hacia `develop`; la aceptación sigue pendiente.

## Problema corregido y límites

El PR #24 integró el dominio, caso de uso, controlador y pruebas de Edwin.
Su evidencia solicitaba a Pablo registrar `ValidarAntiguedad` en el arranque
compartido. Ese registro faltaba: las peticiones válidas devolvían 500, aunque
las pruebas HTTP existentes añadían DI por separado y pasaban.

Primero se añadieron nueve pruebas de composición que reutilizan `MotorFactory`
para configurar JWT/umbral, pero **no registran casos de uso**. Antes del cambio:
8 fallidas por HTTP 500 y 1 aprobada (rol no autorizado, 403). Después de añadir
`AddScoped<ValidarAntiguedad>()` a `Program.cs`: 9 aprobadas.

Al incorporar el PR #26, se reprodujo el mismo pendiente en HU-16. Once pruebas
nuevas de composición, sin registros DI adicionales: 10 fallidas por HTTP 500 y
1 aprobada (rol no autorizado). Tras registrar `ValidarRequisitosCambio` en
`Program.cs`, las 11 pasan dentro de la suite completa. Las nuevas pruebas cubren
los siete escenarios de mínimos y los cuatro casos de roles.

Se actualizó el título/descripción de Swagger para HU-14/HU-15/HU-16. No se cambiaron
reglas, DTO, rutas, política JWT, fechas de calendario, controles de roles ni
Domain/Application de Edwin. HU-14, HU-15 y HU-16 siguen siendo evaluaciones independientes.
No se modificaron los archivos existentes de pruebas de Edwin: la regresión de
composición se cubre en `AntiguedadComposicionTests.cs` y `RequisitosCambioComposicionTests.cs`.

## Arranque integrado

Se añadieron `compose.sprint1.yaml`, `.env.example` sin secretos y la
[guía de ejecución](ejecucion-integrada.md). El stack incluye SQL Server, Auth,
Catálogo, Homologación, Gateway y Frontend; no agrega servicios de otros sprints.
Se verificó la resolución de configuración desde la plantilla con valores
temporales: seis servicios, parámetros JWT compatibles, bases separadas, SQL
sin puerto publicado y puertos HTTP limitados a loopback.

El stack reutiliza una instancia física SQL con bases propias por servicio,
permitida por la arquitectura. No se comparten entidades, DbContext ni consultas
entre Auth y Catálogo. Los destinos YARP, puertos y umbral son externos.
Se conservan los Compose anteriores de cada desarrollador sin editarlos.
La composición y el usuario SQL administrador son para desarrollo local.

Se añadió `tests/sprint-1/smoke.py`, sin dependencias nuevas, que reutiliza el
smoke HU-14 existente y prueba HTTP real con autenticación. No crea ni elimina
datos académicos; el POST con facultad distinta de FISEI comprueba su rechazo 400.
El smoke espera disponibilidad de Auth hasta 90 segundos; `compose --wait` por
sí solo no certifica disponibilidad HTTP de todas las APIs.

## Resultados ejecutados

| Verificación | Resultado real |
|---|---|
| Restore solución principal y catálogo | Correctos |
| Build Release principal y catálogo | Ambos correctos, 0 advertencias y 0 errores |
| Auth.Application.Tests | 13 aprobadas |
| AuthService.Tests | 43 aprobadas |
| Gateway.Tests | 61 aprobadas |
| Homologacion.Domain.Tests | 71 aprobadas |
| Homologacion.Application.Tests | 32 aprobadas |
| Homologacion.Api.Tests | 83 aprobadas |
| Catalogo.Tests | 71 aprobadas |
| Total .NET | **374 aprobadas, 0 fallidas, 0 omitidas** |
| Frontend TypeScript y Vite | Correctos |
| Frontend Vitest | **85 aprobadas**, 9 archivos |
| Chromium, autenticación escritorio y móvil | **2 aprobadas**, 2,4 segundos |
| Compose integrado | Seis contenedores en ejecución; SQL saludable; actualización HU-16 recreó solo Homologación |
| Smoke HTTP integrado | Correcto, sin mocks |

El smoke comprobó login 200, credenciales incorrectas 401, ausencia/token inválido
401 en catálogo y los tres endpoints de homologación. Carreras y asignaturas se
consultan correctamente; otra facultad se rechaza con 400.

HU-15 por Gateway produjo HTTP 200 y estado correcto en aprobación menor de diez
años, exactamente diez años, más de diez años, reprobada y no cursada. Información
incompleta, fecha futura y cuerpo vacío devolvieron 400. Los roles Administrador,
Docente y Coordinador se comprobaron en las pruebas de composición; rol no
permitido devolvió 403. No se falsificaron roles en el entorno de datos real.

HU-16 por Gateway produjo HTTP 200 con `cumple` y motivos coherentes en siete
casos: interno mínimo, interno sin homologables, interno sin períodos, externo
mínimo, externo con una homologable, externo con un período y externo con cambio
previo. Cuerpo vacío, campo obligatorio ausente, valor negativo, tipo desconocido,
tipo numérico y resultado inyectado (`cumple`) se rechazaron con 400. Son datos
sintéticos explícitos; la cantidad declarada no certifica homologaciones reales.
Swagger del contenedor desplegado incluye los tres endpoints y anuncia HU-16.

HU-14 por Gateway conservó el resultado Homologable con tres criterios al 80 %,
y el estado InformacionIncompleta sin sumar horas/créditos de múltiples orígenes.
React respondió 200 y las dos pruebas E2E comprobaron login, error, recarga,
logout, protección de rutas y escritorio/móvil. No se volvió a ejecutar aquí el
E2E de catálogo que agrega datos; sus suites de backend/frontend sí se ejecutaron.

Salidas registradas en [evidencias de integración](evidencias/integracion-pablo/).
Los TRX completos y capturas E2E finales quedaron temporalmente en `/tmp/pablo-hu16-tests`
y `/tmp/pablo-hu16-e2e`; las salidas de catálogo están en `/tmp/pablo-hu16-catalogo-tests.log`.
No se copia configuración
privada en estos documentos. La instalación desde un volumen totalmente vacío
no se ejecutó en esta actividad: se verificó la actualización de la demo existente.

## Conservación de la demostración

En la primera actividad de HU-15, la demostración estaba detenida. Se inició su contenedor SQL existente
y luego los contenedores previos para obtener el inventario antes de reconstruir.
Se verificó el nombre real de la base Catálogo y del volumen antes de actualizar.
En la verificación final de HU-16 los seis contenedores estaban activos: se tomó
otro inventario antes/después y se reconstruyó solo Homologación con `--no-deps`.
SQL Server y los demás servicios permanecieron activos.

- Proyecto conservado: `fisei-uta-demo`.
- Base Catálogo conservada: `HomologacionCatalogoDemo`.
- Volumen conservado: `fisei-uta-demo_auth-data`.
- Inventario antes/después: **2 carreras y 1 asignatura**, con idénticos datos JSON.
- No se ejecutó `down --volumes`, corrección masiva ni reasignación de facultades.
- Se conserva el login con las credenciales privadas anteriores.

El controlador privado `.env.demo.control.sh` ahora usa el Compose versionado;
se añadió el nombre de base al archivo privado. Los archivos locales anteriores
se copiaron a `/tmp` con permisos 600 antes de adaptarlos. Esto es respaldo de
configuración, **no un respaldo completo de SQL Server**.

## Pendientes reales

- PR de estos cambios, revisión cruzada y aceptación funcional de Shirley.
- HU-06 aún pendiente de integración por Edwin; el stack no implementa sus rutas
  ni puede certificar el Sprint completo.
- Validar el flujo de unidades/contenidos después de integrar el PR de HU-06.
- Contrato de procedencia externa nacional y catálogo: coordinación con Edwin/Leslie.
- Corregir registros históricos de la demo solo tras revisión y decisión explícita;
  la UI FISEI existente los conserva identificados y no los convierte automáticamente.
- Mantener en HU-14 información incompleta donde faltan reglas aprobadas de
  agregación horaria/créditos o conversión entre sistemas. No se inventaron reglas.
- La convención de aniversario bisiesto de HU-15 permanece como la implementó Edwin,
  documentada para revisión académica; esta integración no la altera.
