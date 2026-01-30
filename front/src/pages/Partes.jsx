import { useEffect, useState } from 'react'
import { createParte, listPartes } from '../api'

function Partes({ onAction, onAuthError }) {
  const [partes, setPartes] = useState([])
  const [form, setForm] = useState({ nombre: '', stock: '', costo: '' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    const loadPartes = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await listPartes()
        if (!cancelled) {
          setPartes(data)
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
    loadPartes()
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
      const nuevo = await createParte({
        ...form,
        stock: form.stock ? Number(form.stock) : 0,
        costo: form.costo ? Number(form.costo) : 0,
      })
      setPartes((prev) => [nuevo, ...prev])
      setForm({ nombre: '', stock: '', costo: '' })
      onAction(`Parte ${nuevo.nombre} registrada.`)
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
          <h1>Partes & accesorios</h1>
          <p>Controla stock, costos y partes vinculadas a cada venta.</p>
        </div>
        <button className="primary" onClick={() => onAction('Completa el formulario para agregar un repuesto.')}>
          Agregar repuesto
        </button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Registrar repuesto</h3>
          <form className="mini-form" onSubmit={handleSubmit}>
            <input
              name="nombre"
              value={form.nombre}
              onChange={handleChange}
              placeholder="Nombre"
              required
            />
            <input
              name="stock"
              value={form.stock}
              onChange={handleChange}
              placeholder="Stock"
              type="number"
              min="0"
            />
            <input
              name="costo"
              value={form.costo}
              onChange={handleChange}
              placeholder="Costo"
              type="number"
              min="0"
              step="0.01"
            />
            <button className="secondary" type="submit">Guardar</button>
          </form>
          {error ? <p className="inline-error">{error}</p> : null}
        </article>
        <article className="page-card">
          <h3>Inventario actual</h3>
          {loading ? <p>Cargando partes...</p> : null}
          {!loading && partes.length === 0 ? (
            <p>No hay partes registradas.</p>
          ) : (
            <ul className="data-list">
              {partes.map((parte) => (
                <li key={parte.id}>
                  <div>
                    <strong>{parte.nombre}</strong>
                    <span>Stock: {parte.stock}</span>
                  </div>
                  <span>${Number(parte.costo || 0).toFixed(2)}</span>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>
    </section>
  )
}

export default Partes