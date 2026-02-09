import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  createServicio,
  createServicioCatalogo,
  deleteServicio,
  downloadServiciosTemplate,
  listCatalogoServicios,
  listServicios,
  listVehiculos,
} from '../services/api'

const initialServicioForm = {
  vehiculo_id: '', descripcion: '', fecha: '', kilometraje: '', costo: '', notas: '',
}

function Taller({ onAction, onAuthError }) {
  const [vehiculos, setVehiculos] = useState([])
  const [servicios, setServicios] = useState([])
  const [catalogoServicios, setCatalogoServicios] = useState([])
  const [servicioForm, setServicioForm] = useState(initialServicioForm)
  const [nuevaTarea, setNuevaTarea] = useState({ nombre: '', categoria: '', intervalo_km: '' })
  const [showCambioModal, setShowCambioModal] = useState(false)
  const [showTareaModal, setShowTareaModal] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const filteredServicios = useMemo(() => {
    if (!servicioForm.vehiculo_id) return servicios
    return servicios.filter((servicio) => String(servicio.vehiculo_id) === String(servicioForm.vehiculo_id))
  }, [servicios, servicioForm.vehiculo_id])

  const loadData = useCallback(async (cancelledRef = { cancelled: false }) => {
    setLoading(true)
    try {
      const [vehiculosData, serviciosData, catalogoServiciosData] = await Promise.all([
        listVehiculos(),
        listServicios(),
        listCatalogoServicios(),
      ])
      if (!cancelledRef.cancelled) {
        setVehiculos(vehiculosData)
        setServicios(serviciosData)
        setCatalogoServicios(catalogoServiciosData.servicios || [])
      }
    } catch (err) {
      if (!cancelledRef.cancelled) {
        setError(err.message)
        if (err.status === 401) onAuthError()
      }
    } finally {
      if (!cancelledRef.cancelled) setLoading(false)
    }
  }, [onAuthError])

  useEffect(() => {
    const ref = { cancelled: false }
    loadData(ref)
    return () => {
      ref.cancelled = true
    }
  }, [loadData])

  const handleServicioSubmit = async (event) => {
    event.preventDefault()
    try {
      const payload = {
        vehiculo_id: Number(servicioForm.vehiculo_id),
        descripcion: servicioForm.descripcion.trim(),
        fecha: servicioForm.fecha || null,
        kilometraje: servicioForm.kilometraje ? Number(servicioForm.kilometraje) : null,
        costo: servicioForm.costo ? Number(servicioForm.costo) : 0,
        notas: servicioForm.notas.trim() || null,
      }
      const nuevo = await createServicio(payload)
      setServicios((prev) => [nuevo, ...prev])
      setServicioForm(initialServicioForm)
      setShowCambioModal(false)
      onAction('Cambio guardado en el historial del vehículo.')
    } catch (err) {
      setError(err.message)
    }
  }

  const handleNuevaTareaSubmit = async (event) => {
    event.preventDefault()
    try {
      await createServicioCatalogo({
        nombre: nuevaTarea.nombre,
        categoria: nuevaTarea.categoria,
        intervalo_km: nuevaTarea.intervalo_km || null,
      })
      setNuevaTarea({ nombre: '', categoria: '', intervalo_km: '' })
      setShowTareaModal(false)
      await loadData()
      onAction('Tarea agregada al catálogo.')
    } catch (err) {
      setError(err.message)
    }
  }

  const handleDescargarPlantilla = async () => {
    try {
      const blob = await downloadServiciosTemplate()
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'plantilla-tareas-taller.json'
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)
      onAction('Plantilla JSON descargada para cargar cambios y tareas.')
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Gestión de tareas del taller</h1>
          <p>En esta página separada podés registrar cambios y administrar tareas.</p>
        </div>
        <div className="list-buttons">
          <button className="secondary" type="button" onClick={() => setShowTareaModal(true)}>Nueva tarea</button>
          <button className="primary" type="button" onClick={() => setShowCambioModal(true)}>Nuevo cambio</button>
        </div>
      </header>
      {error ? <p className="inline-error">{error}</p> : null}

      <div className="page-grid">
        <article className="page-card">
          <h3>Cambios realizados al auto</h3>
          <select name="vehiculo_id" value={servicioForm.vehiculo_id} onChange={(e) => setServicioForm((p) => ({ ...p, vehiculo_id: e.target.value }))}>
            <option value="">Filtrar por vehículo</option>
            {vehiculos.map((vehiculo) => (
              <option key={vehiculo.id} value={vehiculo.id}>{vehiculo.marca} {vehiculo.modelo} {vehiculo.patente ? `· ${vehiculo.patente}` : ''}</option>
            ))}
          </select>
          {loading ? <p>Cargando cambios...</p> : null}
          {!loading && filteredServicios.length === 0 ? <p>No hay cambios registrados.</p> : (
            <ul className="data-list data-list-stacked">
              {filteredServicios.map((servicio) => (
                <li key={servicio.id}>
                  <div>
                    <strong>{servicio.descripcion}</strong>
                    <span>{servicio.vehiculo?.patente || 'Sin patente'} · {servicio.fecha || 'Fecha N/D'} · {servicio.kilometraje ?? 'KM N/D'} km</span>
                  </div>
                  <button className="secondary" type="button" onClick={async () => {
                    await deleteServicio(servicio.id)
                    setServicios((prev) => prev.filter((item) => item.id !== servicio.id))
                  }}>Eliminar</button>
                </li>
              ))}
            </ul>
          )}
        </article>

        <article className="page-card">
          <h3>Catálogo de tareas</h3>
          <p>Se cargan por modal para mantener la pantalla ordenada.</p>
          <button className="secondary" type="button" onClick={() => setShowTareaModal(true)}>Agregar tarea</button>
          <button className="secondary" type="button" onClick={handleDescargarPlantilla}>Descargar plantilla JSON</button>
          <ul className="data-list data-list-stacked">
            {catalogoServicios.slice(0, 10).map((servicio) => (
              <li key={servicio.nombre}><span>{servicio.nombre}</span></li>
            ))}
          </ul>
        </article>
      </div>

      {showCambioModal ? (
        <div className="modal-backdrop" onClick={() => setShowCambioModal(false)}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>Registrar cambio realizado</h3>
              <button className="secondary" type="button" onClick={() => setShowCambioModal(false)}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleServicioSubmit}>
              <select name="vehiculo_id" value={servicioForm.vehiculo_id} onChange={(e) => setServicioForm((p) => ({ ...p, vehiculo_id: e.target.value }))} required>
                <option value="">Selecciona vehículo</option>
                {vehiculos.map((vehiculo) => (
                  <option key={vehiculo.id} value={vehiculo.id}>{vehiculo.marca} {vehiculo.modelo} {vehiculo.patente ? `· ${vehiculo.patente}` : ''}</option>
                ))}
              </select>
              <input name="descripcion" value={servicioForm.descripcion} onChange={(e) => setServicioForm((p) => ({ ...p, descripcion: e.target.value }))} placeholder="Descripción del cambio" list="servicios-catalogo" required />
              <datalist id="servicios-catalogo">
                {catalogoServicios.map((servicio) => <option key={servicio.nombre} value={servicio.nombre} />)}
              </datalist>
              <input name="fecha" type="date" value={servicioForm.fecha} onChange={(e) => setServicioForm((p) => ({ ...p, fecha: e.target.value }))} />
              <input name="kilometraje" type="number" value={servicioForm.kilometraje} onChange={(e) => setServicioForm((p) => ({ ...p, kilometraje: e.target.value }))} placeholder="Kilometraje" />
              <input name="costo" type="number" value={servicioForm.costo} onChange={(e) => setServicioForm((p) => ({ ...p, costo: e.target.value }))} placeholder="Costo" />
              <textarea name="notas" value={servicioForm.notas} onChange={(e) => setServicioForm((p) => ({ ...p, notas: e.target.value }))} placeholder="Notas" />
              <button className="primary" type="submit">Guardar cambio</button>
            </form>
          </article>
        </div>
      ) : null}

      {showTareaModal ? (
        <div className="modal-backdrop" onClick={() => setShowTareaModal(false)}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>Nueva tarea del taller</h3>
              <button className="secondary" type="button" onClick={() => setShowTareaModal(false)}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleNuevaTareaSubmit}>
              <input name="nombre" value={nuevaTarea.nombre} onChange={(e) => setNuevaTarea((p) => ({ ...p, nombre: e.target.value }))} placeholder="Nombre de tarea" required />
              <input name="categoria" value={nuevaTarea.categoria} onChange={(e) => setNuevaTarea((p) => ({ ...p, categoria: e.target.value }))} placeholder="Categoría" />
              <input name="intervalo_km" value={nuevaTarea.intervalo_km} onChange={(e) => setNuevaTarea((p) => ({ ...p, intervalo_km: e.target.value }))} placeholder="Intervalo KM" type="number" />
              <button className="primary" type="submit">Guardar tarea</button>
            </form>
          </article>
        </div>
      ) : null}
    </section>
  )
}

export default Taller