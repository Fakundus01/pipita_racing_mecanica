param(
    [string]$ServiceName = 'PipitaSyncApi'
)

$ErrorActionPreference = 'Stop'

if (-not ([bool](net session 2>$null))) {
    throw 'Este script necesita PowerShell ejecutado como administrador.'
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    throw "No existe el servicio $ServiceName."
}

if ($service.Status -ne 'Stopped') {
    Restart-Service -Name $ServiceName -Force
    Write-Host "Servicio reiniciado: $ServiceName"
    exit 0
}

Start-Service -Name $ServiceName
Write-Host "Servicio iniciado: $ServiceName"
