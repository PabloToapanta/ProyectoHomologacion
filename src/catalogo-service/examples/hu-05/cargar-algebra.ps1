[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][Guid]$CarreraId,
    [string]$BaseUrl = 'http://127.0.0.1:5082'
)
$ErrorActionPreference = 'Stop'
$baseUri = [Uri]$BaseUrl
if (-not $baseUri.IsAbsoluteUri -or $baseUri.Scheme -notin @('http', 'https') -or $baseUri.UserInfo -or $baseUri.Query -or $baseUri.Fragment -or $baseUri.AbsolutePath -ne '/') {
    throw 'BaseUrl debe ser un origen HTTP(S), sin credenciales, ruta, query ni fragmento.'
}
if ($CarreraId -eq [Guid]::Empty) { throw 'CarreraId es obligatorio.' }
if ([string]::IsNullOrWhiteSpace($env:CATALOGO_DEMO_TOKEN)) { throw 'Configura CATALOGO_DEMO_TOKEN con un JWT de administrador; no lo guardes en archivos versionados.' }
$headers = @{ Authorization = 'Bearer ' + $env:CATALOGO_DEMO_TOKEN }
$baseAddress = $BaseUrl.TrimEnd('/')
# Solo datos de demostración; no se crean carreras ni se modifica información existente.
$carrera = Invoke-RestMethod -Uri "$baseAddress/api/carreras/$CarreraId" -Headers $headers
if ($carrera.nombre.Trim() -ine 'Software') { throw 'T8 requiere una carrera Software existente. Verifica el UUID antes de cargar.' }
$datos = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'algebra-lineal.json') -Raw | ConvertFrom-Json
$lista = Invoke-RestMethod -Uri "$baseAddress/api/carreras/$CarreraId/asignaturas" -Headers $headers
$existente = @($lista | Where-Object { $_.codigo.Trim() -ieq $datos.codigo })
if ($existente.Count -gt 0) {
    $item = $existente[0]
    if ($item.nombre -cne $datos.nombre -or $item.nivel -ne $datos.nivel -or $item.creditos -ne $datos.creditos -or $item.horas -ne $datos.horas) {
        throw 'El código de demostración ya existe con otros datos. No se sobrescribió información.'
    }
    Write-Output "Álgebra Lineal ya estaba registrada; no se creó un duplicado. UUID: $($item.id)"
    return
}
$body = @{ carreraId = $CarreraId.ToString(); codigo = $datos.codigo; nombre = $datos.nombre; nivel = $datos.nivel; creditos = $datos.creditos; horas = $datos.horas } | ConvertTo-Json
try {
    $result = Invoke-RestMethod -Uri "$baseAddress/api/asignaturas" -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([System.Text.Encoding]::UTF8.GetBytes($body))
} catch {
    throw 'No se pudo registrar la demostración. Revisa sesión, permisos, servicio y posibles registros concurrentes; puedes reintentar sin sobrescribir datos.'
}
Write-Output "Álgebra Lineal registrada como demostración (código sintético DEMO). UUID: $($result.id)"
