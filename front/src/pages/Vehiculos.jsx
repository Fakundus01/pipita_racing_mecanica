import { useEffect, useMemo, useState } from 'react'
import {
  addCatalogoVehiculo,
  createServicio,
  createVehiculo,
  deleteServicio,
  deleteVehiculo,
  getVehiculoPorPatente,
  listCatalogoAnios,
  listCatalogoMarcas,
  listCatalogoModelos,
  listCatalogoServicios,
  listCatalogoVersiones,
  listClientes,
  listServicios,
  listVehiculos,
  updateVehiculo,
} from '../services/api'

const initialForm = { patente: '', marca: '', modelo: '', version: '', anio: '' }

function Vehiculos({ onAction, onAuthError }) {
  const [vehiculos, setVehiculos] = useState([])
  const [clientes, setClientes] = useState([])
  const [form, setForm] = useState(initialForm)
  const [editingId, setEditingId] = useState(null)
  const [showVehiculoModal, setShowVehiculoModal] = useState(false)
  const [catalogoMarcas, setCatalogoMarcas] = useState([])
  const [catalogoModelos, setCatalogoModelos] = useState([])
  const [catalogoVersiones, setCatalogoVersiones] = useState([])
  const [catalogoAnios, setCatalogoAnios] = useState([])
  const [catalogoServicios, setCatalogoServicios] = useState([])
  const [catalogoNuevo, setCatalogoNuevo] = useState({ marca: '', modelo: '', version: '', anio: '' })
  const [catalogoNuevoStatus, setCatalogoNuevoStatus] = useState('')
  const [patenteBusqueda, setPatenteBusqueda] = useState('')
  const [clienteBusquedaId, setClienteBusquedaId] = useState('')
  const [patenteClienteSeleccionada, setPatenteClienteSeleccionada] = useState('')
  const [patenteInfo, setPatenteInfo] = useState(null)
  const [loading, setLoading] = useState(true)
  const [serviciosLoading, setServiciosLoading] = useState(true)
  const [servicios, setServicios] = useState([])
  const [servicioForm, setServicioForm] = useState({
    vehiculo_id: '', descripcion: '', fecha: '', kilometraje: '', costo: '', notas: '',
  })
  const [error, setError] = useState('')

  const vehiculosPorCliente = useMemo(() => {
    if (!clienteBusquedaId) {
      return []
    }
    const cliente = clientes.find((item) => String(item.id) === String(clienteBusquedaId))
    return cliente?.vehiculos || []
  }, [clientes, clienteBusquedaId])

  useEffect(() => {
    let cancelled = false
    const loadVehiculos = async () => {
      setLoading(true)
      setError('')
      try {
        const [vehiculosData, clientesData] = await Promise.all([listVehiculos(), listClientes()])
        if (!cancelled) {
          setVehiculos(vehiculosData)
          setClientes(clientesData)
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) onAuthError()
          }        
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    loadVehiculos()
    return () => { cancelled = true }
  }, [onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadServicios = async () => {
      setServiciosLoading(true)
      try {
        const [serviciosData, catalogoServiciosData] = await Promise.all([listServicios(), listCatalogoServicios()])
        if (!cancelled) {
          setServicios(serviciosData)
          setCatalogoServicios(catalogoServiciosData.servicios || [])
        }
      } catch (err) {
        if (!cancelled && err.status === 401) onAuthError()
      } finally {
       if (!cancelled) setServiciosLoading(false)
      }
    }
    loadServicios()
    return () => { cancelled = true }
  }, [onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadCatalogo = async () => {
      try {
        const [marcasData, aniosData] = await Promise.all([listCatalogoMarcas(), listCatalogoAnios()])
        if (!cancelled) {
          setCatalogoMarcas(marcasData.marcas || [])
          setCatalogoAnios(aniosData.anios || [])
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
          if (err.status === 401) onAuthError()
        }
      }
    }
    loadCatalogo()
    return () => { cancelled = true }
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
        }
      } catch (err) {
        if (!cancelled) setError(err.message)
      }
    }
    loadModelos()
    return () => { cancelled = true }
  }, [form.marca])

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
        if (!cancelled) setError(err.message)
      }
    }
    loadVersiones()
    return () => { cancelled = true }
  }, [form.marca, form.modelo])

  const applyVehiculoToForms = (vehiculo) => {
    if (!vehiculo) return
    setForm({
      patente: vehiculo.patente || '',
      marca: vehiculo.marca || '',
      modelo: vehiculo.modelo || '',
      version: vehiculo.version || '',
      anio: vehiculo.anio || '',
    })
    setServicioForm((prev) => ({ ...prev, vehiculo_id: String(vehiculo.id) }))
    setPatenteInfo(vehiculo)
    onAction(`Autocompletado aplicado para ${vehiculo.patente || `${vehiculo.marca} ${vehiculo.modelo}`}.`)
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      const payload = {
        ...form,
        patente: form.patente || null,
        version: form.version || null,
        anio: form.anio ? Number(form.anio) : null,
      }
      if (editingId) {
        const actualizado = await updateVehiculo(editingId, payload)
        setVehiculos((prev) => prev.map((vehiculo) => (vehiculo.id === editingId ? actualizado : vehiculo)))
      } else {
        const nuevo = await createVehiculo(payload)
        setVehiculos((prev) => [nuevo, ...prev])
      }
      setEditingId(null)
      setForm(initialForm)
      setShowVehiculoModal(false)
      onAction('Ficha de vehículo guardada.')
    } catch (err) {
      setError(err.message)
      if (err.status === 401) onAuthError()
   }
  }

  const handleEdit = (vehiculo) => {
    setEditingId(vehiculo.id)
    applyVehiculoToForms(vehiculo)
    setShowVehiculoModal(true)
  }

  const handleDelete = async (vehiculoId) => {
    setError('')
    try {
      await deleteVehiculo(vehiculoId)
      setVehiculos((prev) => prev.filter((vehiculo) => vehiculo.id !== vehiculoId))
      onAction('Vehículo eliminado.')
    } catch (err) {
      setError(err.message)
    }
  }

  const handlePatenteLookup = async () => {
    if (!patenteBusqueda) return
    try {
      const data = await getVehiculoPorPatente(patenteBusqueda)
      applyVehiculoToForms(data)
    } catch (err) {
      setError(err.message)
    }
  }

  const handleCatalogoNuevoSubmit = async (event) => {
    event.preventDefault()
    setCatalogoNuevoStatus('')
    try {
      const payload = {
        marca: catalogoNuevo.marca.trim(),
        modelo: catalogoNuevo.modelo.trim(),
        version: catalogoNuevo.version.trim(),
        anio: catalogoNuevo.anio ? Number(catalogoNuevo.anio) : null,
      }
      await addCatalogoVehiculo(payload)
      setCatalogoNuevoStatus('Entrada agregada al catálogo con versión.')
      setCatalogoNuevo({ marca: '', modelo: '', version: '', anio: '' })
    } catch (err) {
      setCatalogoNuevoStatus(err.message)
    }
  }

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
      setServicioForm({ vehiculo_id: '', descripcion: '', fecha: '', kilometraje: '', costo: '', notas: '' })
      onAction('Servicio registrado en el historial del vehículo.')
    } catch (err) {
      setError(err.message)
    }
  }

