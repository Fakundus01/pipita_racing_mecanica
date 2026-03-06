$databaseDir = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'PipitaGarageDesktop'
$databasePath = Join-Path $databaseDir 'pipita-desktop.db'

if (Test-Path $databasePath) {
    Remove-Item -Force $databasePath
    Write-Host "Base local eliminada: $databasePath"
} else {
    Write-Host "No habia base local para eliminar."
}
