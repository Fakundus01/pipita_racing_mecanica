import { useEffect, useMemo, useState } from 'react'
import Home from './pages/Home'
import Clientes from './pages/Clientes'
import Vehiculos from './pages/Vehiculos'
import Taller from './pages/Taller'
import CatalogoVehiculos from './pages/CatalogoVehiculos'
import Partes from './pages/Partes'
import Reportes from './pages/Reportes'
import Login from './components/Login'
import { getSession, login, logout } from './services/api'
import './App.css'

const navigation = [
  { key: 'home', label: 'Panel', description: 'Resumen general' },
  { key: 'clientes', label: 'Clientes', description: 'Contactos y seguimiento' },
  { key: 'vehiculos', label: 'Vehículos', description: 'Autos, camionetas, motos' },
  { key: 'taller', label: 'Tareas de taller', description: 'Cambios y servicios del auto' },
  { key: 'catalogo', label: 'Catálogo de vehículos', description: 'Marcas, modelos y versiones' },
  { key: 'partes', label: 'Partes & accesorios', description: 'Inventario de repuestos' },
  { key: 'reportes', label: 'Reportes', description: 'Resumen mensual' },
]

const views = {
  home: Home,
  clientes: Clientes,
  vehiculos: Vehiculos,
  taller: Taller,
  catalogo: CatalogoVehiculos,
  partes: Partes,
  reportes: Reportes,
}

function App() {
  const [activeView, setActiveView] = useState('home')
  const [message, setMessage] = useState('Listo para registrar una nueva venta.')
  const [auth, setAuth] = useState({ loading: true, authenticated: false })
  const [authError, setAuthError] = useState('')

  const ActiveComponent = useMemo(() => views[activeView], [activeView])
  const activeNav = navigation.find((item) => item.key === activeView)

  useEffect(() => {
    let cancelled = false
    const loadSession = async () => {
      try {
        const data = await getSession()
        if (!cancelled) {
          setAuth({ loading: false, authenticated: data.authenticated })
        }
      } catch {
        if (!cancelled) {
          setAuth({ loading: false, authenticated: false })
        }
      }
    }
    loadSession()
    return () => {
      cancelled = true
    }
  }, [])

  const handleLogin = async (payload) => {
    setAuthError('')
    setAuth((prev) => ({ ...prev, loading: true }))
    try {
      await login(payload)
      setAuth({ loading: false, authenticated: true })
      setMessage('Sesión iniciada. Bienvenido/a.')
    } catch (error) {
      setAuth({ loading: false, authenticated: false })
      setAuthError(error.message)
    }
  }

  const handleLogout = async () => {
    try {
      await logout()
    } catch {
      // Ignorar errores de cierre de sesión
    }
    setAuth({ loading: false, authenticated: false })
    setMessage('Sesión cerrada.')
  }

  const handleAuthError = () => {
    setAuth({ loading: false, authenticated: false })
    setMessage('Sesión expirada. Ingresa nuevamente.')
  }

  if (auth.loading) {
    return (
      <div className="auth-shell">
        <p>Verificando sesión...</p>
      </div>
    )
  }

  if (!auth.authenticated) {
    return (
      <Login onSubmit={handleLogin} error={authError} loading={auth.loading} />
    )
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand-icon">🏁</span>
          <div>
            <p className="brand-title">Pipita Garage</p>
            <p className="brand-subtitle">Gestión de taller</p>
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
          <h3>Gestión rápida</h3>
          <p>
            Acceso directo para cargar tareas del taller y cambios por vehículo.
          </p>
          <ul>
            <li>Clientes y vehículos</li>
            <li>Tareas del taller</li>
            <li>Cambios por vehículo</li>
          </ul>
          <button className="secondary" onClick={() => { setActiveView('taller'); setMessage('Abrimos tareas y cambios de taller.') }}
          >
            Ir a tareas
          </button>
        </div>
        <button className="secondary" onClick={handleLogout}>
          Cerrar sesión
        </button>
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
        <ActiveComponent onAction={setMessage} onAuthError={handleAuthError} onNavigate={setActiveView} />
      </main>
    </div>
  )
}

export default App