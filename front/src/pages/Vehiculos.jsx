import { useEffect, useState } from 'react'
import { createVehiculo, decodeVin, listVehiculos } from '../api'

function Vehiculos({ onAction, onAuthError }) {
  const [vehiculos, setVehiculos] = useState([])
  const [form, setForm] = useState({ marca: '', modelo: '', anio: '' })
  const [vin, setVin] = useState('')
  const [vinInfo, setVinInfo] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [vinError, setVinError] = useState('')

  useEffect(() => {
    let cancelled = false
    const loadVehiculos = async () => {
      setLoading(true)
      setError('')
      try {
        const data = await listVehiculos()
        if (!cancelled) {
          setVehiculos(data)
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
    loadVehiculos()
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
      const nuevo = await createVehiculo({
        ...form,
        anio: form.anio ? Number(form.anio) : null,
      })
      setVehiculos((prev) => [nuevo, ...prev])
      setForm({ marca: '', modelo: '', anio: '' })
      onAction(`Vehículo ${nuevo.marca} ${nuevo.modelo} guardado.`)
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleVinLookup = async (event) => {
    event.preventDefault()
    setVinError('')
    setVinInfo(null)
    try {
      const data = await decodeVin(vin)
      setVinInfo(data)
      onAction('VIN decodificado desde la API externa.')
    } catch (err) {
      setVinError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Vehículos en catálogo</h1>
          <p>Organiza inventario y carga fichas con datos de API.</p>
        </div>
        <button className="primary" onClick={() => onAction('Completa el formulario para crear una ficha.')}>
          Nueva ficha
        </button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Registrar vehículo</h3>
          <form className="mini-form" onSubmit={handleSubmit}>
            <input
              name="marca"
              value={form.marca}
              onChange={handleChange}
              placeholder="Marca"
              required
            />
            <input
              name="modelo"
              value={form.modelo}
              onChange={handleChange}
              placeholder="Modelo"
              required
            />
            <input
              name="anio"
              value={form.anio}
              onChange={handleChange}
              placeholder="Año"
              type="number"
              min="1900"
              max="2100"
            />
            <button className="secondary" type="submit">Guardar</button>
          </form>
          {error ? <p className="inline-error">{error}</p> : null}
        </article>
        <article className="page-card">
          <h3>Decodificar VIN (API NHTSA)</h3>
          <form className="mini-form" onSubmit={handleVinLookup}>
            <input
              value={vin}
              onChange={(event) => setVin(event.target.value)}
              placeholder="VIN / Chasis"
              required
            />
            <button className="secondary" type="submit">Consultar</button>
          </form>
          {vinError ? <p className="inline-error">{vinError}</p> : null}
          {vinInfo ? (
            <div className="vin-result">
              <p><strong>{vinInfo.marca || 'Marca N/D'}</strong> {vinInfo.modelo || ''}</p>
              <p>Año: {vinInfo.anio || 'N/D'} · Combustible: {vinInfo.combustible || 'N/D'}</p>
              <p>Carrocería: {vinInfo.tipo_carroceria || 'N/D'} · País: {vinInfo.pais_origen || 'N/D'}</p>
            </div>
          ) : null}
        </article>
        <article className="page-card">
          <h3>Inventario activo</h3>
          {loading ? <p>Cargando vehículos...</p> : null}
          {!loading && vehiculos.length === 0 ? (
            <p>No hay vehículos registrados.</p>
          ) : (
            <ul className="data-list">
              {vehiculos.map((vehiculo) => (
                <li key={vehiculo.id}>
                  <div>
                    <strong>{vehiculo.marca} {vehiculo.modelo}</strong>
                    <span>Estado: {vehiculo.estado}</span>
                  </div>
                  <span>{vehiculo.anio || 'Año N/D'}</span>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>
    </section>
  )
}

export default Vehiculos