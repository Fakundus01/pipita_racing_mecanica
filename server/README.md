# Pipita Sync API

Backend ASP.NET Core para login remoto y sync de perfiles por snapshot.

## Que hace
- registro e inicio de sesion con JWT
- perfiles remotos por usuario
- upload de snapshot ZIP por perfil
- descarga del ultimo snapshot ZIP por perfil
- `healthcheck` para validar si el servicio esta vivo
- soporte para correr como servicio de Windows

## Datos
La API guarda su informacion en:
- `server/PipitaSyncApi/App_Data/pipita-sync.db`

## Desarrollo
### Ejecutar en desarrollo
```powershell
powershell -ExecutionPolicy Bypass -File .\server\dev.ps1 -Task run
```

### Compilar
```powershell
powershell -ExecutionPolicy Bypass -File .\server\dev.ps1 -Task build
```

### Publicar release
```powershell
powershell -ExecutionPolicy Bypass -File .\server\dev.ps1 -Task publish
```

## Windows Service
### Generar paquete listo para servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\server\build-windows-service-package.ps1
```

Eso genera:
- `server/dist/PipitaSyncApi.WindowsService.zip`

### Instalar en una PC o servidor Windows
1. Extraer `PipitaSyncApi.WindowsService.zip` en una carpeta fija.
2. Editar `appsettings.Production.json`.
3. Cambiar:
   - `Jwt:SigningKey`
   - `Server:PublicBaseUrl`
4. Ejecutar PowerShell como administrador.
5. Instalar el servicio:
```powershell
powershell -ExecutionPolicy Bypass -File .\install-service.ps1 -StartNow
```

### Reiniciar el servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\restart-service.ps1
```

### Ver estado del servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\status-service.ps1
```

### Desinstalar el servicio
```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall-service.ps1
```

## URL para la app desktop
En `Perfil y sync`, usar una URL accesible desde la PC cliente, por ejemplo:
- `http://IP-DEL-SERVIDOR:5188`
- `http://NOMBRE-DEL-EQUIPO:5188`

## Endpoints principales
- `GET /`
- `GET /health`
- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/profiles`
- `POST /api/profiles`
- `PUT /api/profiles/{id}`
- `DELETE /api/profiles/{id}`
- `GET /api/profiles/{id}/snapshot/meta`
- `POST /api/profiles/{id}/snapshot/upload`
- `GET /api/profiles/{id}/snapshot/download`

## Nota
La clave JWT de `appsettings.json` es solo para desarrollo local.
Antes de exponer esto fuera de tu PC, cambiala en `appsettings.Production.json`.
