param(
    [string]$PublishDir = 'server\dist',
    [string]$OutputZip = 'server\dist\PipitaSyncApi.zip'
)

$root = Resolve-Path '.'
$publishPath = Join-Path $root $PublishDir
if (-not (Test-Path $publishPath)) {
    throw "No se encontro la carpeta publish: $publishPath"
}

$zipPath = Join-Path $root $OutputZip
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $publishPath '*') -DestinationPath $zipPath -Force
Write-Host "ZIP generado en: $zipPath"
