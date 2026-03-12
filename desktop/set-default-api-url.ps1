param(
    [Parameter(Mandatory=$true)]
    [string]$ApiBaseUrl
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSCommandPath
$configPath = Join-Path $root 'src\PipitaDesktop\Config\remote-config.json'

$payload = @{
    ApiBaseUrl = $ApiBaseUrl.Trim()
} | ConvertTo-Json

Set-Content -Path $configPath -Value $payload -Encoding UTF8
Write-Host "URL por defecto guardada en: $configPath"
Write-Host "Valor: $ApiBaseUrl"
