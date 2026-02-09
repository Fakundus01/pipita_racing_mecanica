import { useEffect, useMemo, useState } from 'react'
import {
  createCliente,
  listCatalogoAnios,
  listCatalogoMarcas,
  listCatalogoModelos,
  listCatalogoVersiones,
  listClientes,
} from '../services/api'

const initialForm = {
  nombre: '',
  telefono: '',
  email: '',
  patente: '',
  marca: '',
  modelo: '',
  version: '',
  anio: '',
}

function Clientes({ onAction, onAuthError }) {
  const [clientes, setClientes] = useState([])
  const [form, setForm] = useState(initialForm)
  const [showModal, setShowModal] = useState(false)
  const [catalogoMarcas, setCatalogoMarcas] = useState([])
  const [catalogoModelos, setCatalogoModelos] = useState([])
  const [catalogoVersiones, setCatalogoVersiones] = useState([])
  const [catalogoAnios, setCatalogoAnios] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const clientesOrdenados = useMemo(
    () => [...clientes].sort((a, b) => a.nombre.localeCompare(b.nombre)),
    [clientes],
  )

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

  const closeModal = () => {
    setShowModal(false)
    setForm(initialForm)
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
      closeModal()
      onAction(`Cliente ${nuevo.nombre} creado con su vehículo.`)
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
          <p>Alta rápida con popup y detalle de historial de vehículos.</p>
        </div>
        <button className="primary" onClick={() => setShowModal(true)}>
          Cargar cliente
        </button>
      </header>
      
      {error ? <p className="inline-error">{error}</p> : null}

      <article className="page-card">
        <h3>Historial de vehículos por cliente</h3>
        {loading ? <p>Cargando clientes...</p> : null}
        {!loading && clientesOrdenados.length === 0 ? (
          <p>No hay clientes registrados.</p>
        ) : (
          <ul className="data-list data-list-stacked">
            {clientesOrdenados.map((cliente) => (
              <li key={cliente.id}>
                <div>
                  <strong>{cliente.nombre}</strong>
                  <span>{cliente.email || 'Sin email'} · {cliente.telefono || 'Sin teléfono'}</span>
                  {(cliente.vehiculos || []).length === 0 ? (
                    <span>Sin vehículos asociados.</span>
                  ) : (
                    <div className="vehicle-history-inline">
                      {cliente.vehiculos.map((vehiculo) => (
                        <span key={vehiculo.id}>
                          {vehiculo.patente || 'Sin patente'} · {vehiculo.marca} {vehiculo.modelo} · {vehiculo.version || 'Sin versión'} · {vehiculo.anio || 'Año N/D'}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </article>

      {showModal ? (
        <div className="modal-backdrop" onClick={closeModal}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>Nuevo cliente</h3>
              <button className="secondary" type="button" onClick={closeModal}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleSubmit}>
              <input name="nombre" value={form.nombre} onChange={handleChange} placeholder="Nombre" required />
              <input name="telefono" value={form.telefono} onChange={handleChange} placeholder="Teléfono" />
              <input name="email" value={form.email} onChange={handleChange} placeholder="Email" />
              <input name="patente" value={form.patente} onChange={handleChange} placeholder="Patente" />
              <select name="marca" value={form.marca} onChange={handleChange}>
                <option value="">Selecciona marca</option>
                {catalogoMarcas.map((marca) => (
                  <option key={marca} value={marca}>{marca}</option>
                ))}
              </select>
              <select name="modelo" value={form.modelo} onChange={handleChange} disabled={!form.marca}>
                <option value="">Selecciona modelo</option>
                {catalogoModelos.map((modelo) => (
                  <option key={modelo} value={modelo}>{modelo}</option>
                ))}
              </select>
              <select name="version" value={form.version} onChange={handleChange} disabled={!form.modelo}>
                <option value="">Selecciona versión</option>
                {catalogoVersiones.map((version) => (
                  <option key={version} value={version}>{version}</option>
                ))}
              </select>
              <select name="anio" value={form.anio} onChange={handleChange}>
                <option value="">Selecciona año</option>
                {catalogoAnios.map((anio) => (
                  <option key={anio} value={anio}>{anio}</option>
                ))}
              </select>
              <button className="primary" type="submit">Guardar cliente</button>
            </form>
          </article>
        </div>
      ) : null}
    </section>
  )
}

export default Clientes