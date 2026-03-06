param(
    [string]$PublishDir = "desktop\src\PipitaDesktop\bin\Release\net8.0-windows\win-x64\publish",
    [string]$OutputDir = "desktop\dist",
    [string]$AppFolderName = "Pipita Garage Desktop"
)

$root = Resolve-Path "."
$publishPath = Join-Path $root $PublishDir
if (-not (Test-Path $publishPath)) {
    throw "No se encontro la carpeta publish: $publishPath"
}

$outputPath = Join-Path $root $OutputDir
$appPath = Join-Path $outputPath $AppFolderName
$zipPath = Join-Path $outputPath "PipitaGarageDesktop.zip"

Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $appPath, $zipPath
New-Item -ItemType Directory -Force -Path $appPath | Out-Null
Copy-Item -Path (Join-Path $publishPath '*') -Destination $appPath -Recurse -Force

$launcher = @"
@echo off
set APP_DIR=%~dp0
start "" "%APP_DIR%PipitaDesktop.exe"
"@
Set-Content -Path (Join-Path $appPath 'Abrir Pipita Garage.cmd') -Value $launcher -Encoding ASCII

$shortcutScript = @"
`$shell = New-Object -ComObject WScript.Shell
`$desktop = [Environment]::GetFolderPath('Desktop')
`$shortcutPath = Join-Path `$desktop 'Pipita Garage Desktop.lnk'
`$targetPath = Join-Path `$PSScriptRoot 'PipitaDesktop.exe'
`$shortcut = `$shell.CreateShortcut(`$shortcutPath)
`$shortcut.TargetPath = `$targetPath
`$shortcut.WorkingDirectory = `$PSScriptRoot
`$shortcut.IconLocation = `$targetPath
`$shortcut.Save()
Write-Host 'Acceso directo creado en el escritorio:' `$shortcutPath
"@
Set-Content -Path (Join-Path $appPath 'Crear acceso directo.ps1') -Value $shortcutScript -Encoding UTF8

$readme = @"
Pipita Garage Desktop

1. Extraer este ZIP en una carpeta fija.
2. Abrir 'Abrir Pipita Garage.cmd' para iniciar la app.
3. Si se quiere acceso directo en el escritorio, ejecutar 'Crear acceso directo.ps1'.

Nota:
- Este paquete publicado ya incluye el runtime de .NET; no hace falta instalar .NET en la PC del cliente.
- El acceso directo se crea despues de extraer, porque un .lnk dentro del ZIP no conoce la ruta final del usuario.
- El acceso directo toma automaticamente el icono del PipitaDesktop.exe publicado.
"@
Set-Content -Path (Join-Path $appPath 'LEEME.txt') -Value $readme -Encoding UTF8

Compress-Archive -Path $appPath -DestinationPath $zipPath -Force
Write-Host "ZIP generado en: $zipPath"
