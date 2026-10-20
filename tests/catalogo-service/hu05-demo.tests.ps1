$ErrorActionPreference = 'Stop'
$carga = Join-Path $PSScriptRoot '../../src/catalogo-service/examples/hu-05/cargar-algebra.ps1'
$fixture = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../../src/catalogo-service/examples/hu-05/algebra-lineal.json') -Raw | ConvertFrom-Json
$carreraPrueba = [Guid]'11111111-1111-4111-8111-111111111111'
$tokenAnterior = $env:CATALOGO_DEMO_TOKEN
$demoEstado = @{ Escenario = 'nuevo'; Registros = 0 }

# Doble de transporte HTTP: se ejecuta el script real sin contactar servicios.
function Invoke-RestMethod {
    param($Uri, $Headers, $Method, $ContentType, $Body)
    if ($Headers.Authorization -cne 'Bearer token-sintetico-de-prueba') { throw 'Authorization no coincide con la sesión de prueba.' }
    if ($Method -eq 'Post') {
        $demoEstado.Registros++
        $inputDemo = [System.Text.Encoding]::UTF8.GetString($Body) | ConvertFrom-Json
        if ($inputDemo.nivel -ne 1 -or $inputDemo.creditos -ne 3 -or $inputDemo.horas -ne 144 -or $inputDemo.carreraId -ne $carreraPrueba.ToString()) { throw 'Datos incorrectos en POST.' }
        return @{ id = 'asignatura-sintetica' }
    }
    if ($Uri -like '*/asignaturas') {
        if ($demoEstado.Escenario -eq 'nuevo') { return @() }
        return @(@{ id = 'asignatura-sintetica'; codigo = $fixture.codigo; nombre = $fixture.nombre; nivel = 1; creditos = $(if ($demoEstado.Escenario -eq 'conflicto') { 9 } else { 3 }); horas = 144 })
    }
    return @{ nombre = $(if ($demoEstado.Escenario -eq 'otra-carrera') { 'Otra carrera' } else { 'Software' }) }
}

function DebeFallar($accion, $patron) {
    try { & $accion | Out-Null } catch {
        if ($_.Exception.Message -notlike $patron) { throw }
        return
    }
    throw 'Se esperaba un error y el script continuó.'
}

try {
    $env:CATALOGO_DEMO_TOKEN = 'token-sintetico-de-prueba'
    & $carga -CarreraId $carreraPrueba | Out-Null
    if ($demoEstado.Registros -ne 1) { throw 'Primera carga no creó exactamente una asignatura.' }
    Write-Output 'PASS: primera carga envía Álgebra Lineal con 1 nivel, 3 créditos y 144 horas.'
    $demoEstado.Escenario = 'existente'
    & $carga -CarreraId $carreraPrueba | Out-Null
    if ($demoEstado.Registros -ne 1) { throw 'La segunda carga duplicó la asignatura.' }
    Write-Output 'PASS: repetición no registra duplicados.'
    $demoEstado.Escenario = 'conflicto'
    DebeFallar { & $carga -CarreraId $carreraPrueba } '*no se sobrescribió*'
    Write-Output 'PASS: código existente con otros datos no se sobrescribe.'
    $demoEstado.Escenario = 'otra-carrera'
    DebeFallar { & $carga -CarreraId $carreraPrueba } '*Software existente*'
    Write-Output 'PASS: rechaza una carrera diferente de Software.'
    DebeFallar { & $carga -CarreraId $carreraPrueba -BaseUrl 'https://example.test/otra-ruta' } '*origen HTTP*'
    Write-Output 'PASS: valida el origen HTTP de destino.'
    $env:CATALOGO_DEMO_TOKEN = ''
    DebeFallar { & $carga -CarreraId $carreraPrueba } '*CATALOGO_DEMO_TOKEN*'
    Write-Output 'PASS: exige sesión de administrador para cargar datos.'
    Write-Output '6 pruebas correctas; transporte HTTP simulado, no certifica SQL Server/Gateway.'
} finally {
    $env:CATALOGO_DEMO_TOKEN = $tokenAnterior
}
