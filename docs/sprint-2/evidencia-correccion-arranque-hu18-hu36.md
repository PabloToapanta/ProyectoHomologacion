# PR #55: corrección de arranque HU-18/HU-36

03/11/2026, fecha local Ecuador. Rama feature/hu-18-analisis-interno, sobre
6781678. Relacionada con Issue #42 y la integración HU-36. Leslie autorizó corregir
el fallo y publicar el ajuste en la misma PR, sin merge.

## Fallo y corrección

La revisión de código de Shirley figura APPROVED en GitHub, enviada el
04/11/2026 a las 04:19:22 UTC para la versión anterior de la PR. No se confunde esa
revisión con una prueba de modelo real ni con aceptación funcional de todos los criterios.

La ejecución [HU-36 fallida](https://github.com/PabloToapanta/ProyectoHomologacion/actions/runs/41771149654)
pasó sus suites .NET, pero falló al comprobar el diagnóstico de clave interna ausente
en Homologación. Program.cs intentaba aplicar migraciones antes de resolver las
validaciones existentes de clave interna y JWT. Con SQL no disponible, el diagnóstico
de base ocultaba el de seguridad y se intentaba acceder a SQL antes de validar la clave.

TDD: se añadieron cuatro pruebas (clave nula, vacía, corta y JWT inválido) con
Database:ApplyMigrations=true y una fábrica de DbContext que registra la resolución
y lanza una excepción sin conectar. Las cuatro fallaron antes de corregir. Después
se movieron las dos validaciones existentes antes del bloque de migraciones. Las
pruebas exigen el error de seguridad y cero resoluciones del contexto de base.
No se alteraron permisos, claves, algoritmos, motor, modelo, migraciones ni esquema.

El smoke local superó entonces el fallo de seguridad y detectó un cierre transitorio
de conexión al esperar el login del Gateway. Se añadieron RemoteDisconnected y
ConnectionResetError a las excepciones de conexión ya reintentadas solo en esa espera.
Se conserva el límite de 120 segundos y cada aserción de seguridad. El resto de
errores y comprobaciones siguen provocando fallo. No se deshabilita ningún paso CI.

## Resultados locales reales

- `dotnet test Homologacion.sln -c Release --no-restore`: 436 correctas, 0 fallidas,
  0 omitidas. Domain 71, Application 65, Homologacion.Api 113, Auth.Application 13,
  AuthService 64 y Gateway 110. Compilación Release incluida.
- Las cuatro pruebas nuevas tienen fallos previos registrados en
  TestResults/hu36-arranque-red; resultados finales en TestResults/hu36-arranque-verde.
- `python -u tests/security/hu36-smoke.py`: repetición final con 52 comprobaciones
  correctas. SQL Server, los cuatro servicios y Gateway reales; secretos y datos
  sintéticos, proyecto hu36-check-ae70a1edca. No se leyeron .env existentes ni se
  tocaron bases persistentes. Se eliminaron solo su volumen y contenedores temporales.
- Formato whitespace del test modificado verificado y diff comprobado. No se
  repitió frontend localmente: no hay cambios en esa capa. No se invocó Ollama real.

Evidencia saneada: [correccion-arranque.json](evidencias/hu18-20261103/correccion-arranque.json).
No se suman estas repeticiones a las 805 pruebas de la entrega anterior. La nueva
ejecución de GitHub se comprobará después del push; la aprobación anterior de Shirley
no se presenta como revisión de cambios publicados posteriormente.
