param(
    [int]$Port = 5188,
    [string]$RuleName = 'Pipita Sync API 5188'
)

$ErrorActionPreference = 'Stop'

if (-not ([bool](net session 2>$null))) {
    throw 'Este script necesita PowerShell ejecutado como administrador.'
}

$existing = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "La regla ya existe: $RuleName"
    exit 0
}

New-NetFirewallRule -DisplayName $RuleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port | Out-Null
Write-Host "Regla de firewall creada para TCP $Port"
