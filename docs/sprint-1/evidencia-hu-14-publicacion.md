# HU-14 / Issue #5 — integración previa a publicación y revisión

Fecha local: 2026-10-19. Rama: `feature/hu-14-equivalencia`.
El usuario autorizó expresamente commits atómicos, push, creación de PR hacia `develop` y solicitud
de revisión a Leslie. Esa autorización es posterior a las entregas técnicas que se registraron sin commits.
No implica autorización para fusionar la PR ni aceptación de la Product Owner.

## Integración elegida

Se actualizó `origin` y se consultó GitHub. `develop` incorporó los PR #19 (HU-04) y #20 (HU-05)
de Leslie. La revisión de esos PR y de sus permisos confirmó su usuario `LeslieCoello` (write).
No existía una PR abierta para HU-14 al comprobarlo antes de publicar.

Como HU-14 ya estaba publicada, se eligió **merge de origin/develop dentro de la rama de HU-14**,
conservando los commits existentes. No se hizo rebase, reset ni push forzado. El merge `6140c14`
incorpora `origin/develop` en `3ac1a1d` y terminó sin conflictos.
El diff respecto a develop conserva intactos catálogo, sus pruebas y frontend; sus cambios entran
por el merge, no se vuelven a implementar en HU-14. No se altera main ni se fusiona hacia develop.

Se separaron los cambios nuevos en commits Conventional Commits en español:

- `2351d15`: corrección de motor, aplicación, contrato HTTP y pruebas de tres criterios.
- `e8079be`: fixture y smoke de integración del contrato nuevo.
- `8a4990b`: contratos, criterios, coordinación y evidencias de la aclaración académica.
- `6140c14`: integración de develop, conservando la historia publicada.

Este registro se incorpora en un commit adicional de documentación. Las evidencias previas siguen
siendo históricas; sus conteos de 43 pruebas React corresponden al frontend anterior a HU-04/HU-05.

## Verificación ejecutada después del merge

| Componente | Resultado real |
|---|---|
| Homologacion.sln | restore y build Release correctos; 212 pruebas aprobadas |
| src/catalogo-service/Catalogo.sln | restore y build Release correctos; 62 pruebas aprobadas |
| Total .NET | 274 aprobadas, 0 fallidas y 0 omitidas; ambas compilaciones sin errores ni advertencias |
| Frontend integrado de Leslie | chequeo de tipos y build correctos; 84 pruebas aprobadas |
| Chromium sobre frontend Docker actualizado | 2 E2E aprobadas: autenticación/protección/logout y móvil |
| Smoke Auth → Gateway → Homologación | login real sobre SQL Server, tres criterios al 80 % y Homologable true |
| Smoke de varios orígenes | cobertura sin doble conteo; horas/créditos indeterminados y sin recomendación |

La solución del catálogo está separada de la solución raíz, por lo que se ejecutaron ambas.
No se cambian referencias de proyectos ni CI para reunirlas artificialmente.
No se ejecutó el script PowerShell de demostración de HU-05 porque `pwsh` no está instalado.
Las pruebas de catálogo utilizan SQLite; no se afirma aquí ejecución de sus migraciones en SQL Server.
TestServer advierte sobre el límite de cuerpo, y Node sobre variables de color; no hubo fallos.

Evidencias: [verificación](evidencias/hu-14-publicacion/verificacion.txt) y
[smoke](evidencias/hu-14-publicacion/compose-smoke.txt).
TRX y screenshots locales se guardaron en `/tmp`, fuera de Git. El stack de esta prueba es un
proyecto Docker aislado, con credenciales temporales fuera del repositorio; se retira al terminar.

## PR y revisión

La PR se prepara con destino `develop` y referencia #5, sin cierre automático del Issue.
El usuario solicita expresamente **Leslie como revisora**, sustituyendo para esta PR la asignación
inicial de revisión cruzada a Edwin. La solicitud de revisión no equivale a una aprobación recibida.
Revisión humana, aceptación de Shirley y CI remoto permanecen como comprobaciones externas;
no se afirma su resultado por las pruebas locales. No se fusionará la PR desde esta tarea.

El contrato es incompatible con la versión de cuatro dimensiones: ver
[guía de migración](../../src/homologacion-service/README.md) y
[aclaración académica y pendientes](evidencia-hu-14-tres-criterios.md).
Quedan pendientes las reglas de agregación entre varios orígenes, conversiones de sistemas distintos
y verificación humana de las correspondencias con planes autorizados; esas situaciones no se
resuelven por suposición ni bloquean la revisión del comportamiento conservador implementado.
