param(
    [string]$ServiceName = 'PipitaSyncApi',
    [string]$DisplayName = 'Pipita Sync API',
    [string]$Description = 'Backend de sincronizacion para Pipita Garage Desktop',
    [switch]$StartNow
)

$ErrorActionPreference = 'Stop'

if (-not ([bool](net session 2>$null))) {
    throw 'Este script necesita PowerShell ejecutado como administrador.'
}

$exePath = Join-Path $PSScriptRoot 'PipitaSyncApi.exe'
if (-not (Test-Path $exePath)) {
    throw "No se encontro el ejecutable del servicio: $exePath"
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    throw "El servicio $ServiceName ya existe. Usa update-service.ps1 o uninstall-service.ps1 primero."
}

$binaryPath = '"' + $exePath + '"'
New-Service -Name $ServiceName -BinaryPathName $binaryPath -DisplayName $DisplayName -Description $Description -StartupType Automatic | Out-Null

Write-Host "Servicio instalado: $ServiceName"
Write-Host 'Antes de iniciarlo, valida appsettings.Production.json y el puerto/firewall.'

if ($StartNow) {
    Start-Service -Name $ServiceName
    Write-Host "Servicio iniciado: $ServiceName"
}
