import { useEffect, useState } from 'react'
import {
  createVehiculo,
  decodeVehiculo,
  deleteVehiculo,
  getVehiculoPorPatente,
  listVehiculos,
  listCatalogoAnios,
  listCatalogoMarcas,
  listCatalogoModelos,
  listCatalogoVersiones,
  updateVehiculo,
} from '../services/api'

function Vehiculos({ onAction, onAuthError }) {
  const [vehiculos, setVehiculos] = useState([])
  const [form, setForm] = useState({
    patente: '',
    marca: '',
    modelo: '',
    version: '',
    anio: '',
  })
  const [editingId, setEditingId] = useState(null)
  const [catalogoForm, setCatalogoForm] = useState({ marca: '', modelo: '', version: '' })
  const [catalogoInfo, setCatalogoInfo] = useState(null)
  const [catalogoMarcas, setCatalogoMarcas] = useState([])
  const [catalogoModelos, setCatalogoModelos] = useState([])
  const [catalogoVersiones, setCatalogoVersiones] = useState([])
  const [catalogoAnios, setCatalogoAnios] = useState([])
  const [catalogoModelosForm, setCatalogoModelosForm] = useState([])
  const [catalogoVersionesForm, setCatalogoVersionesForm] = useState([])
  const [patenteBusqueda, setPatenteBusqueda] = useState('')
  const [patenteInfo, setPatenteInfo] = useState(null)  
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [catalogoError, setCatalogoError] = useState('')
  const [patenteError, setPatenteError] = useState('')

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

  useEffect(() => {
    let cancelled = false
    const loadModelosCatalogo = async () => {
      if (!catalogoForm.marca) {
        setCatalogoModelosForm([])
        setCatalogoVersionesForm([])
        return
      }
      try {
        const data = await listCatalogoModelos(catalogoForm.marca)
        if (!cancelled) {
          setCatalogoModelosForm(data.modelos || [])
          setCatalogoVersionesForm([])
        }
      } catch (err) {
        if (!cancelled) {
          setCatalogoError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      }
    }
    loadModelosCatalogo()
    return () => {
      cancelled = true
    }
  }, [catalogoForm.marca, onAuthError])

  useEffect(() => {
    let cancelled = false
    const loadVersionesCatalogo = async () => {
      if (!catalogoForm.marca || !catalogoForm.modelo) {
        setCatalogoVersionesForm([])
        return
      }
      try {
        const data = await listCatalogoVersiones(catalogoForm.marca, catalogoForm.modelo)
        if (!cancelled) {
          setCatalogoVersionesForm(data.versiones || [])
        }
      } catch (err) {
        if (!cancelled) {
          setCatalogoError(err.message)
          if (err.status === 401) {
            onAuthError()
          }
        }
      }
    }
    loadVersionesCatalogo()
    return () => {
      cancelled = true
    }
  }, [catalogoForm.marca, catalogoForm.modelo, onAuthError])

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
        patente: form.patente || null,
        version: form.version || null,
        anio: form.anio ? Number(form.anio) : null,
      }
      if (editingId) {
        const actualizado = await updateVehiculo(editingId, payload)
        setVehiculos((prev) =>
          prev.map((vehiculo) => (vehiculo.id === editingId ? actualizado : vehiculo))
        )
        setEditingId(null)
        setForm({ patente: '', marca: '', modelo: '', version: '', anio: '' })
        onAction(`Vehículo ${actualizado.marca} actualizado.`)
      } else {
        const nuevo = await createVehiculo(payload)
        setVehiculos((prev) => [nuevo, ...prev])
        setForm({ patente: '', marca: '', modelo: '', version: '', anio: '' })
        onAction(`Vehículo ${nuevo.marca} ${nuevo.modelo} guardado.`)
      }
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleEdit = (vehiculo) => {
    setEditingId(vehiculo.id)
    setForm({
      patente: vehiculo.patente || '',
      marca: vehiculo.marca || '',
      modelo: vehiculo.modelo || '',
      version: vehiculo.version || '',
      anio: vehiculo.anio || '',
    })
  }

  const handleDelete = async (vehiculoId) => {
    setError('')
    try {
      await deleteVehiculo(vehiculoId)
      setVehiculos((prev) => prev.filter((vehiculo) => vehiculo.id !== vehiculoId))
      onAction('Vehículo eliminado.')
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleCatalogoLookup = async (event) => {
    event.preventDefault()
    setCatalogoError('')
    setCatalogoInfo(null)
    try {
      const data = await decodeVehiculo(catalogoForm)
      setCatalogoInfo(data)
      onAction('Ficha localizada en el catálogo local.')
    } catch (err) {
      setCatalogoError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handlePatenteLookup = async (event) => {
    event.preventDefault()
    setPatenteError('')
    setPatenteInfo(null)
    if (!patenteBusqueda) {
      setPatenteError('Ingresa una patente para consultar.')
      return
    }
    try {
      const data = await getVehiculoPorPatente(patenteBusqueda)
      setPatenteInfo(data)
      onAction('Patente localizada en la base de datos.')
    } catch (err) {
      setPatenteError(err.message)
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
          <p>Organiza inventario y consulta fichas desde el catálogo local.</p>
        </div>
        <button className="primary" onClick={() => onAction('Completa el formulario para crear una ficha.')}>
          Nueva ficha
        </button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>{editingId ? 'Editar vehículo' : 'Registrar vehículo'}</h3>
          <form className="mini-form" onSubmit={handleSubmit}>
            <input
              name="patente"
              value={form.patente}
              onChange={handleChange}
              placeholder="Patente"
            />
            <select
              name="marca"
              value={form.marca}
              onChange={handleChange}
              required
            >
              <option value="">Selecciona marca</option>
              {catalogoMarcas.map((marca) => (
                <option key={marca} value={marca}>{marca}</option>
              ))}
            </select>
            <select
              name="modelo"
              value={form.modelo}
              onChange={handleChange}
              required
              disabled={!form.marca}
            >
              <option value="">Selecciona modelo</option>
              {catalogoModelos.map((modelo) => (
                <option key={modelo} value={modelo}>{modelo}</option>
              ))}
            </select>
            <select
              name="version"
              value={form.version}
              onChange={handleChange}
              disabled={!form.modelo}
            >
              <option value="">Selecciona versión</option>
              {catalogoVersiones.map((version) => (
                <option key={version} value={version}>{version}</option>
              ))}
            </select>
            <select
              name="anio"
              value={form.anio}
              onChange={handleChange}
            >
              <option value="">Selecciona año</option>
              {catalogoAnios.map((anio) => (
                <option key={anio} value={anio}>{anio}</option>
              ))}
            </select>
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
                    setForm({ patente: '', marca: '', modelo: '', version: '', anio: '' })
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
          <h3>Buscar en catálogo local</h3>
          <form className="mini-form" onSubmit={handleCatalogoLookup}>
           <select
              value={catalogoForm.marca}
              onChange={(event) =>
                setCatalogoForm((prev) => ({ ...prev, marca: event.target.value, modelo: '', version: '' }))
              }
              required
            >
              <option value="">Selecciona marca</option>
              {catalogoMarcas.map((marca) => (
                <option key={marca} value={marca}>{marca}</option>
              ))}
            </select>
            <select
              value={catalogoForm.modelo}
              onChange={(event) =>
                setCatalogoForm((prev) => ({ ...prev, modelo: event.target.value, version: '' }))
              }
              required
            disabled={!catalogoForm.marca}
            >
              <option value="">Selecciona modelo</option>
              {catalogoModelosForm.map((modelo) => (
                <option key={modelo} value={modelo}>{modelo}</option>
              ))}
            </select>
            <select
              value={catalogoForm.version}
              onChange={(event) =>
                setCatalogoForm((prev) => ({ ...prev, version: event.target.value }))
              }
            disabled={!catalogoForm.modelo}
            >
              <option value="">Selecciona versión</option>
              {catalogoVersionesForm.map((version) => (
                <option key={version} value={version}>{version}</option>
              ))}
            </select>
            <button className="secondary" type="submit">Consultar</button>
          </form>
          {catalogoError ? <p className="inline-error">{catalogoError}</p> : null}
          {catalogoInfo ? (
            <div className="vin-result">
              <p><strong>{catalogoInfo.marca || 'Marca N/D'}</strong> {catalogoInfo.modelo || ''}</p>
              <p>Año: {catalogoInfo.anio || 'N/D'} · Combustible: {catalogoInfo.combustible || 'N/D'}</p>
              <p>Carrocería: {catalogoInfo.tipo_carroceria || 'N/D'} · País: {catalogoInfo.pais_origen || 'N/D'}</p>
            </div>
          ) : null}
        </article>
        <article className="page-card">
          <h3>Consultar por patente</h3>
          <form className="mini-form" onSubmit={handlePatenteLookup}>
            <input
              value={patenteBusqueda}
              onChange={(event) => setPatenteBusqueda(event.target.value)}
              placeholder="Patente (ej. AA123BB)"
            />
            <button className="secondary" type="submit">Buscar</button>
          </form>
          {patenteError ? <p className="inline-error">{patenteError}</p> : null}
          {patenteInfo ? (
            <div className="vin-result">
              <p><strong>{patenteInfo.patente || 'Patente N/D'}</strong></p>
              <p>{patenteInfo.marca} {patenteInfo.modelo} {patenteInfo.version || ''}</p>
              <p>Año: {patenteInfo.anio || 'N/D'} · Estado: {patenteInfo.estado}</p>
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
                    <span>{vehiculo.patente || 'Sin patente'}</span>
                    <span>Estado: {vehiculo.estado}</span>
                  </div>
                  <div className="list-actions">
                    <span>{vehiculo.anio || 'Año N/D'} · {vehiculo.version || 'Versión N/D'}</span>
                    <div className="list-buttons">
                      <button className="secondary" type="button" onClick={() => handleEdit(vehiculo)}>
                        Editar
                      </button>
                      <button className="secondary" type="button" onClick={() => handleDelete(vehiculo.id)}>
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

export default Vehiculos