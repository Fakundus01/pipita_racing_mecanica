import { useEffect, useState } from 'react'
import { createParte, deleteParte, listCatalogoPartes, listPartes, updateParte } from '../services/api'

function Partes({ onAction, onAuthError }) {
  const [partes, setPartes] = useState([])
  const [form, setForm] = useState({ nombre: '', stock: '', costo: '' })
  const [editingId, setEditingId] = useState(null)
  const [loading, setLoading] = useState(true)
  const [catalogoLoading, setCatalogoLoading] = useState(true)
  const [catalogoPartes, setCatalogoPartes] = useState([])
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

  useEffect(() => {
    let cancelled = false
    const loadCatalogo = async () => {
      setCatalogoLoading(true)
      try {
        const data = await listCatalogoPartes()
        if (!cancelled) {
          setCatalogoPartes(data.partes || [])
        }
      } catch (err) {
        if (!cancelled) {
          if (err.status === 401) {
            onAuthError()
          }
        }
      } finally {
        if (!cancelled) {
          setCatalogoLoading(false)
        }
      }
    }
    loadCatalogo()
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
      const payload = {
        ...form,
        stock: form.stock ? Number(form.stock) : 0,
        costo: form.costo ? Number(form.costo) : 0,
      }
      if (editingId) {
        const actualizada = await updateParte(editingId, payload)
        setPartes((prev) =>
          prev.map((parte) => (parte.id === editingId ? actualizada : parte))
        )
        setEditingId(null)
        setForm({ nombre: '', stock: '', costo: '' })
        onAction(`Parte ${actualizada.nombre} actualizada.`)
      } else {
        const nuevo = await createParte(payload)
        setPartes((prev) => [nuevo, ...prev])
        setForm({ nombre: '', stock: '', costo: '' })
        onAction(`Parte ${nuevo.nombre} registrada.`)
      }
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleEdit = (parte) => {
    setEditingId(parte.id)
    setForm({
      nombre: parte.nombre || '',
      stock: parte.stock ?? '',
      costo: parte.costo ?? '',
    })
  }

  const handleCatalogoPick = (parte) => {
    setEditingId(null)
    setForm({ nombre: parte.nombre || '', stock: '', costo: '' })
    onAction(`Parte sugerida "${parte.nombre}" preparada para cargar.`)
  }

  const handleDelete = async (parteId) => {
    setError('')
    try {
      await deleteParte(parteId)
      setPartes((prev) => prev.filter((parte) => parte.id !== parteId))
      onAction('Parte eliminada.')
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
          <h3>{editingId ? 'Editar repuesto' : 'Registrar repuesto'}</h3>
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
            <div className="form-actions-inline">
              <button className="secondary" type="submit">
                {editingId ? 'Actualizar' : 'Guardar'}
              </button>
              {editingId ? (
                <button
                  className="secondary"
                  type="button"
                  onClick={() => {
                    setEditingId(null)
                    setForm({ nombre: '', stock: '', costo: '' })
                  }}
                >
                  Cancelar
                </button>
              ) : null}
            </div>
          </form>
          {error ? <p className="inline-error">{error}</p> : null}
        </article>
         <article className="page-card">
          <h3>Catálogo sugerido</h3>
          {catalogoLoading ? <p>Cargando catálogo...</p> : null}
          {!catalogoLoading && catalogoPartes.length === 0 ? (
            <p>No hay piezas sugeridas.</p>
          ) : (
            <ul className="data-list">
              {catalogoPartes.map((parte, index) => (
                <li key={`${parte.nombre}-${index}`}>
                  <div>
                    <strong>{parte.nombre}</strong>
                    <span>{parte.categoria || 'Sin categoría'}</span>
                  </div>
                  <div className="list-actions">
                    <span>{parte.descripcion || 'Sin descripción'}</span>
                    <div className="list-buttons">
                      <button
                        className="secondary"
                        type="button"
                        onClick={() => handleCatalogoPick(parte)}
                      >
                        Usar
                      </button>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
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
                  <div className="list-actions">
                    <span>${Number(parte.costo || 0).toFixed(2)}</span>
                    <div className="list-buttons">
                      <button className="secondary" type="button" onClick={() => handleEdit(parte)}>
                        Editar
                      </button>
                      <button className="secondary" type="button" onClick={() => handleDelete(parte.id)}>
                        Eliminar
                      </button>
                    </div>
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

export default Partes