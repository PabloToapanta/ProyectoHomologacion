[CmdletBinding()]
param([string]$EnvFile = '.env.hu05-validacion')
$ErrorActionPreference = 'Stop'
$settings = @{}
foreach ($line in Get-Content -LiteralPath $EnvFile) {
    if ($line -and -not $line.StartsWith('#')) { $at = $line.IndexOf('='); if ($at -gt 0) { $settings[$line.Substring(0, $at)] = $line.Substring($at + 1) } }
}
$base = 'http://127.0.0.1:' + $settings.GATEWAY_HTTP_PORT
$headers = @{}
function VerificarHttp([string]$method, [string]$path, $body, [int]$expected, [string]$label) {
    $params = @{ Uri=$base+$path; Method=$method; Headers=$headers; SkipHttpErrorCheck=$true; TimeoutSec=30 }
    if ($null -ne $body) { $params.ContentType = 'application/json; charset=utf-8'; $params.Body = [Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Depth 5)) }
    $response = Invoke-WebRequest @params
    if ([int]$response.StatusCode -ne $expected) { throw "$label : esperado HTTP $expected, recibido $($response.StatusCode)." }
    Write-Host "PASS: $label (HTTP $expected)"
    if ($response.Content) { return ($response.Content | ConvertFrom-Json) }
}

VerificarHttp 'GET' '/api/carreras' $null 401 'sin sesión no accede al catálogo' | Out-Null
VerificarHttp 'POST' '/api/auth/login' @{ nombreUsuario=$settings.AUTH_ADMIN_USERNAME; password='incorrecta-sintetica' } 401 'credenciales incorrectas rechazadas' | Out-Null
$login = VerificarHttp 'POST' '/api/auth/login' @{ nombreUsuario=$settings.AUTH_ADMIN_USERNAME; password=$settings.AUTH_ADMIN_PASSWORD } 200 'login real por Gateway y Auth'
$headers = @{ Authorization='Bearer ' + $login.accessToken }
$mark = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$carrera = VerificarHttp 'POST' '/api/carreras' @{ nombre='Software'; facultad="Validación sintética $mark"; vigencia=$true } 201 'crear carrera Software de demostración'
$carreraId = $carrera.id
VerificarHttp 'GET' "/api/carreras/$carreraId" $null 200 'consultar carrera individual por Gateway' | Out-Null
VerificarHttp 'POST' '/api/carreras' @{ nombre=' software '; facultad="validación sintética $mark"; vigencia=$true } 409 'duplicado de carrera rechazado' | Out-Null
VerificarHttp 'PUT' "/api/carreras/$carreraId" @{ nombre='Software'; facultad="Validación sintética $mark"; vigencia=$false } 200 'editar vigencia de carrera' | Out-Null
$sinRelacion = VerificarHttp 'POST' '/api/carreras' @{ nombre="Sin asignaturas $mark"; facultad='Facultad sintética'; vigencia=$true } 201 'crear carrera sin asignaturas'
VerificarHttp 'DELETE' "/api/carreras/$($sinRelacion.id)" $null 204 'eliminar carrera sin asignaturas' | Out-Null
$lista = @(VerificarHttp 'GET' "/api/carreras/$carreraId/asignaturas" $null 200 'consultar carrera aún vacía')
if ($lista.Count -ne 0) { throw 'La nueva carrera debe estar vacía.' }
$inputAsignatura = @{ carreraId=$carreraId; codigo='DEMO-N2'; nombre='Asignatura sintética segundo nivel'; nivel=2; creditos=3; horas=144 }
VerificarHttp 'POST' '/api/asignaturas' $inputAsignatura 201 'registrar asignatura de nivel 2' | Out-Null
foreach ($field in @('nivel','creditos','horas')) {
    foreach ($value in @(0, -1)) {
        $invalid = $inputAsignatura.Clone(); $invalid[$field] = $value; $invalid.codigo = 'INVALIDO'
        VerificarHttp 'POST' '/api/asignaturas' $invalid 400 "$field = $value rechazado sin persistir" | Out-Null
    }
}
$missing = $inputAsignatura.Clone(); $missing.carreraId = [Guid]::NewGuid().ToString()
VerificarHttp 'POST' '/api/asignaturas' $missing 404 'carrera inexistente rechazada' | Out-Null
$demoScript = Join-Path $PSScriptRoot '../../src/catalogo-service/examples/hu-05/cargar-algebra.ps1'
$oldToken = $env:CATALOGO_DEMO_TOKEN
try {
    $env:CATALOGO_DEMO_TOKEN = $login.accessToken
    & $demoScript -CarreraId $carreraId -BaseUrl $base | ForEach-Object { Write-Host $_ }
    & $demoScript -CarreraId $carreraId -BaseUrl $base | ForEach-Object { Write-Host $_ }
} finally { $env:CATALOGO_DEMO_TOKEN = $oldToken }
$lista = @(VerificarHttp 'GET' "/api/carreras/$carreraId/asignaturas" $null 200 'listar malla por nivel tras cargar demo dos veces')
if ($lista.Count -ne 2 -or $lista[0].nombre -ne 'Álgebra Lineal' -or $lista[0].nivel -ne 1 -or $lista[0].creditos -ne 3 -or $lista[0].horas -ne 144 -or $lista[1].nivel -ne 2) { throw 'Listado/datos de Álgebra Lineal/orden o idempotencia incorrectos.' }
Write-Output 'PASS: T8 real repetible, sin duplicados; listado ordenado por nivel.'
VerificarHttp 'GET' "/api/asignaturas/$($lista[0].id)" $null 200 'consultar datos de asignatura' | Out-Null
$duplicado = $inputAsignatura.Clone(); $duplicado.codigo = ' demo-alg-lin-001 '
VerificarHttp 'POST' '/api/asignaturas' $duplicado 409 'código normalizado duplicado rechazado' | Out-Null
VerificarHttp 'DELETE' "/api/carreras/$carreraId" $null 409 'carrera relacionada no puede eliminarse' | Out-Null

# Dos solicitudes en vuelo con el mismo código; ninguna credencial se imprime.
$parallelInput = $inputAsignatura.Clone(); $parallelInput.codigo = 'DEMO-CONCURRENTE'
$http = [Net.Http.HttpClient]::new(); $http.Timeout = [TimeSpan]::FromSeconds(30)
$http.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $login.accessToken)
try {
    $payload = $parallelInput | ConvertTo-Json
    $first = $http.PostAsync($base + '/api/asignaturas', [Net.Http.StringContent]::new($payload, [Text.Encoding]::UTF8, 'application/json'))
    $second = $http.PostAsync($base + '/api/asignaturas', [Net.Http.StringContent]::new($payload, [Text.Encoding]::UTF8, 'application/json'))
    $responses = @($first.GetAwaiter().GetResult(), $second.GetAwaiter().GetResult())
    $codes = @($responses | ForEach-Object { [int]$_.StatusCode } | Sort-Object)
    if (($codes -join ',') -ne '201,409') { throw 'La creación concurrente no devolvió exactamente 201 y 409.' }
    foreach ($response in $responses) { $response.Dispose() }
    Write-Output 'PASS: solicitudes concurrentes, una creación y un rechazo de duplicado (201/409).'
} finally { $http.Dispose() }
Write-Output 'VALIDACIÓN HTTP COMPLETA: Auth → Gateway → Catálogo → SQL Server, con datos sintéticos conservados para revisión.'
