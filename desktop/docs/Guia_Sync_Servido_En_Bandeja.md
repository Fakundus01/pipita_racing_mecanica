# Guia simple - como dejarle todo servido al cliente

## Respuesta corta
Si queres que tu cliente haga solo `instalar y usar`, la mejor opcion es esta:
- la app desktop instalada en su PC
- el backend `PipitaSyncApi` corriendo en la nube
- el instalador del desktop ya preparado con la URL del backend

Eso es mejor que usar una PC del taller como servidor.

## Por que la nube es mejor
### Opcion A: servidor en una PC del taller
Ventajas:
- no pagas nube
- sirve si hay una sola red local y una sola sede

Problemas:
- esa PC tiene que estar siempre prendida
- si cambia la IP o se rompe la PC, deja de andar el sync
- hay que abrir firewall y mantener Windows Service
- no es la experiencia mas simple para tu cliente

### Opcion B: backend en la nube
Ventajas:
- el backend queda siempre prendido
- no depende de ninguna PC del taller
- la URL siempre es la misma
- es la opcion mas parecida a `instalar y listo`

Problemas:
- tiene costo si escalas o queres mejor plan
- hay que desplegarlo una vez

## Conclusion
Para tu caso, la mejor opcion es `backend en la nube`.

## Que pasa hoy si solo instalas el desktop
Si tu cliente instala solo `PipitaGarageDesktopSetup.exe`, ya puede usar la app.
Eso significa:
- crea un perfil local
- guarda datos localmente en su PC
- no necesita internet
- no necesita backend

Eso ya es `instalar y usar`.

Lo que NO tiene en ese caso:
- sync entre PCs
- backup remoto
- mover facil el perfil entre computadoras

## Cuando necesitas el backend
Solo si queres esto:
- usar la misma info en mas de una PC
- tener copia remota del perfil
- restaurar un perfil en otra maquina

## Como dejarlo realmente servido en bandeja
### Lo que haces vos una sola vez
1. Desplegas `PipitaSyncApi` en Render.
2. Render te da una URL, por ejemplo:
   - `https://pipita-sync-api.onrender.com`
3. Guardas esa URL en el desktop con este comando:
```powershell
powershell -ExecutionPolicy Bypass -File .\desktop\set-default-api-url.ps1 -ApiBaseUrl "https://pipita-sync-api.onrender.com"
```
4. Regeneras el instalador del desktop.

### Lo que recibe tu cliente
Tu cliente recibe solo:
- `PipitaGarageDesktopSetup.exe`

### Lo que hace tu cliente
1. Instala la app.
2. La abre.
3. Ya tiene la URL del backend precargada.
4. Si quiere usar sync, entra a `Perfil y sync` y se registra o inicia sesion.

## La verdad importante
Si queres `cero configuracion total`, hay dos niveles:

### Nivel 1: cero configuracion local
Eso ya lo tenes hoy.
Tu cliente instala y usa la app local.

### Nivel 2: cero configuracion con sync remoto
Para eso necesitas dos cosas previas hechas por vos:
- backend ya desplegado
- instalador ya regenerado con la URL del backend

Sin esas dos cosas, el cliente no puede tener sync remoto automaticamente.

## Que recomiendo yo
1. Dejar el backend en Render.
2. Poner la URL en el instalador del desktop.
3. Entregar al cliente un solo setup del desktop.
4. Si usas sync, decirle solo:
   - instala
   - entra a `Perfil y sync`
   - registrate una sola vez

## Resumen final
- si queres simplicidad total: desktop local sin sync
- si queres simplicidad + sync real: backend en Render
- mejor opcion para vos: `Render`
