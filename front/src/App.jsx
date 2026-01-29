import { useMemo, useState } from 'react'
import Home from './pages/Home'
import './App.css'

const navigation = [
{ key: 'home', label: 'Panel', description: 'Resumen general' },
{ key: 'clientes', label: 'Clientes', description: 'Contactos y seguimiento' },
{ key: 'vehiculos', label: 'Vehículos', description: 'Autos, camionetas, motos' },
{ key: 'partes', label: 'Partes & accesorios', description: 'Inventario de repuestos' },
{ key: 'reportes', label: 'Reportes', description: 'Resumen mensual' },
]

const views = {
  home: Home,
  clientes: ({ onAction }) => (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Clientes activos</h1>
          <p>Gestiona datos, estado y preferencias de tus compradores.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nuevo cliente creado.')}>Nuevo cliente</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Pipeline de clientes</h3>
          <p>3 contactos en seguimiento, 2 listos para cerrar.</p>
          <button className="secondary" onClick={() => onAction('Vista de seguimiento abierta.')}>Ver seguimiento</button>
        </article>
        <article className="page-card">
          <h3>Preferencias guardadas</h3>
          <p>Combustible, precio objetivo y marcas favoritas por cliente.</p>
          <button className="secondary" onClick={() => onAction('Preferencias actualizadas.')}>Actualizar preferencias</button>
        </article>
      </div>
    </section>
  ),
  vehiculos: ({ onAction }) => (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Vehículos en catálogo</h1>
          <p>Organiza inventario y carga fichas con datos de API.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nueva ficha de vehículo iniciada.')}>Nueva ficha</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Sincronizar API</h3>
          <p>Autocompleta modelos, versiones y valores estimados.</p>
          <button className="secondary" onClick={() => onAction('Sincronización programada.')}>Sincronizar</button>
        </article>
        <article className="page-card">
          <h3>Checklist de revisión</h3>
          <p>Estado mecánico, estética, documentación y fotos.</p>
          <button className="secondary" onClick={() => onAction('Checklist actualizado.')}>Editar checklist</button>
        </article>
      </div>
      </section>
  ),
  partes: ({ onAction }) => (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Partes & accesorios</h1>
          <p>Controla stock, costos y partes vinculadas a cada venta.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nuevo repuesto agregado.')}>Agregar repuesto</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Inventario crítico</h3>
          <p>2 piezas por debajo del stock mínimo.</p>
          <button className="secondary" onClick={() => onAction('Alerta de stock enviada.')}>Reponer stock</button>
        </article>
        <article className="page-card">
          <h3>Paquetes destacados</h3>
          <p>Kits de mantenimiento listos para imprimir.</p>
          <button className="secondary" onClick={() => onAction('Paquetes actualizados.')}>Editar paquetes</button>
        </article>
       </div>
        </section>
  ),
  reportes: ({ onAction }) => (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Reportes y planillas</h1>
          <p>Genera PDFs listos para imprimir y compartir.</p>
        </div>
        <button className="primary" onClick={() => onAction('Reporte mensual generado.')}>Generar reporte</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Resumen mensual</h3>
          <p>Ventas, ingresos y vehículos más consultados.</p>
          <button className="secondary" onClick={() => onAction('Resumen descargado.')}>Descargar resumen</button>
        </article>
        <article className="page-card">
          <h3>Planillas personalizadas</h3>
          <p>Crea formatos distintos para autos, motos o partes.</p>
          <button className="secondary" onClick={() => onAction('Plantilla guardada.')}>Editar plantilla</button>
        </article>
      </div>
    </section>
  ),
}
function App() {
  const [activeView, setActiveView] = useState('home')
  const [message, setMessage] = useState('Listo para registrar una nueva venta.')

  const ActiveComponent = useMemo(() => views[activeView], [activeView])
  const activeNav = navigation.find((item) => item.key === activeView)

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-icon">🏁</span>
          <div>
            <p className="brand-title">Pipita Garage</p>
            <p className="brand-subtitle">Anotaciones de ventas</p>
          </div>
        </div>
        <nav className="nav">
          {navigation.map((item) => (
            <button
              key={item.key}
              className={`nav-item ${activeView === item.key ? 'active' : ''}`}
              onClick={() => setActiveView(item.key)}
            >
              <span>{item.label}</span>
              <span className="nav-detail">{item.description}</span>
            </button>
          ))}
        </nav>
        <div className="sidebar-card">
          <h3>API rápida</h3>
          <p>
            Conecta una API de autos para autocompletar marca, modelo, versión y
            precio sugerido al escribir.
          </p>
          <ul>
            <li>Autocompletar VIN/Patente</li>
            <li>Lista de modelos por marca</li>
            <li>Historial de mantenimiento</li>
          </ul>
          <button className="secondary" onClick={() => setMessage('Abrimos la configuración de API.')}
          >
            Configurar API
          </button>
        </div>
      </aside>

      <main className="main">
        <div className="status-banner">
          <div>
            <p className="status-title">{activeNav?.label}</p>
            <p className="status-message">{message}</p>
          </div>
          <button className="secondary" onClick={() => setMessage('Estado actualizado.')}>
            Actualizar estado
          </button>
        </div>
        <ActiveComponent onAction={setMessage} />
      </main>
    </div>
  )
}

export default App
