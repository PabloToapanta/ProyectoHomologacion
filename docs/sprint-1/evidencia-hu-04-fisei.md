# HU-04 / Issue #2 — SC-01: catálogo receptor FISEI

Fecha: 20/10/2026. Leslie confirmó en la conversación que acordó el alcance con
Shirley y autorizó implementarlo. Rama `feature/hu-04-alcance-fisei`, desde
`origin/develop` `12fc7b4`. Esto registra autorización del alcance, no aceptación
funcional ni una norma institucional. No adelanta HU-06/HU-16.

## Comportamiento implementado

- Carrera valida la facultad en Domain antes de modificar propiedades: únicamente
  FISEI, con caso/espacios externos normalizados y valor canónico `FISEI`.
  No se admiten alias arbitrarios ni el nombre completo sin acuerdo específico.
- POST/PUT conservan rutas, DTO, UUID y campo facultad obligatorio. Otro valor
  devuelve 400; duplicados normalizados siguen dando 409. Swagger actualizado.
- React presenta FISEI fija y la envía automáticamente. Nombre, modalidad y
  vigencia conservan su funcionamiento. El backend protege también llamadas directas.
- Los registros anteriores conservan su facultad y relaciones al consultar.
  La UI los identifica como pendientes de revisión y bloquea su edición para
  evitar convertirlos silenciosamente. No se ocultan ni eliminan datos históricos.
- Se conservan Clean Architecture, esquema, índice único y FK sin cascada.
  Sin dependencias nuevas ni cambios en Auth, Gateway, Homologación o App.tsx.
- Se adaptan smoke HTTP, E2E y fixtures de HU-05. T8 reutiliza Software FISEI
  y conserva la carga repetible de Álgebra Lineal sin duplicarla.

## Pruebas ejecutadas

| Comprobación | Resultado |
|---|---|
| TDD backend antes de implementar | 9 fallidas esperadas y 8 correctas: aún se admitían otras facultades y no se canonizaba FISEI |
| TDD frontend antes de implementar | 4 fallidas esperadas y 8 correctas: campo editable, vacío y registros anteriores editables |
| Restauración del catálogo | Correcta, SDK .NET 8.0.130 |
| Build Release del catálogo | Correcto: 0 advertencias y 0 errores |
| Suite completa catálogo | 71 correctas, incluyendo CRUD, unicidad, eliminación restringida, API, Swagger, capas y lectura de datos anteriores con asignaturas |
| Suite frontend | 85 correctas |
| TypeScript / Vite | Build correcto |
| Compose | Reconstruidos únicamente catálogo y frontend; mismos volúmenes |
| HTTP real por Auth/Gateway/Catálogo/SQL Server | Otra facultad 400 en POST/PUT; FISEI canónica 201; edición válida 200; duplicado 409; borrado relacionado 409; concurrencia 201/409; regresiones de asignaturas y T8 correctas |
| Chromium escritorio y móvil | 2 pruebas correctas, formulario fijo al crear/editar, API directa inválida rechazada, datos conservados, asignaturas, logout y móvil sin desbordamiento |

Evidencias en [hu-04-fisei](evidencias/hu-04-fisei/): TRX rojo/verde,
JSON frontend rojo/verde, HTTP real, navegador y capturas.

## Inventario y conservación

Se inventarió exclusivamente la demostración local `hu05-validacion`:
8 carreras, 9 asignaturas. Facultades previas: Facultad sintética (3),
Facultad sintética de pruebas (4), Validación sintética f9f96195 (1).
No se encontraron nombres completos equivalentes a FISEI en este entorno.
Este inventario no representa la base de Pablo ni otros despliegues.

Las comprobaciones de conteos y CHECKSUM_AGG de ambas tablas coincidieron antes
y después de recrear los servicios, antes de generar los nuevos datos de prueba.
Ver `inventario-antes.txt`, `inventario-despues.txt` e
`tests/catalogo-service/inventario-fisei.sql`. CHECKSUM es una comprobación rápida,
no un respaldo ni una garantía criptográfica.

El primer smoke HTTP se lanzó antes de terminar la reconstrucción y alcanzó la
API anterior (201 en vez de 400). Su log se conserva en
`http-antes-de-recrear.txt`. Tras terminar la recreación, se identificó únicamente
su carrera sintética nueva, se verificó que no tenía asignaturas y se retiró.
La lectura inicial del arreglo vacío en PowerShell requirió corregir su
enumeración; no se ejecutó ningún borrado en ese primer intento de limpieza.
Luego se verificó igualdad de inventarios y el smoke completo pasó en la versión
nueva. Las pruebas posteriores dejan datos sintéticos FISEI para demostración.

## Plan para datos anteriores

1. Inventariar cada entorno con el SQL de solo lectura y revisar facultades,
   nombres normalizados, UUID y asignaturas con el responsable de sus datos.
2. Obtener y verificar un respaldo antes de corregir datos persistentes.
3. Acordar por UUID cuáles son demostraciones, equivalencias reales de FISEI
   o carreras de otra facultad. No inferirlo por el nombre Software.
4. Revisar colisiones de nombre al pasar a FISEI. No fusionar ni eliminar carreras
   para sortear un 409. Conservar sus UUID y relaciones.
5. Corregir solo registros expresamente acordados con PUT existente y FISEI
   explícita; comprobar por API las asignaturas y datos antes/después y registrar
   la actuación. No hay script automático ni cambio de facultad al arrancar.
6. Mantener los casos pendientes identificados y coordinar su tratamiento con
   Shirley/Pablo; el origen externo tendrá un contrato separado con Edwin.

No se ejecutó corrección masiva ni respaldo de otras bases. El tratamiento de
los registros anteriores queda pendiente; el software no los reasigna por sí solo.

## Repetir

Con Node 24 configurado, Docker activo y configuración privada local existente:

```powershell
dotnet restore src/catalogo-service/Catalogo.sln
dotnet build src/catalogo-service/Catalogo.sln --no-restore -c Release
dotnet test src/catalogo-service/Catalogo.sln --no-restore -c Release
./scripts/iniciar-hu05.ps1
./tests/catalogo-service/smoke-hu05-integrado.ps1
./scripts/e2e-hu05.ps1 -OutputDirectory docs/sprint-1/evidencias/hu-04-fisei/navegador
```

Dentro de frontend: `npm ci`, `npm test -- --maxWorkers=2` y `npm run build`.
Instalar Chromium según la evidencia de integración HU-05 si aún no está disponible.
Esperar que el arranque termine antes de ejecutar HTTP/E2E. No versionar `.env`.

## Pendientes

- Revisión cruzada de Pablo e integración del ajuste mediante PR.
- Shirley registra la aceptación funcional del comportamiento nuevo.
- Corrección controlada de datos históricos por entorno, con respaldo y acuerdo
  por registro. No cambia el código de procedencia ni los mínimos de HU-16.
