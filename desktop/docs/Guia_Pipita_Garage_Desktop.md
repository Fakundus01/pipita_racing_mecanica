# Guia de uso - Pipita Garage Desktop

## 1. Que es la app
Pipita Garage Desktop es una app de escritorio para administrar el trabajo diario del taller.
Permite cargar clientes, vehiculos, partes, servicios, solicitudes, distribuidoras, reportes y agenda.
Tambien exporta e importa Excel, prepara avisos para clientes por WhatsApp, Gmail o copia de mensaje y ahora maneja perfiles locales con sync remoto opcional.

## 2. Instalacion
- Abrir el instalador `PipitaGarageDesktopSetup.exe`.
- Seguir el asistente de instalacion.
- Si se desea, crear acceso directo en el escritorio.
- Abrir la app desde el acceso directo o desde el menu Inicio.

## 3. Donde guarda los datos
La app guarda los datos en una base local SQLite del usuario actual de Windows.
Ahora cada perfil tiene su propia base dentro de la carpeta de perfiles.

Rutas usadas por la app:
- estado de perfiles: `%LOCALAPPDATA%\PipitaGarageDesktop\profiles.json`
- bases por perfil: `%LOCALAPPDATA%\PipitaGarageDesktop\profiles\NOMBRE-DEL-PERFIL\pipita-desktop.db`

Ejemplo:
- `C:\Users\NombreDeUsuario\AppData\Local\PipitaGarageDesktop\profiles\principal\pipita-desktop.db`

Esto significa que cada PC guarda sus datos localmente sin depender de internet ni de un servidor externo.
Si se usa sync remoto, la app puede subir y bajar snapshots del perfil mediante la API de sincronizacion.

## 4. Perfiles locales
- Al abrir la app se elige un perfil local.
- Cada perfil tiene su propia base de datos.
- Se puede crear, renombrar y eliminar perfiles.
- Un perfil puede tener PIN.
- Desde `Perfil y sync` se puede:
  - abrir carpeta del perfil
  - abrir carpeta de backups
  - crear backup ZIP
  - restaurar un backup ZIP
  - cambiar al perfil seleccionado reiniciando la app

## 5. Sync remoto opcional
- Desde `Perfil y sync` se puede configurar la URL de la API.
- Se puede registrar o iniciar sesion con email y password.
- Cada perfil local puede vincularse a un perfil remoto.
- El sync remoto trabaja por `snapshot`:
  - `Subir snapshot`: sube un backup ZIP del perfil local
  - `Descargar snapshot`: baja el ultimo backup ZIP remoto y lo restaura localmente
- Esto sirve para mover un perfil entre PCs o tener una copia remota del taller.
- Si el backend esta instalado como servicio de Windows, la URL a usar en la app suele ser:
  - `http://IP-DEL-SERVIDOR:5188`
  - `http://NOMBRE-DEL-EQUIPO:5188`

## 6. Modulos principales
### Clientes
- Alta, edicion y baja de clientes.
- Carga de telefono y email.
- Historial del cliente dentro de la misma app.

### Vehiculos
- Alta, edicion y baja de autos.
- Relacion con el cliente.
- Patente, marca, modelo, version, anio y estado.

### Partes
- Registro de partes y repuestos.
- Stock, costo y observaciones.
- Campo util para trabajos como cambio de gomas.

### Servicios
- Registro de servicios hechos al vehiculo.
- Relacion con cliente y vehiculo.
- Fecha, descripcion, costo y notas.

### Solicitudes
- Vista principal para cargar citas o pedidos del cliente.
- Fecha, hora, duracion, estado, prioridad y notas.
- Validacion de choques de horario.

### Distribuidoras
- Registro de terceros o proveedores externos.
- Ejemplos: tornero, lava autos, alineador.
- Seguimiento de gastos por distribuidora.

### Reportes
- Carga de reportes internos del taller.
- Seguimiento por periodo.

### Agenda
- Calendario mensual.
- Colores por estado dominante del dia.
- Resumen visual del mes.

### Agenda detalle
- Vista del dia seleccionado.
- Lista de citas del dia.
- Vista semanal por hora.
- Botones para avisar al cliente.

### Dashboard
- Resumen general del taller.
- Metricas operativas principales.

## 7. Avisos a clientes
Desde Agenda detalle se puede usar:
- `WhatsApp`: intenta abrir la app de WhatsApp primero y, si no esta disponible, abre WhatsApp Web.
- `Email`: abre Gmail con asunto y mensaje precargados.
- `Copiar mensaje`: copia el texto para pegarlo manualmente.

Cada aviso queda registrado en la solicitud con:
- tipo de aviso
- canal usado
- fecha y hora del ultimo aviso

## 8. Excel
### Exportar Excel
- Genera un archivo Excel con las hojas del sistema.
- Permite reemplazar un archivo existente o crear uno nuevo.
- Se usa como archivo maestro de trabajo.

### Importar Excel
- Permite traer cambios desde un Excel.
- Tiene modo `Merge recomendado`.
- Tiene modo `Sincronizacion exacta`.

## 9. Respaldo recomendado
Para hacer backup conviene guardar:
- la carpeta de perfiles: `%LOCALAPPDATA%\PipitaGarageDesktop\profiles`
- el archivo de estado: `%LOCALAPPDATA%\PipitaGarageDesktop\profiles.json`
- los Excel exportados del taller
- si se usa sync remoto, tambien validar que la API este accesible

## 10. Flujo recomendado de uso
1. Crear o elegir perfil local.
2. Opcional: poner PIN al perfil.
3. Cargar cliente.
4. Cargar vehiculo.
5. Registrar solicitud o cita.
6. Registrar partes y servicios.
7. Si hay terceros, cargar distribuidora y trabajo externo.
8. Usar Agenda para seguir turnos.
9. Avisar al cliente cuando corresponda.
10. Exportar Excel para respaldo o gestion.
11. Si hace falta mover la info a otra PC, usar `Perfil y sync`.

## 11. Soporte basico
Si la app no abre o hay que revisar informacion:
- comprobar que la instalacion este completa
- revisar que exista la carpeta `%LOCALAPPDATA%\PipitaGarageDesktop`
- verificar `profiles.json`
- verificar la carpeta del perfil activo y su `pipita-desktop.db`
- usar los Excel exportados como respaldo operativo
- si se usa sync remoto, probar la URL de la API desde `Perfil y sync`

## 12. Estado del producto
La app esta pensada para uso local en PC con Windows de 64 bits.
No requiere instalar .NET aparte, porque la publicacion actual ya incluye el runtime necesario.

