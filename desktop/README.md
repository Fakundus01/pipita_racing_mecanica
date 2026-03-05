# Pipita Desktop (fase 1-2)

Primera migracion a app de escritorio con .NET 8 + WPF.

## Incluye
- App sin login.
- Base local SQLite con EF Core.
- Pantalla de carga para clientes y vehiculos (alta, edicion y eliminacion).
- Estructura lista para agregar exportacion Excel en la siguiente fase.

## Estructura
- `desktop/src/PipitaDesktop`: proyecto WPF.
- `desktop/src/PipitaDesktop/Data`: contexto y configuracion de base local.
- `desktop/src/PipitaDesktop/Models`: entidades del dominio.

## Comandos
Desde la raiz del repo:

```powershell
powershell -ExecutionPolicy Bypass -File .\desktop\dev.ps1 -Task restore
powershell -ExecutionPolicy Bypass -File .\desktop\dev.ps1 -Task build
powershell -ExecutionPolicy Bypass -File .\desktop\dev.ps1 -Task run
```

Para generar `.exe` final:

```powershell
powershell -ExecutionPolicy Bypass -File .\desktop\dev.ps1 -Task publish
```

El ejecutable quedara en:
`desktop/src/PipitaDesktop/bin/Release/net8.0-windows/win-x64/publish/`

## Base de datos
La app crea automaticamente el archivo SQLite en:
`%LOCALAPPDATA%\PipitaGarageDesktop\pipita-desktop.db`
