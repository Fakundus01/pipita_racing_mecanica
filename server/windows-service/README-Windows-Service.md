# Pipita Sync API - Windows Service

## Antes de instalar
1. Extraer este paquete en una carpeta fija, por ejemplo `C:\PipitaSyncApi`.
2. Editar `appsettings.Production.json`.
3. Cambiar estas claves:
   - `Jwt:SigningKey`
   - `Server:PublicBaseUrl`
4. Abrir el puerto en firewall si la app desktop se conecta desde otra PC.

## Instalar servicio
Abrir PowerShell como administrador y ejecutar:
```powershell
powershell -ExecutionPolicy Bypass -File .\install-service.ps1 -StartNow
```

## Reiniciar servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\restart-service.ps1
```

## Ver estado
```powershell
powershell -ExecutionPolicy Bypass -File .\status-service.ps1
```

## Desinstalar
```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall-service.ps1
```

## URL para la app desktop
En `Perfil y sync`, pegar:
- `http://IP-DEL-SERVIDOR:5188`

## Endpoint de salud
- `http://IP-DEL-SERVIDOR:5188/health`

## Abrir firewall
```powershell
powershell -ExecutionPolicy Bypass -File .\open-firewall-port.ps1
```