const filteredServicios = useMemo(() => {
    if (!servicioForm.vehiculo_id) return servicios
    return servicios.filter((servicio) => String(servicio.vehiculo_id) === String(servicioForm.vehiculo_id))
  }, [servicios, servicioForm.vehiculo_id])

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Vehículos y servicios</h1>
          <p>Autocompletado por cliente o patente + historial técnico del vehículo.</p>
        </div>
        <button className="primary" onClick={() => setShowVehiculoModal(true)}>Cargar vehículo</button>
      </header>
      {error ? <p className="inline-error">{error}</p> : null}

      <div className="page-grid">
        <article className="page-card">
          <h3>Autocompletar ficha</h3>
          <select value={clienteBusquedaId} onChange={(event) => setClienteBusquedaId(event.target.value)}>
            <option value="">Selecciona cliente</option>
            {clientes.map((cliente) => <option key={cliente.id} value={cliente.id}>{cliente.nombre}</option>)}
          </select>
          <select value={patenteClienteSeleccionada} onChange={(event) => {
            const patente = event.target.value
            setPatenteClienteSeleccionada(patente)
            const vehiculo = vehiculosPorCliente.find((item) => item.patente === patente)
            applyVehiculoToForms(vehiculo)
          }} disabled={vehiculosPorCliente.length === 0}>
            <option value="">Selecciona patente</option>
            {vehiculosPorCliente.map((vehiculo) => (
              <option key={vehiculo.id} value={vehiculo.patente || `vehiculo-${vehiculo.id}`}>
                {vehiculo.patente || 'Sin patente'} · {vehiculo.marca} {vehiculo.modelo}
              </option>
            ))}
          </select>
          <input
            value={patenteBusqueda}
            onChange={(event) => setPatenteBusqueda(event.target.value)}
            placeholder="Buscar por patente"
            list="patentes-list"
          />
          <datalist id="patentes-list">
            {vehiculos.filter((vehiculo) => vehiculo.patente).map((vehiculo) => (
              <option key={vehiculo.id} value={vehiculo.patente} />
            ))}
          </datalist>
          <button className="secondary" onClick={handlePatenteLookup}>Autocompletar por patente</button>
          {patenteInfo ? (
            <div className="vin-result">
              <p><strong>{patenteInfo.patente || 'Patente N/D'}</strong></p>
              <p>{patenteInfo.marca} {patenteInfo.modelo} · {patenteInfo.version || 'Sin versión'}</p>
              <p>Año: {patenteInfo.anio || 'N/D'}</p>
            </div>
          ) : null}
        </article>

        <article className="page-card">
          <h3>Agregar versión al JSON catálogo</h3>
          <form className="mini-form" onSubmit={handleCatalogoNuevoSubmit}>
            <input name="marca" value={catalogoNuevo.marca} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, marca: e.target.value }))} placeholder="Marca" required />
            <input name="modelo" value={catalogoNuevo.modelo} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, modelo: e.target.value }))} placeholder="Modelo" required />
            <input name="version" value={catalogoNuevo.version} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, version: e.target.value }))} placeholder="Versión" />
            <input name="anio" value={catalogoNuevo.anio} onChange={(e) => setCatalogoNuevo((p) => ({ ...p, anio: e.target.value }))} placeholder="Año" type="number" />
            <button className="secondary" type="submit">Agregar</button>
          </form>
          {catalogoNuevoStatus ? <p className="inline-error">{catalogoNuevoStatus}</p> : null}
        </article>
        
        <article className="page-card">
          <h3>Servicios realizados</h3>
          <form className="mini-form" onSubmit={handleServicioSubmit}>
            <select name="vehiculo_id" value={servicioForm.vehiculo_id} onChange={(e) => setServicioForm((p) => ({ ...p, vehiculo_id: e.target.value }))} required>
              <option value="">Selecciona vehículo</option>
              {vehiculos.map((vehiculo) => (
                <option key={vehiculo.id} value={vehiculo.id}>{vehiculo.marca} {vehiculo.modelo} {vehiculo.patente ? `· ${vehiculo.patente}` : ''}</option>
              ))}
            </select>
            <input name="descripcion" value={servicioForm.descripcion} onChange={(e) => setServicioForm((p) => ({ ...p, descripcion: e.target.value }))} placeholder="Descripción" list="servicios-catalogo" required />
            <datalist id="servicios-catalogo">
              {catalogoServicios.map((servicio) => <option key={servicio.nombre} value={servicio.nombre} />)}
            </datalist>
            <input name="fecha" type="date" value={servicioForm.fecha} onChange={(e) => setServicioForm((p) => ({ ...p, fecha: e.target.value }))} />
            <input name="kilometraje" type="number" value={servicioForm.kilometraje} onChange={(e) => setServicioForm((p) => ({ ...p, kilometraje: e.target.value }))} placeholder="Kilometraje" />
            <input name="costo" type="number" value={servicioForm.costo} onChange={(e) => setServicioForm((p) => ({ ...p, costo: e.target.value }))} placeholder="Costo" />
            <textarea name="notas" value={servicioForm.notas} onChange={(e) => setServicioForm((p) => ({ ...p, notas: e.target.value }))} placeholder="Notas" />
            <button className="secondary" type="submit">Guardar servicio</button>
          </form>
          {serviciosLoading ? <p>Cargando servicios...</p> : null}
          {!serviciosLoading && filteredServicios.length === 0 ? <p>No hay servicios registrados.</p> : (
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
          <h3>Inventario activo</h3>
          {loading ? <p>Cargando vehículos...</p> : null}
          {!loading && vehiculos.length === 0 ? <p>No hay vehículos registrados.</p> : (
            <ul className="data-list data-list-stacked">
              {vehiculos.map((vehiculo) => (
                <li key={vehiculo.id}>
                  <div>
                    <strong>{vehiculo.marca} {vehiculo.modelo}</strong>
                    <span>{vehiculo.patente || 'Sin patente'} · {vehiculo.version || 'Sin versión'} · {vehiculo.anio || 'Año N/D'}</span>
                  </div>
                  <div className="list-buttons">
                    <button className="secondary" onClick={() => handleEdit(vehiculo)}>Editar</button>
                    <button className="secondary" onClick={() => handleDelete(vehiculo.id)}>Eliminar</button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      {showVehiculoModal ? (
        <div className="modal-backdrop" onClick={() => setShowVehiculoModal(false)}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>{editingId ? 'Editar vehículo' : 'Registrar vehículo'}</h3>
              <button className="secondary" onClick={() => setShowVehiculoModal(false)}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleSubmit}>
              <input name="patente" value={form.patente} onChange={(e) => setForm((p) => ({ ...p, patente: e.target.value }))} placeholder="Patente" />
              <select name="marca" value={form.marca} onChange={(e) => setForm((p) => ({ ...p, marca: e.target.value }))} required>
                <option value="">Selecciona marca</option>
                {catalogoMarcas.map((marca) => <option key={marca} value={marca}>{marca}</option>)}
              </select>
              <select name="modelo" value={form.modelo} onChange={(e) => setForm((p) => ({ ...p, modelo: e.target.value }))} required disabled={!form.marca}>
                <option value="">Selecciona modelo</option>
                {catalogoModelos.map((modelo) => <option key={modelo} value={modelo}>{modelo}</option>)}
              </select>
              <select name="version" value={form.version} onChange={(e) => setForm((p) => ({ ...p, version: e.target.value }))} disabled={!form.modelo}>
                <option value="">Selecciona versión</option>
                {catalogoVersiones.map((version) => <option key={version} value={version}>{version}</option>)}
              </select>
              <select name="anio" value={form.anio} onChange={(e) => setForm((p) => ({ ...p, anio: e.target.value }))}>
                <option value="">Selecciona año</option>
                {catalogoAnios.map((anio) => <option key={anio} value={anio}>{anio}</option>)}
              </select>
              <button className="primary" type="submit">Guardar vehículo</button>
            </form>
          </article>
        </div>
      ) : null}
    </section>
  )
}

export default Vehiculos