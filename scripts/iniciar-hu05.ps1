[CmdletBinding()]
param([switch]$SoloPreparar)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$envPath = Join-Path $repoRoot '.env.hu05-validacion'
Push-Location $repoRoot
try {
    if (-not (Test-Path -LiteralPath $envPath)) {
        & git check-ignore --quiet -- $envPath
        if ($LASTEXITCODE -ne 0) { throw 'La configuración privada no está excluida de Git; no se generaron secretos.' }
        $sqlKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(20)) + 'aA1!'
        $jwtKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
        $adminKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(18)) + 'aA1!'
        @(
            'AUTH_SQL_PASSWORD=' + $sqlKey
            'CATALOGO_SQL_PASSWORD=' + $sqlKey
            'AUTH_JWT_ISSUER=homologacion-validacion'
            'AUTH_JWT_AUDIENCE=homologacion-validacion'
            'AUTH_JWT_KEY=' + $jwtKey
            'JWT_ISSUER=homologacion-validacion'
            'JWT_AUDIENCE=homologacion-validacion'
            'JWT_SIGNING_KEY=' + $jwtKey
            'AUTH_ADMIN_USERNAME=admin-validacion-hu05'
            'AUTH_ADMIN_PASSWORD=' + $adminKey
            'CATALOGO_SERVICE_URL=http://catalogo-api:8080/'
            'HOMOLOGACION_SERVICE_URL=http://homologacion-service:8080/'
            'AUTH_HTTP_PORT=5081'
            'GATEWAY_HTTP_PORT=5080'
            'FRONTEND_HTTP_PORT=5173'
        ) | Set-Content -LiteralPath $envPath -Encoding utf8
    }
    & docker compose --env-file $envPath -f compose.hu05.yaml -p hu05-validacion config --quiet
    if ($LASTEXITCODE -ne 0) { throw 'Configuración de Compose inválida.' }
    Write-Output 'Configuración privada conservada y Compose válido; no se muestran credenciales.'
    if (-not $SoloPreparar) {
        & docker compose --env-file $envPath -f compose.hu05.yaml -p hu05-validacion up -d --build --wait --wait-timeout 240
        if ($LASTEXITCODE -ne 0) { throw 'No se completó el arranque. Revisa Docker y el estado de los servicios.' }
        Write-Output 'Frontend disponible en http://127.0.0.1:5173. Credenciales en el archivo privado .env.hu05-validacion.'
    }
} finally { Pop-Location }
