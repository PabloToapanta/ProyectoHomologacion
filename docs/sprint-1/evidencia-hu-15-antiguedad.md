# HU-15 / T11 — aprobación y antigüedad

Fecha: 20/10/2026. Desarrollo: Edwin con asistencia de Codex.
Rama local: `feature/hu-15-antiguedad`, creada desde `develop`.
Issue consultado por API de GitHub: [#6](https://github.com/PabloToapanta/ProyectoHomologacion/issues/6).
Los cuatro criterios remotos coinciden con los locales. No se actualizó el Issue.

## Alcance implementado

`homologacion-service` y HU-14 ya existen en `develop`. Se agregaron archivos propios a
sus proyectos existentes, sin copiar la rama de Pablo ni crear otro servicio.

- Domain: `ValidadorAntiguedad`, `EstadoAsignatura`, `EstadoAntiguedad`, `ResultadoAntiguedad`.
- Application: `ValidarAntiguedad`, `SolicitudAntiguedad`.
- Api: `AntiguedadController`, `ValidarAntiguedadRequest`, `ValidarAntiguedadResponse`.
- Pruebas: `AntiguedadTests`, `ValidarAntiguedadTests`, `AntiguedadEndpointTests`.
- Infrastructure: no requiere adaptadores, persistencia ni reloj; las fechas son entradas explícitas.

Las reglas puras no dependen de HTTP, EF Core, configuración, HU-14 ni LLM.
Una aprobación con hasta diez años inclusive es `Elegible`; sin aprobación es
`NoElegible`; con más de diez años es `RequiereValidacionConocimientos`.
Estos estados solo responden a HU-15; elegible no significa homologable ni aprobación oficial.

Se compara la fecha de evaluación con `fechaAprobacion.AddYears(10)`, sin aproximación a días.
Se usa `DateOnly` sin conversión de zonas temporales. Para el 29 de febrero, `AddYears`
sitúa el aniversario en el 28 de febrero si el año destino no es bisiesto: convención
técnica explícita, no norma académica adicional; someterla a revisión de aceptación.
Se evita desbordar el calendario cuando todavía no han transcurrido diez años.

Fecha de aprobación futura, estado desconocido o aprobación sin fecha producen error
de datos; no se interpretan como una decisión académica negativa. La fecha ausente
se explica como «Información incompleta». Reprobada/no cursada no requieren fecha de aprobación.

## Contrato HTTP preparado

`POST /api/homologaciones/validar-antiguedad`, con la protección JWT y roles existentes.
Entrada de ejemplo sintética:

```json
{
  "estadoAsignatura": "Aprobada",
  "fechaAprobacion": "2016-10-20",
  "fechaEvaluacion": "2026-10-20"
}
```

Respuesta HTTP 200:

```json
{
  "estado": "Elegible",
  "motivo": "Asignatura aprobada con antigüedad de hasta diez años inclusive."
}
```

Estados de entrada exactos: `Aprobada`, `Reprobada`, `NoCursada`.
Estado y fecha de evaluación obligatorios; fechas ISO `yyyy-MM-dd`.
Datos inválidos: HTTP 400 con Problem Details. Sin JWT válido: 401; política de roles
heredada del servicio. Campos JSON desconocidos y estados numéricos se rechazan.
Swagger incluye ruta, contratos y códigos HTTP.

## TDD y resultados ejecutados

SDK .NET 8.0.426 instalado temporalmente en `/tmp/hu15-dotnet` porque no había SDK en PATH.
Paquetes en `/tmp/hu15-nuget`, perfil CLI en `/tmp/hu15-cli`.
Se requirió ejecución fuera del sandbox por el mutex de NuGet y acceso de restauración.

1. Pruebas escritas antes de implementar reglas: primer intento falló por tipos inexistentes.
2. Con contrato mínimo y `NotImplementedException`: **15 fallidas, 0 aprobadas**.
3. Con reglas implementadas: **15 aprobadas, 0 fallidas**.
4. Suites completas de homologación:

| Proyecto | Aprobadas | Fallidas | Omitidas |
|---|---:|---:|---:|
| Homologacion.Domain.Tests | 55 | 0 | 0 |
| Homologacion.Application.Tests | 27 | 0 | 0 |
| Homologacion.Api.Tests | 44 | 0 | 0 |

Total: **126 pruebas**, incluidas **29 nuevas de HU-15** (15 Domain, 1 Application, 13 Api).
Las pruebas HTTP usan el servidor ASP.NET de pruebas y un registro DI aislado de HU-15.
No prueban que el arranque normal ya tenga ese registro.

Comandos ejecutados (con las variables temporales anteriores):

```bash
dotnet test tests/Homologacion.Domain.Tests/Homologacion.Domain.Tests.csproj --filter FullyQualifiedName~Antiguedad --verbosity minimal
dotnet test tests/Homologacion.Domain.Tests/Homologacion.Domain.Tests.csproj --verbosity minimal
dotnet test tests/Homologacion.Application.Tests/Homologacion.Application.Tests.csproj --verbosity minimal
dotnet test tests/Homologacion.Api.Tests/Homologacion.Api.Tests.csproj --verbosity minimal
```

| Criterio / límite | Entrada sintética | Resultado comprobado |
|---|---|---|
| Menos de diez años | Aprobada 2017-10-20; evaluación 2026-10-20 | Elegible, Domain y HTTP |
| Exactamente diez años | Aprobada 2016-10-20; evaluación 2026-10-20 | Elegible, Domain y HTTP |
| Reprobada / no cursada | Ambos estados sin fecha | NoElegible, Domain y HTTP |
| Más de diez años | Aprobada 2016-10-19; evaluación 2026-10-20 | RequiereValidacionConocimientos, Domain y HTTP |
| A un día del aniversario | Aprobada 2016-10-21; evaluación 2026-10-20 | Elegible |
| Aprobación el mismo día | 2026-10-20 en ambas fechas | Elegible |
| Aprobación bisiesta | 2016-02-29; evaluación 2026-02-27/28 y 2026-03-01 | Elegible / Elegible / RequiereValidacionConocimientos |
| Fecha futura | Aprobada 2026-10-21; evaluación 2026-10-20 | Error de datos, HTTP 400 |
| Aprobación sin fecha | Aprobada; evaluación 2026-10-20 | Información incompleta, HTTP 400 |

También se ejecutaron pruebas de estado desconocido, fechas mal formadas, campos
obligatorios, extremo del calendario, autenticación y presencia en Swagger.

## Integración pendiente con Pablo

No se modificaron solución, `.csproj`, `Program.cs`, JWT, Gateway ni motor de equivalencias.
Los proyectos SDK incluyen automáticamente los archivos nuevos. El Gateway ya reserva
la ruta exacta de HU-15 hacia el clúster `homologacion`; no se cambió esa configuración.

**El endpoint no está habilitado para uso normal todavía:** falta registrar su caso de uso.
El controlador resuelve el caso de uso por `[FromServices]` al invocar la acción; sin el
registro DI una llamada autenticada válida fallará. Coordinar con Pablo este cambio mínimo
en `Homologacion.Api/Program.cs` antes de desplegar o integrar:

```csharp
builder.Services.AddScoped<Homologacion.Application.Antiguedad.ValidarAntiguedad>();
```

El registro debe preceder a `builder.Build()`. Actualizar con Pablo el título/descripción
de Swagger que actualmente anuncia HU-14, sin cambiar autenticación ni política de roles.
Después comprobar el arranque normal, JWT, ruta completa por Gateway y Docker Compose.
La factory de pruebas registra el mismo caso de uso sin editar el archivo compartido.

Pendientes de DoD: integración de composición acordada, ejecución Docker Compose,
revisión cruzada por Leslie, PR hacia `develop` y aceptación/evidencias de Shirley.
No se ejecutó Docker Compose ni se afirma aceptación de la PO. No se hicieron commits,
pushes, merges ni comunicaciones externas al equipo.
