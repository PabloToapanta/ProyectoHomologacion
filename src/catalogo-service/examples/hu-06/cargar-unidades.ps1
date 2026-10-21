param(
    [Parameter(Mandatory)][Guid]$AsignaturaId,
    [Parameter(Mandatory)][string]$Archivo,
    [string]$BaseUrl = 'http://127.0.0.1:5080'
)
$ErrorActionPreference = 'Stop'
$uri = [Uri]$BaseUrl
if ($uri.Scheme -notin @('http', 'https') -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or $uri.AbsolutePath -ne '/') { throw 'BaseUrl debe ser un origen HTTP(S).' }
if (-not $env:CATALOGO_DEMO_TOKEN) { throw 'Configura CATALOGO_DEMO_TOKEN con un JWT de administrador.' }
$headers = @{ Authorization = "Bearer $env:CATALOGO_DEMO_TOKEN" }
$base = $BaseUrl.TrimEnd('/')
$detalle = Invoke-RestMethod "$base/api/asignaturas/$AsignaturaId" -Headers $headers
if ($detalle.nombre -notmatch '(?i)^[ÁA]lgebra Lineal$' -or $detalle.horas -ne 144 -or $detalle.creditos -ne 3) { throw 'Selecciona Álgebra Lineal de 3 créditos y 144 horas para esta carga de demostración.' }
$unidades = @(Get-Content -LiteralPath $Archivo -Raw -Encoding utf8 | ConvertFrom-Json)
foreach ($unidad in $unidades) {
    $previa = @($detalle.unidades | Where-Object { $_.numero -eq $unidad.numero })
    if ($previa.Count) {
        $temasPrevios = @($previa[0].contenidos | Sort-Object orden | ForEach-Object { $_.tema })
        if ($previa[0].nombre -ne $unidad.nombre -or $previa[0].horas -ne $unidad.horas -or $previa[0].resultadoAprendizaje -ne $unidad.resultadoAprendizaje -or (ConvertTo-Json -InputObject $temasPrevios -Compress) -cne (ConvertTo-Json -InputObject @($unidad.contenidos) -Compress)) { throw "La unidad $($unidad.numero) ya existe con datos distintos. No se sobrescribe." }
        Write-Output "Unidad $($unidad.numero) ya registrada; se conserva."
        continue
    }
    $body = $unidad | ConvertTo-Json -Depth 5
    $null = Invoke-RestMethod "$base/api/asignaturas/$AsignaturaId/unidades" -Method Post -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($body))
    Write-Output "Unidad $($unidad.numero) registrada."
}
