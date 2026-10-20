# HU-16 / T12 — mínimos para cambio interno y externo

Fecha: 20/10/2026. Desarrollo: Edwin con asistencia de Codex.
Issue consultado: [#7](https://github.com/PabloToapanta/ProyectoHomologacion/issues/7), cuyos
criterios coinciden con los locales. No se modificó el Issue remoto.
Rama: `feature/hu-16-requisitos`, creada desde `develop` local (`ad9ee26`).
HU-15 permanece en su propia rama y no forma parte de este cambio.

## Implementación

Se reutilizan los proyectos del microservicio compartido; no se crea otro servicio.

| Capa | Tipos nuevos | Responsabilidad |
|---|---|---|
| Domain/RequisitosCambio | TipoCambio, ResultadoRequisitosCambio, ValidadorRequisitosCambio | Reglas puras y motivos de incumplimiento |
| Application/RequisitosCambio | SolicitudRequisitosCambio, ValidarRequisitosCambio | Contrato y caso de uso |
| Api/Contracts | ValidarRequisitosRequest, ValidarRequisitosResponse | Validación de entrada y contrato HTTP |
| Api/Controllers | RequisitosCambioController | Endpoint y Problem Details |

No se necesitan adaptadores en Infrastructure: no hay persistencia ni servicios externos.
No se modificaron HU-14, HU-15, solución, proyectos, composición, JWT ni Gateway.

Reglas implementadas:

- Interno: al menos un período y una asignatura homologable.
- Externo: al menos dos períodos y dos asignaturas homologables, sin cambios previos.
- Los requisitos no se compensan entre sí. Se devuelven todos los incumplimientos.
- Los cambios previos no añaden una restricción al caso interno, pues el criterio solo
  la establece para el externo.
- Cantidades negativas o tipo desconocido son datos inválidos, no decisiones académicas.

`Cumple` se refiere exclusivamente a estos mínimos de T12. No verifica gratuidad,
apostilla ni documentos extranjeros y no autoriza un cambio de carrera. Esas condiciones
quedan pendientes cuando correspondan; no se inventa un estado documental.
La cantidad de homologables es una entrada declarada: el módulo no certifica ni calcula
esa cantidad. El consumidor debe obtenerla de evaluaciones completas verificadas, sin
contar `InformacionIncompleta` de HU-14 como homologable. No hay integración automática
con catálogo, HU-14 o HU-15 en este incremento.

## Contrato preparado

`POST /api/homologaciones/validar-requisitos` con JWT y política de roles existente.
Ejemplo sintético:

```json
{
  "tipoCambio": "Externo",
  "periodosCursados": 2,
  "asignaturasHomologables": 2,
  "cambiosPrevios": 0
}
```

Respuesta HTTP 200:

```json
{
  "cumple": true,
  "incumplimientos": []
}
```

Todos los campos son obligatorios, sin valores por defecto que conviertan ausencias
en cero. Tipo exacto `Interno` o `Externo`; cantidades enteras no negativas.
Un incumplimiento académico responde 200 con `cumple: false` y motivos.
Datos ausentes, negativos, tipos desconocidos o campos JSON adicionales: 400 Problem Details.
Sin autenticación válida: 401; rol no autorizado: 403.
Swagger incluye endpoint, contratos y códigos HTTP.

## TDD y ejecución real

Pruebas de reglas escritas primero, con contrato mínimo que lanzaba
`NotImplementedException`: **16 fallidas, 0 aprobadas**. Se implementaron después las reglas.
Suites completas ejecutadas con SDK .NET 8.0.426:

| Suite | Aprobadas | Fallidas | Omitidas |
|---|---:|---:|---:|
| Homologacion.Domain.Tests | 56 | 0 | 0 |
| Homologacion.Application.Tests | 31 | 0 | 0 |
| Homologacion.Api.Tests | 50 | 0 | 0 |

Total: **137 aprobadas**, incluidas **40 nuevas de HU-16** (16 Domain, 5 Application,
19 API). Las restantes corresponden a la regresión disponible en la base `develop`.
Las pruebas de HU-15 no están en esta rama independiente.

| Criterio | Entrada: tipo / períodos / homologables / cambios | Resultado comprobado |
|---|---|---|
| Interno mínimo | Interno / 1 / 1 / 0 | Cumple en Domain, Application y HTTP |
| Interno sin homologables | Interno / 1 / 0 / 0 | No cumple en las tres capas |
| Externo mínimo | Externo / 2 / 2 / 0 | Cumple en las tres capas |
| Externo con una homologable | Externo / 2 / 1 / 0 | No cumple en las tres capas |
| Externo con cambio previo | Externo / 2 / 2 / 1 | No cumple en las tres capas |

También se comprobaron períodos insuficientes, valores mayores al mínimo, cero,
negativos, tipo desconocido, cambios previos en interno, múltiples motivos,
datos HTTP incompletos, JWT, rol no autorizado y presencia en Swagger.

Comandos ejecutados, con `DOTNET_CLI_HOME=/tmp/hu15-cli`, `NUGET_PACKAGES=/tmp/hu15-nuget`
y ejecutable `/tmp/hu15-dotnet/dotnet`:

```bash
dotnet test tests/Homologacion.Domain.Tests/Homologacion.Domain.Tests.csproj --filter FullyQualifiedName~RequisitosCambio --verbosity minimal
dotnet test tests/Homologacion.Domain.Tests/Homologacion.Domain.Tests.csproj --verbosity minimal
dotnet test tests/Homologacion.Application.Tests/Homologacion.Application.Tests.csproj --verbosity minimal
dotnet test tests/Homologacion.Api.Tests/Homologacion.Api.Tests.csproj --verbosity minimal
```

## Propuesta de integración con Pablo — pendiente

El arranque normal todavía no registra el caso de uso; **el endpoint no está listo
para uso normal**. Una llamada autenticada válida fallará sin el registro.
Las pruebas HTTP registran el caso de uso únicamente en su servidor de pruebas.
Coordinar con Pablo este cambio en `Homologacion.Api/Program.cs`, antes de `builder.Build()`:

```csharp
builder.Services.AddScoped<Homologacion.Application.RequisitosCambio.ValidarRequisitosCambio>();
```

Los archivos nuevos se incluyen automáticamente en los proyectos SDK existentes.
La ruta exacta ya está configurada en Gateway con `AcademicAccess`; no se editó.
Coordinar también la descripción global de Swagger, que sigue anunciando HU-14.
Después del registro, comprobar arranque normal, Gateway y Docker Compose.

Pendientes de DoD: acuerdo de composición, ejecución integrada con Docker Compose,
PR hacia `develop`, revisión cruzada de Leslie y aceptación/evidencias de Shirley.
No se ejecutó Docker Compose ni se afirma aceptación funcional. No se hicieron
commits, push, merge ni comunicaciones al equipo.
