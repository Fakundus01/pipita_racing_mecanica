# Guia de uso - Pipita Garage Desktop

## 1. Que es la app
Pipita Garage Desktop es una app de escritorio para administrar el trabajo diario del taller.
Permite cargar clientes, vehiculos, partes, servicios, solicitudes, distribuidoras, reportes y agenda.
Tambien exporta e importa Excel y prepara avisos para clientes por WhatsApp, Gmail o copia de mensaje.

## 2. Instalacion
- Abrir el instalador `PipitaGarageDesktopSetup.exe`.
- Seguir el asistente de instalacion.
- Si se desea, crear acceso directo en el escritorio.
- Abrir la app desde el acceso directo o desde el menu Inicio.

## 3. Donde guarda los datos
La app guarda los datos en una base local SQLite del usuario actual de Windows.
Ruta usada por la app:
- `%LOCALAPPDATA%\PipitaGarageDesktop\pipita-desktop.db`

Ejemplo:
- `C:\Users\NombreDeUsuario\AppData\Local\PipitaGarageDesktop\pipita-desktop.db`

Esto significa que cada PC guarda sus datos localmente sin depender de internet ni de un servidor externo.

## 4. Modulos principales
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

## 5. Avisos a clientes
Desde Agenda detalle se puede usar:
- `WhatsApp`: intenta abrir la app de WhatsApp primero y, si no esta disponible, abre WhatsApp Web.
- `Email`: abre Gmail con asunto y mensaje precargados.
- `Copiar mensaje`: copia el texto para pegarlo manualmente.

Cada aviso queda registrado en la solicitud con:
- tipo de aviso
- canal usado
- fecha y hora del ultimo aviso

## 6. Excel
### Exportar Excel
- Genera un archivo Excel con las hojas del sistema.
- Permite reemplazar un archivo existente o crear uno nuevo.
- Se usa como archivo maestro de trabajo.

### Importar Excel
- Permite traer cambios desde un Excel.
- Tiene modo `Merge recomendado`.
- Tiene modo `Sincronizacion exacta`.

## 7. Respaldo recomendado
Para hacer backup conviene guardar:
- la base local: `%LOCALAPPDATA%\PipitaGarageDesktop\pipita-desktop.db`
- los Excel exportados del taller

## 8. Flujo recomendado de uso
1. Cargar cliente.
2. Cargar vehiculo.
3. Registrar solicitud o cita.
4. Registrar partes y servicios.
5. Si hay terceros, cargar distribuidora y trabajo externo.
6. Usar Agenda para seguir turnos.
7. Avisar al cliente cuando corresponda.
8. Exportar Excel para respaldo o gestion.

## 9. Soporte basico
Si la app no abre o hay que revisar informacion:
- comprobar que la instalacion este completa
- revisar que exista la carpeta `%LOCALAPPDATA%\PipitaGarageDesktop`
- verificar el archivo `pipita-desktop.db`
- usar los Excel exportados como respaldo operativo

## 10. Estado del producto
La app esta pensada para uso local en PC con Windows de 64 bits.
No requiere instalar .NET aparte, porque la publicacion actual ya incluye el runtime necesario.
