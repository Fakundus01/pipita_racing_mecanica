import { useEffect, useState } from 'react'
import { downloadExcel, getDashboard } from '../services/api'

function Home({ onAction, onAuthError }) {
  const [dashboard, setDashboard] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState('')

  useEffect(() => {
      let cancelled = false
      const loadDashboard = async () => {
        setLoading(true)
        setError('')
        try {
          const data = await getDashboard()
          if (!cancelled) {
            setDashboard(data)
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
      loadDashboard()
      return () => {
        cancelled = true
      }
    }, [onAuthError])

    const handleExport = async () => {
      setExportError('')
      setExporting(true)
      try {
        const blob = await downloadExcel()
        const url = window.URL.createObjectURL(blob)
        const link = document.createElement('a')
        link.href = url
        link.download = 'pipita-datos.xlsx'
        document.body.appendChild(link)
        link.click()
        link.remove()
        window.URL.revokeObjectURL(url)
        onAction('Exportación generada en Excel.')
      } catch (err) {
        setExportError(err.message)
        if (err.status === 401) {
          onAuthError()
        }
      } finally {
        setExporting(false)
      }
    }

    const stats = [
      {
        label: 'Clientes activos',
        value: dashboard?.stats?.clientes ?? 0,
        detail: 'Base total registrada',
      },
      {
        label: 'Vehículos en stock',
        value: dashboard?.stats?.vehiculos ?? 0,
        detail: 'Autos, camionetas y motos',
      },
      {
        label: 'Partes registradas',
        value: dashboard?.stats?.partes ?? 0,
        detail: 'Repuestos y accesorios',
      },
    ]

    const recentVehiculos = dashboard?.recent_vehiculos ?? []
    const recentReportes = dashboard?.recent_reportes ?? []
    
    return (
      <section className="home">
        <header className="header">
          <div>
            <h1>Panel de anotaciones</h1>
            <p>
              Centraliza ventas de autos, camionetas, motos y sus partes en un
              formato listo para imprimir.
            </p>
          </div>
          <div className="header-actions">
            <button className="secondary" onClick={() => onAction('Plantilla abierta.')}>Ver plantilla</button>
            <button
              className="secondary"
              onClick={handleExport}
              disabled={exporting}
            >
              {exporting ? 'Exportando...' : 'Exportar Excel'}
            </button>
            <button className="primary" onClick={() => onAction('Nueva venta creada.')}>Nueva venta</button>
          </div>
        </header>
        {exportError ? <p className="inline-error">{exportError}</p> : null}

        <section className="stats">
          {stats.map((stat) => (
            <article className="stat-card" key={stat.label}>
              <p className="stat-label">{stat.label}</p>
              <p className="stat-value">{stat.value}</p>
              <p className="stat-detail">{stat.detail}</p>
            </article>
          ))}
        </section>

        <section className="content-grid">
          <article className="form-card">
            <div className="card-header">
              <div>
                <h2>Nueva anotación</h2>
                <p>
                  Carga ventas y partes con autocompletado para evitar planillas
                  manuales.
                </p>
              </div>
              <span className="badge">Borrador</span>
            </div>
            <form className="form-grid">
              <label>
                Cliente
                <input placeholder="Nombre completo" />
              </label>
              <label>
                Tipo de vehículo
                <select>
                  <option>Auto</option>
                  <option>Camioneta</option>
                  <option>Moto</option>
                </select>
              </label>
              <label>
                Marca
                <input placeholder="Toyota, Ford, Honda" />
              </label>
              <label>
                Modelo
                <input placeholder="Hilux, Ranger, CB500" />
              </label>
              <label>
                Año
                <input placeholder="2022" />
              </label>
              <label>
                Kilometraje
                <input placeholder="18.400 km" />
              </label>
              <label>
                Precio de venta
                <input placeholder="$" />
              </label>
              <label>
                Estado
                <select>
                  <option>Reservado</option>
                  <option>En negociación</option>
                  <option>Pendiente de pago</option>
                  <option>Vendido</option>
                </select>
              </label>
              <label className="full">
                Partes incluidas
                <input placeholder="Kit de frenos, neumáticos, accesorios" />
              </label>
              <label className="full">
                Observaciones
                <textarea placeholder="Detalles de pago, entrega o documentación" />
              </label>
            </form>
            <div className="form-actions">
              <button className="secondary" type="button" onClick={() => onAction('Borrador guardado.')}>Guardar borrador</button>
              <button className="primary" type="button" onClick={() => onAction('Planilla enviada a imprimir.')}>Imprimir planilla</button>
            </div>
          </article>

          <aside className="notes-card">
            <h2>Actividad reciente</h2>
          {loading ? <p>Cargando resumen...</p> : null}
          {error ? <p className="inline-error">{error}</p> : null}
          {!loading && !error ? (
            <div className="notes-list">
              {recentVehiculos.length === 0 ? (
                <p>No hay vehículos recientes.</p>
              ) : (
                recentVehiculos.map((vehiculo) => (
                  <div className="note" key={vehiculo.id}>
                    <div>
                      <h3>{vehiculo.marca} {vehiculo.modelo}</h3>
                      <p>Año {vehiculo.anio || 'N/D'} · Estado {vehiculo.estado}</p>
                      <div className="tags">
                        <span>Vehículo</span>
                        <span>Inventario</span>
                      </div>
                    </div>
                    <button
                      className="icon-button"
                      onClick={() => onAction(`Abrimos ${vehiculo.marca} ${vehiculo.modelo}.`)}
                    >
                      ↗
                    </button>
                    </div>
                 ))
              )}
            </div>
          ) : null}
            <div className="parts">
               <h3>Últimos reportes</h3>
            {recentReportes.length === 0 ? (
              <p>No hay reportes recientes.</p>
            ) : (
              <ul>
                {recentReportes.map((reporte) => (
                  <li key={reporte.id}>{reporte.titulo}</li>
                ))}
              </ul>
            )}
            <button
              className="secondary"
              onClick={() => onAction('Resumen actualizado desde el backend.')}
              >
                Actualizar lista
              </button>
            </div>
          </aside>
        </section>
      </section>
    )
  }

export default Home