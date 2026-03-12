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
    Stop-Service -Name $ServiceName -Force
}

sc.exe delete $ServiceName | Out-Null
Write-Host "Servicio eliminado: $ServiceName"
