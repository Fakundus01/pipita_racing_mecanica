import { useState } from 'react'
import { addCatalogoVehiculo } from '../services/api'

function CatalogoVehiculos({ onAction }) {
  const [showModal, setShowModal] = useState(false)
  const [catalogoNuevo, setCatalogoNuevo] = useState({ marca: '', modelo: '', version: '', anio: '' })
  const [status, setStatus] = useState('')

  const handleSubmit = async (event) => {
    event.preventDefault()
    setStatus('')
    try {
      const payload = {
        marca: catalogoNuevo.marca.trim(),
        modelo: catalogoNuevo.modelo.trim(),
        version: catalogoNuevo.version.trim(),
        anio: catalogoNuevo.anio ? Number(catalogoNuevo.anio) : null,
      }
      await addCatalogoVehiculo(payload)
      setStatus('Entrada agregada al catálogo de vehículos.')
      setCatalogoNuevo({ marca: '', modelo: '', version: '', anio: '' })
      setShowModal(false)
      onAction('Marca/modelo/versión agregados al catálogo.')
    } catch (err) {
      setStatus(err.message)
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Catálogo de vehículos</h1>
          <p>Alta de nuevas marcas, modelos y versiones en una página dedicada.</p>
        </div>
        <button className="primary" type="button" onClick={() => setShowModal(true)}>
          Agregar entrada
        </button>
      </header>

      <article className="page-card">
        <h3>Gestión de catálogo</h3>
        <p>Casi todo lo editable/cargable se gestiona en modal para mantener limpieza visual.</p>
        <button className="secondary" type="button" onClick={() => setShowModal(true)}>Nueva marca/modelo</button>
        {status ? <p className="inline-error">{status}</p> : null}
      </article>

      {showModal ? (
        <div className="modal-backdrop" onClick={() => setShowModal(false)}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>Agregar versión al catálogo</h3>
              <button className="secondary" type="button" onClick={() => setShowModal(false)}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleSubmit}>
              <input name="marca" value={catalogoNuevo.marca} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, marca: e.target.value }))} placeholder="Marca" required />
              <input name="modelo" value={catalogoNuevo.modelo} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, modelo: e.target.value }))} placeholder="Modelo" required />
              <input name="version" value={catalogoNuevo.version} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, version: e.target.value }))} placeholder="Versión" />
              <input name="anio" value={catalogoNuevo.anio} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, anio: e.target.value }))} placeholder="Año" type="number" />
              <button className="primary" type="submit">Agregar</button>
            </form>
          </article>
        </div>
      ) : null}
    </section>
  )
}

export default CatalogoVehiculos