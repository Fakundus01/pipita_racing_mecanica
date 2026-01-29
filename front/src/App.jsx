import { useMemo, useState } from 'react'
import Home from './pages/Home'
import Clientes from './pages/Clientes'
import Vehiculos from './pages/Vehiculos'
import Partes from './pages/Partes'
import Reportes from './pages/Reportes'
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
  clientes: Clientes,
  vehiculos: Vehiculos,
  partes: Partes,
  reportes: Reportes,
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
          <button className="secondary" onClick={() => setMessage('Estado actualizado.')}
          >
            Actualizar estado
          </button>
        </div>
        <ActiveComponent onAction={setMessage} />
      </main>
    </div>
  )
}

export default App
