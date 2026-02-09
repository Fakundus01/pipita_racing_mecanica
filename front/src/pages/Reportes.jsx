import { useEffect, useState } from 'react'
import {
  createReporte,
  deleteReporte,
  listReportes,
  printReporteUrl,
  updateReporte,
} from '../services/api'

function Reportes({ onAction, onAuthError }) {
  const [reportes, setReportes] = useState([])
  const [form, setForm] = useState({ titulo: '', periodo: '' })
  const [editingId, setEditingId] = useState(null)
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
      if (editingId) {
        const actualizado = await updateReporte(editingId, form)
        setReportes((prev) =>
          prev.map((reporte) => (reporte.id === editingId ? actualizado : reporte))
        )
        setEditingId(null)
        setForm({ titulo: '', periodo: '' })
        onAction(`Reporte ${actualizado.titulo} actualizado.`)
      } else {
        const nuevo = await createReporte(form)
        setReportes((prev) => [nuevo, ...prev])
        setForm({ titulo: '', periodo: '' })
        onAction(`Reporte ${nuevo.titulo} generado.`)
      }
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleEdit = (reporte) => {
    setEditingId(reporte.id)
    setForm({ titulo: reporte.titulo || '', periodo: reporte.periodo || '' })
  }

  const handleDelete = async (reporteId) => {
    setError('')
    try {
      await deleteReporte(reporteId)
      setReportes((prev) => prev.filter((reporte) => reporte.id !== reporteId))
      onAction('Reporte eliminado.')
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handlePrint = (reporteId) => {
    window.open(printReporteUrl(reporteId), '_blank', 'noopener,noreferrer')
    onAction('Enviamos el reporte a impresión.')
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Reportes y planillas</h1>
          <p>Genera PDFs listos para imprimir y compartir.</p>
        </div>
        <button
          className="primary"
          type="button"
          onClick={() => {
            if (reportes.length === 0) {
              onAction('No hay reportes para imprimir todavía.')
              return
            }
            handlePrint(reportes[0].id)
          }}
        >
          Imprimir último
        </button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>{editingId ? 'Editar reporte' : 'Generar reporte'}</h3>
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
                    setForm({ titulo: '', periodo: '' })
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
                  <div className="list-actions">
                    <span>{reporte.generado_el}</span>
                    <div className="list-buttons">
                      <button className="secondary" type="button" onClick={() => handlePrint(reporte.id)}>
                        Imprimir
                      </button>
                      <button className="secondary" type="button" onClick={() => handleEdit(reporte)}>
                        Editar
                      </button>
                      <button className="secondary" type="button" onClick={() => handleDelete(reporte.id)}>
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

export default Reportes