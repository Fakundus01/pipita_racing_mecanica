param(
    [string]$ServiceName = 'PipitaSyncApi'
)

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    throw "No existe el servicio $ServiceName."
}

Get-Service -Name $ServiceName | Select-Object Name, Status, StartType
