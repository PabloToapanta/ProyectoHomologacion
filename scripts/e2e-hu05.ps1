[CmdletBinding()]
param(
    [string]$EnvFile = '.env.hu05-validacion',
    [string]$OutputDirectory = 'docs/sprint-1/evidencias/hu-05-integracion/navegador'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$envPath = [IO.Path]::GetFullPath($EnvFile)
$settings = @{}
foreach ($line in Get-Content -LiteralPath $envPath) {
    if ($line -and -not $line.StartsWith('#')) { $at = $line.IndexOf('='); if ($at -gt 0) { $settings[$line.Substring(0, $at)] = $line.Substring($at + 1) } }
}
if (-not $settings.AUTH_ADMIN_USERNAME -or -not $settings.AUTH_ADMIN_PASSWORD) { throw 'Faltan credenciales de prueba en el archivo privado.' }
$previous = @{}
foreach ($name in @('E2E_ADMIN_USERNAME','E2E_ADMIN_PASSWORD','E2E_BASE_URL','PLAYWRIGHT_BROWSERS_PATH')) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
$frontendRoot = Join-Path $repoRoot 'frontend'
try {
    $env:E2E_ADMIN_USERNAME = $settings.AUTH_ADMIN_USERNAME
    $env:E2E_ADMIN_PASSWORD = $settings.AUTH_ADMIN_PASSWORD
    $env:E2E_BASE_URL = 'http://127.0.0.1:' + $settings.FRONTEND_HTTP_PORT
    $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $frontendRoot 'node_modules/.cache/playwright'
    & node (Join-Path $frontendRoot 'node_modules/@playwright/test/cli.js') test e2e/catalogo.spec.ts --config (Join-Path $frontendRoot 'playwright.config.ts') --output (Join-Path $repoRoot $OutputDirectory)
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas de navegador; consulta la salida anterior.' }
} finally {
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
}
