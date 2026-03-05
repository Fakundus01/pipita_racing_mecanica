# Pipita Desktop (fase 1-5)

Migracion a app de escritorio con .NET 8 + WPF.

## Incluye
- App sin login.
- Base local SQLite con EF Core.
- Pantallas de carga para clientes, vehiculos, partes, servicios y reportes (alta, edicion y eliminacion).
- Exportacion a Excel profesional (hoja resumen + tablas por modulo con estilo, filtros y formatos).
- Importacion de datos legacy desde una DB SQLite existente (reemplaza datos actuales).
- Filtros rapidos en todas las grillas (texto, estado, cliente y rangos de fechas segun modulo).

## Estructura
- `desktop/src/PipitaDesktop`: proyecto WPF.
- `desktop/src/PipitaDesktop/Data`: contexto y configuracion de base local.
- `desktop/src/PipitaDesktop/Models`: entidades del dominio.
- `desktop/src/PipitaDesktop/Services/ExcelExportService.cs`: generacion de Excel.
- `desktop/src/PipitaDesktop/Services/LegacyImportService.cs`: importacion de base legacy.

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

El ejecutable queda en:
`desktop/src/PipitaDesktop/bin/Release/net8.0-windows/win-x64/publish/`

## Base de datos
La app crea automaticamente el archivo SQLite en:
`%LOCALAPPDATA%\PipitaGarageDesktop\pipita-desktop.db`
