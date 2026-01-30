import { useEffect, useState } from 'react'
import { createReporte, listReportes } from '../api'

function Reportes({ onAction, onAuthError }) {
  const [reportes, setReportes] = useState([])
  const [form, setForm] = useState({ titulo: '', periodo: '' })
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let cancelled = false
    const loadReportes = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await listReportes()
        if (!cancelled) {
          setReportes(data)
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
    loadReportes()
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
      const nuevo = await createReporte(form)
      setReportes((prev) => [nuevo, ...prev])
      setForm({ titulo: '', periodo: '' })
      onAction(`Reporte ${nuevo.titulo} generado.`)
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handlePrint = () => {
    window.print()
    onAction('Enviamos el reporte a impresión.')
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Reportes y planillas</h1>
          <p>Genera PDFs listos para imprimir y compartir.</p>
        </div>
        <button className="primary" onClick={handlePrint}>Imprimir</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Generar reporte</h3>
          <form className="mini-form" onSubmit={handleSubmit}>
            <input
              name="titulo"
              value={form.titulo}
              onChange={handleChange}
              placeholder="Título"
              required
            />
            <input
              name="periodo"
              value={form.periodo}
              onChange={handleChange}
              placeholder="Periodo (ej. Marzo 2025)"
            />
            <button className="secondary" type="submit">Guardar</button>
          </form>
          {error ? <p className="inline-error">{error}</p> : null}
        </article>
        <article className="page-card">
          <h3>Historial reciente</h3>
          {loading ? <p>Cargando reportes...</p> : null}
          {!loading && reportes.length === 0 ? (
            <p>No hay reportes registrados.</p>
          ) : (
            <ul className="data-list">
              {reportes.map((reporte) => (
                <li key={reporte.id}>
                  <div>
                    <strong>{reporte.titulo}</strong>
                    <span>{reporte.periodo || 'Sin periodo'}</span>
                  </div>
                  <span>{reporte.generado_el}</span>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>
    </section>
  )
}

export default Reportes