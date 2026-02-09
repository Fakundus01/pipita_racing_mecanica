import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  createCliente,
  createVehiculo,
  deleteCliente,
  listCatalogoAnios,
  listCatalogoMarcas,
  listCatalogoModelos,
  listCatalogoVersiones,
  listClientes,
  listVehiculos,
  updateCliente,
  updateVehiculo,
} from '../services/api'

const initialForm = {
  nombre: '',
  telefono: '',
  email: '',
  patente: '',
  marca: '',
  modelo: '',
  version: '',
  anio: '',
  vehiculo_id: '',
}

const toContactKey = (cliente) =>
  [cliente.nombre, cliente.email || '', cliente.telefono || '']
    .map((value) => String(value).trim().toLowerCase())
    .join('|')

function Clientes({ onAction, onAuthError }) {
  const [clientes, setClientes] = useState([])
  const [vehiculos, setVehiculos] = useState([])
  const [form, setForm] = useState(initialForm)
  const [showModal, setShowModal] = useState(false)
  const [editingCliente, setEditingCliente] = useState(null)
  const [catalogoMarcas, setCatalogoMarcas] = useState([])
  const [catalogoModelos, setCatalogoModelos] = useState([])
  const [catalogoVersiones, setCatalogoVersiones] = useState([])
  const [catalogoAnios, setCatalogoAnios] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const clientesAgrupados = useMemo(() => {
    const map = new Map()
    clientes.forEach((cliente) => {
      const key = toContactKey(cliente)
      const current = map.get(key) || {
        id: cliente.id,
        nombre: cliente.nombre,
        email: cliente.email,
        telefono: cliente.telefono,
        aliases: [],
        vehiculos: [],
      }
      current.aliases.push(cliente.id)
      current.vehiculos = [...current.vehiculos, ...(cliente.vehiculos || [])]
      map.set(key, current)
    })
    return [...map.values()].sort((a, b) => a.nombre.localeCompare(b.nombre))
  }, [clientes])

  const vehiculosSinCliente = useMemo(
    () => vehiculos.filter((vehiculo) => !vehiculo.cliente_id || (editingCliente && vehiculo.cliente_id === editingCliente.id)),
    [vehiculos, editingCliente],
  )

  const loadClientes = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [clientesData, vehiculosData] = await Promise.all([listClientes(), listVehiculos()])
      setClientes(clientesData)
      setVehiculos(vehiculosData)
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
     } finally {
      setLoading(false)
    }
  }, [onAuthError])

  useEffect(() => {
    loadClientes()
  }, [loadClientes])

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
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
        }
      }
    }
    loadModelos()
    return () => {
      cancelled = true
    }
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
        }
      } catch (err) {
        if (!cancelled) {
          setError(err.message)
        }
      }
    }
    loadVersiones()
    return () => {
      cancelled = true
    }
  }, [form.marca, form.modelo])

  const handleChange = (event) => {
    const { name, value } = event.target
    setForm((prev) => ({ ...prev, [name]: value }))
  }

  const openCreateModal = () => {
    setEditingCliente(null)
    setForm(initialForm)
    setShowModal(true)
  }

  const openEditModal = (cliente) => {
    const vehiculo = cliente.vehiculos?.[0]
    setEditingCliente(cliente)
    setForm({
      ...initialForm,
      nombre: cliente.nombre || '',
      telefono: cliente.telefono || '',
      email: cliente.email || '',
      vehiculo_id: vehiculo?.id ? String(vehiculo.id) : '',
      patente: vehiculo?.patente || '',
      marca: vehiculo?.marca || '',
      modelo: vehiculo?.modelo || '',
      version: vehiculo?.version || '',
      anio: vehiculo?.anio || '',
    })
    setShowModal(true)
  }

  const closeModal = () => {
    setShowModal(false)
    setEditingCliente(null)
    setForm(initialForm)
  }

  const handleVehiculoSelect = (value) => {
    const vehiculo = vehiculos.find((item) => String(item.id) === String(value))
    if (!vehiculo) {
      setForm((prev) => ({ ...prev, vehiculo_id: '', patente: '', marca: '', modelo: '', version: '', anio: '' }))
      return
    }
    setForm((prev) => ({
      ...prev,
      vehiculo_id: String(vehiculo.id),
      patente: vehiculo.patente || '',
      marca: vehiculo.marca || '',
      modelo: vehiculo.modelo || '',
      version: vehiculo.version || '',
      anio: vehiculo.anio || '',
    }))
  }

  const handleSubmit = async (event) => {
    event.preventDefault()
    setError('')
    try {
      if (editingCliente) {
        const actualizado = await updateCliente(editingCliente.id, {
          nombre: form.nombre,
          telefono: form.telefono,
          email: form.email,
        })

        if (form.vehiculo_id) {
          await updateVehiculo(Number(form.vehiculo_id), {
            patente: form.patente || null,
            marca: form.marca,
            modelo: form.modelo,
            version: form.version || null,
            anio: form.anio ? Number(form.anio) : null,
            cliente_id: editingCliente.id,
          })
        } else if (form.marca && form.modelo) {
          await createVehiculo({
            patente: form.patente || null,
            marca: form.marca,
            modelo: form.modelo,
            version: form.version || null,
            anio: form.anio ? Number(form.anio) : null,
            cliente_id: editingCliente.id,
          })
        }

        await loadClientes()
        closeModal()
        onAction(`Cliente ${actualizado.nombre} actualizado.`)
        return
      }

      const payload = {
        nombre: form.nombre,
        telefono: form.telefono,
        email: form.email,
        vehiculo:
          form.marca && form.modelo
            ? {
                patente: form.patente || null,
                marca: form.marca,
                modelo: form.modelo,
                version: form.version || null,
                anio: form.anio ? Number(form.anio) : null,
              }
            : null,
      }
      const nuevo = await createCliente(payload)
      setClientes((prev) => [nuevo, ...prev])
      closeModal()
      onAction(`Cliente ${nuevo.nombre} creado con su vehículo.`)
    } catch (err) {
      setError(err.message)
      if (err.status === 401) {
        onAuthError()
      }
    }
  }

  const handleDeleteCliente = async (clienteId) => {
    if (!window.confirm('¿Eliminar cliente y sus vehículos asociados?')) return
    setError('')
    try {
      await deleteCliente(clienteId)
      await loadClientes()
      onAction('Cliente eliminado.')
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Clientes activos</h1>
          <p>Fichas de clientes con edición y lista de vehículos por persona.</p>
        </div>
        <button className="primary" onClick={openCreateModal}>
          Cargar cliente
        </button>
      </header>

      {error ? <p className="inline-error">{error}</p> : null}

      <article className="page-card">
        <h3>Historial de vehículos por cliente</h3>
        {loading ? <p>Cargando clientes...</p> : null}
        {!loading && clientesAgrupados.length === 0 ? (
          <p>No hay clientes registrados.</p>
        ) : (
          <ul className="data-list data-list-stacked">
            {clientesAgrupados.map((cliente) => (
              <li key={`${cliente.id}-${cliente.aliases.join('-')}`}>
                <div>
                  <strong>{cliente.nombre}</strong>
                  <span>{cliente.email || 'Sin email'} · {cliente.telefono || 'Sin teléfono'}</span>
                  {cliente.vehiculos.length === 0 ? (
                    <span>Sin vehículos asociados.</span>
                  ) : (
                    <div className="vehicle-history-inline">
                      {cliente.vehiculos.map((vehiculo) => (
                        <span key={vehiculo.id}>
                          {vehiculo.patente || 'Sin patente'} · {vehiculo.marca} {vehiculo.modelo} · {vehiculo.version || 'Sin versión'} · {vehiculo.anio || 'Año N/D'}
                        </span>
                      ))}
                    </div>
                  )}
                </div>
                <div className="list-buttons">
                  <button className="secondary" type="button" onClick={() => openEditModal(cliente)}>
                    Editar cliente
                  </button>
                  <button className="secondary" type="button" onClick={() => handleDeleteCliente(cliente.id)}>
                    Eliminar cliente
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </article>

      {showModal ? (
        <div className="modal-backdrop" onClick={closeModal}>
          <article className="modal-card" onClick={(event) => event.stopPropagation()}>
            <div className="modal-header">
              <h3>{editingCliente ? 'Editar cliente' : 'Nuevo cliente'}</h3>
              <button className="secondary" type="button" onClick={closeModal}>Cerrar</button>
            </div>
            <form className="mini-form" onSubmit={handleSubmit}>
              <input name="nombre" value={form.nombre} onChange={handleChange} placeholder="Nombre" required />
              <input name="telefono" value={form.telefono} onChange={handleChange} placeholder="Teléfono" />
              <input name="email" value={form.email} onChange={handleChange} placeholder="Email" />

              {editingCliente ? (
                <>
                  <h4>Vehículo asociado</h4>
                  <select name="vehiculo_id" value={form.vehiculo_id} onChange={(e) => handleVehiculoSelect(e.target.value)}>
                    <option value="">Crear nuevo o dejar sin vehículo</option>
                    {vehiculosSinCliente.map((vehiculo) => (
                      <option key={vehiculo.id} value={vehiculo.id}>
                        {vehiculo.patente || 'Sin patente'} · {vehiculo.marca} {vehiculo.modelo}
                      </option>
                    ))}
                  </select>
                </>
              ) : null}

               <input name="patente" value={form.patente} onChange={handleChange} placeholder="Patente" />
              <select name="marca" value={form.marca} onChange={handleChange}>
                <option value="">Selecciona marca</option>
                {catalogoMarcas.map((marca) => (
                  <option key={marca} value={marca}>{marca}</option>
                ))}
              </select>
              <select name="modelo" value={form.modelo} onChange={handleChange} disabled={!form.marca}>
                <option value="">Selecciona modelo</option>
                {catalogoModelos.map((modelo) => (
                  <option key={modelo} value={modelo}>{modelo}</option>
                ))}
              </select>
              <select name="version" value={form.version} onChange={handleChange} disabled={!form.modelo}>
                <option value="">Selecciona versión</option>
                {catalogoVersiones.map((version) => (
                  <option key={version} value={version}>{version}</option>
                ))}
              </select>
              <select name="anio" value={form.anio} onChange={handleChange}>
                <option value="">Selecciona año</option>
                {catalogoAnios.map((anio) => (
                  <option key={anio} value={anio}>{anio}</option>
                ))}
              </select>

              <button className="primary" type="submit">{editingCliente ? 'Guardar cambios' : 'Guardar cliente'}</button>
            </form>
          </article>
        </div>
      ) : null}
    </section>
  )
}

export default Clientes