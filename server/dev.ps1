param(
    [ValidateSet('restore', 'build', 'run', 'publish')]
    [string]$Task = 'run'
)

$root = Split-Path -Parent $PSCommandPath
$project = Join-Path $root 'PipitaSyncApi\PipitaSyncApi.csproj'

$env:DOTNET_CLI_HOME = $root
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:APPDATA = Join-Path $root '.appdata'
$env:NUGET_PACKAGES = Join-Path $root '.nuget\packages'

New-Item -ItemType Directory -Force -Path $env:APPDATA, $env:NUGET_PACKAGES | Out-Null

$dotnet = Join-Path $env:USERPROFILE '.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) {
    $dotnet = 'dotnet'
}

switch ($Task) {
    'restore' {
        & $dotnet restore $project
    }
    'build' {
        & $dotnet restore $project
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $dotnet build $project -c Debug --no-restore
    }
    'run' {
        & $dotnet restore $project
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $dotnet run --project $project --launch-profile http
    }
    'publish' {
        & $dotnet restore $project
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $dotnet publish $project -c Release -o (Join-Path $root 'dist')
    }
}

exit $LASTEXITCODE
