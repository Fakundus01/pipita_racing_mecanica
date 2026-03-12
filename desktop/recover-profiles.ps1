param(
    [switch]$ClearPins = $true,
    [switch]$PreferPrincipal = $true
)

$appRoot = Join-Path $env:LOCALAPPDATA "PipitaGarageDesktop"
$statePath = Join-Path $appRoot "profiles.json"

if (-not (Test-Path $statePath)) {
    throw "No se encontro profiles.json en $statePath"
}

$state = Get-Content $statePath -Raw | ConvertFrom-Json

if ($ClearPins -and $state.Profiles) {
    foreach ($profile in $state.Profiles) {
        $profile.PinHash = $null
        $profile.PinSalt = $null
    }
}

if ($PreferPrincipal -and $state.Profiles) {
    $principal = $state.Profiles | Where-Object { $_.Name -eq "Principal" } | Select-Object -First 1
    if ($null -ne $principal) {
        $state.LastProfileId = $principal.Id
    }
    elseif (-not [string]::IsNullOrWhiteSpace($state.Profiles[0].Id)) {
        $state.LastProfileId = $state.Profiles[0].Id
    }
}

foreach ($profile in $state.Profiles) {
    if (-not [string]::IsNullOrWhiteSpace($profile.Slug)) {
        $profileDirectory = Join-Path (Join-Path $appRoot "profiles") $profile.Slug
        New-Item -ItemType Directory -Force -Path $profileDirectory | Out-Null
    }
}

$json = $state | ConvertTo-Json -Depth 8
Set-Content -Path $statePath -Value $json -Encoding UTF8

Write-Output "Recuperacion aplicada."
Write-Output "Archivo actualizado: $statePath"
Write-Output "Perfiles detectados:"
$state.Profiles | ForEach-Object {
    Write-Output ("- {0} | Id={1} | PIN={2}" -f $_.Name, $_.Id, [bool](-not [string]::IsNullOrWhiteSpace($_.PinHash)))
}
