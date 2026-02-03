import { useEffect, useState } from 'react'
import {
  createCliente,
  listCatalogoAnios,
  listCatalogoMarcas,
  listCatalogoModelos,
  listCatalogoVersiones,
  listClientes,
} from '../services/api'

function Clientes({ onAction, onAuthError }) {
  const [clientes, setClientes] = useState([])
  const [form, setForm] = useState({
    nombre: '',
    telefono: '',
    email: '',
    patente: '',
    marca: '',
    modelo: '',
    version: '',
    anio: '',
  })
  const [catalogoMarcas, setCatalogoMarcas] = useState([])
  const [catalogoModelos, setCatalogoModelos] = useState([])
  const [catalogoVersiones, setCatalogoVersiones] = useState([])
  const [catalogoAnios, setCatalogoAnios] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    const loadClientes = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await listClientes()
        if (!cancelled) {
          setClientes(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }
    loadClientes()
    return () => {
      cancelled = true
    }
  }, [onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadCatalogo = async () => {
      try {
        const [marcasData, aniosData] = await Promise.all([
          listCatalogoMarcas(),
          listCatalogoAnios(),
        ])
        if (!cancelled) {
          setCatalogoMarcas(marcasData.marcas || [])
          setCatalogoAnios(aniosData.anios || [])
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      }
    }
    loadCatalogo()
    return () => {
      cancelled = true
    }
  }, [onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadModelos = async () => {
      if (!form.marca) {
        setCatalogoModelos([])
        setCatalogoVersiones([])
        return
      }
      try {
        const data = await listCatalogoModelos(form.marca)
        if (!cancelled) {
          setCatalogoModelos(data.modelos || [])
          setForm((prev) => ({ ...prev, modelo: '', version: '' }))
          setCatalogoVersiones([])
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      }
    }
    loadModelos()
    return () => {
      cancelled = true
    }
  }, [form.marca, onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadVersiones = async () => {
      if (!form.marca || !form.modelo) {
        setCatalogoVersiones([])
        return
      }
      try {
        const data = await listCatalogoVersiones(form.marca, form.modelo)
        if (!cancelled) {
          setCatalogoVersiones(data.versiones || [])
          setForm((prev) => ({ ...prev, version: '' }))
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      }
    }
    loadVersiones()
    return () => {
      cancelled = true
    }
  }, [form.marca, form.modelo, onAuthError])

  const handleChange = (event) => {
    const { name, value } = event.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      const payload = {
        nombre: form.nombre,
        telefono: form.telefono,
        email: form.email,
        vehiculo: form.marca && form.modelo
          ? {
            patente: form.patente || null,
            marca: form.marca,
            modelo: form.modelo,
            version: form.version || null,
            anio: form.anio ? Number(form.anio) : null,
          }
          : null,
      }
      const nuevo = await createCliente(payload)
      setClientes((prev) => [nuevo, ...prev])
       setForm({
        nombre: '',
        telefono: '',
        email: '',
        patente: '',
        marca: '',
        modelo: '',
        version: '',
        anio: '',
      })
      onAction(`Cliente ${nuevo.nombre} creado.`)
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Clientes activos</h1>
          <p>Gestiona datos, estado y preferencias de tus compradores.</p>
        </div>
        <button className="primary" onClick={() => onAction('Completa el formulario para crear un cliente.')}>
          Nuevo cliente
        </button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Registrar cliente</h3>
          <form className="mini-form" onSubmit={handleSubmit}>
            <input
              name="nombre"
              value={form.nombre}
              onChange={handleChange}
              placeholder="Nombre"
              required
            />
            <input
              name="telefono"
              value={form.telefono}
              onChange={handleChange}
              placeholder="Teléfono"
            />
            <input
              name="email"
              value={form.email}
              onChange={handleChange}
              placeholder="Email"
            />
            <input
              name="patente"
              value={form.patente}
              onChange={handleChange}
              placeholder="Patente"
            />
            <select
              name="marca"
              value={form.marca}
              onChange={handleChange}
            >
              <option value="">Selecciona marca</option>
              {catalogoMarcas.map((marca) => (
                <option key={marca} value={marca}>{marca}</option>
              ))}
            </select>
            <select
              name="modelo"
              value={form.modelo}
              onChange={handleChange}
              disabled={!form.marca}
            >
              <option value="">Selecciona modelo</option>
              {catalogoModelos.map((modelo) => (
                <option key={modelo} value={modelo}>{modelo}</option>
              ))}
            </select>
            <select
              name="version"
              value={form.version}
              onChange={handleChange}
              disabled={!form.modelo}
            >
              <option value="">Selecciona versión</option>
              {catalogoVersiones.map((version) => (
                <option key={version} value={version}>{version}</option>
              ))}
            </select>
            <select
              name="anio"
              value={form.anio}
              onChange={handleChange}
            >
              <option value="">Selecciona año</option>
              {catalogoAnios.map((anio) => (
                <option key={anio} value={anio}>{anio}</option>
              ))}
            </select>
            <button className="secondary" type="submit">Guardar</button>
          </form>
          {error ? <p className="inline-error">{error}</p> : null}
        </article>
        <article className="page-card">
          <h3>Listado actualizado</h3>
          {loading ? <p>Cargando clientes...</p> : null}
          {!loading && clientes.length === 0 ? (
            <p>No hay clientes registrados.</p>
          ) : (
            <ul className="data-list">
              {clientes.map((cliente) => (
                <li key={cliente.id}>
                  <div>
                    <strong>{cliente.nombre}</strong>
                    <span>{cliente.email || 'Sin email'}</span>
                  </div>
                  <div>
                    <span>{cliente.telefono || 'Sin teléfono'}</span>
                    <span>
                      {cliente.vehiculos?.length
                        ? `${cliente.vehiculos[0].patente || 'Sin patente'} · ${cliente.vehiculos[0].marca} ${cliente.vehiculos[0].modelo}`
                        : 'Sin vehículo asociado'}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>
    </section>
  )
}

export default Clientes