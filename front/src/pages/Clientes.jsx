import { useEffect, useState } from 'react'
import { createCliente, listClientes } from '../services/api'

function Clientes({ onAction, onAuthError }) {
  const [clientes, setClientes] = useState([])
  const [form, setForm] = useState({
    nombre: '',
    telefono: '',
    email: '',
  })
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

  const handleChange = (event) => {
    const { name, value } = event.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      const nuevo = await createCliente(form)
      setClientes((prev) => [nuevo, ...prev])
      setForm({ nombre: '', telefono: '', email: '' })
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
                  <span>{cliente.telefono || 'Sin teléfono'}</span>
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