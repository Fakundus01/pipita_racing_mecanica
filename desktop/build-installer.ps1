param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSCommandPath
$issPath = Join-Path $root 'installer\PipitaGarageDesktop.iss'
$publishScript = Join-Path $root 'dev.ps1'

$candidatePaths = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:APPDATA 'Inno Setup 6\ISCC.exe')
) | Where-Object { $_ }

$innoCandidates = @($candidatePaths | Where-Object { Test-Path $_ })

if (-not $innoCandidates) {
    $searchRoots = @($env:LOCALAPPDATA, $env:APPDATA) | Where-Object { $_ -and (Test-Path $_) }
    foreach ($searchRoot in $searchRoots) {
        $found = Get-ChildItem -Path $searchRoot -Filter ISCC.exe -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
        if ($found) {
            $innoCandidates += $found
            break
        }
    }
}

if (-not (Test-Path $issPath)) {
    throw "No se encontro el script del instalador: $issPath"
}

powershell -ExecutionPolicy Bypass -File $publishScript -Task publish
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $innoCandidates) {
    throw "No se encontro Inno Setup. Instala Inno Setup 6 y volve a ejecutar este script."
}

$iscc = $innoCandidates[0]
Write-Host "Usando Inno Setup en: $iscc"
& $iscc $issPath
exit $LASTEXITCODE
