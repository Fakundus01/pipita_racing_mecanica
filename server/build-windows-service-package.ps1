param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$ServiceFolderName = 'PipitaSyncApi.WindowsService'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSCommandPath
$project = Join-Path $root 'PipitaSyncApi\PipitaSyncApi.csproj'
$serviceScripts = Join-Path $root 'windows-service'
$outputRoot = Join-Path $root 'dist'
$publishDir = Join-Path $outputRoot $ServiceFolderName
$zipPath = Join-Path $outputRoot ($ServiceFolderName + '.zip')

$env:DOTNET_CLI_HOME = $root
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:APPDATA = Join-Path $root '.appdata'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'

New-Item -ItemType Directory -Force -Path $env:APPDATA, $env:NUGET_PACKAGES, $outputRoot | Out-Null

$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

& $dotnet restore $project
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet publish $project -c $Configuration -r $Runtime --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -Path (Join-Path $serviceScripts '*') -Destination $publishDir -Recurse -Force
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force
Write-Host "Paquete Windows Service generado en: $zipPath"
