const stats = [
  { label: 'Ventas del mes', value: '12', detail: '3 en negociación' },
  { label: 'Vehículos en stock', value: '8', detail: 'Autos, camionetas y motos' },
  { label: 'Partes registradas', value: '26', detail: 'Motores, frenos, accesorios' },
]

const recentNotes = [
  {
    title: 'Toyota Hilux 2021 · Diésel',
    detail: 'Cliente: Rodrigo S. · $28.900.000 · Entrega en 3 días',
    tags: ['Camioneta', 'Reservado'],
  },
  {
    title: 'Honda CB500F 2019 · 18.400 km',
    detail: 'Cliente: Valentina R. · $4.950.000 · Transferencia en curso',
    tags: ['Moto', 'Documentación'],
  },
  {
    title: 'Chevrolet Onix 2022 · AT',
    detail: 'Cliente: Laura M. · $13.700.000 · Pendiente de pago',
    tags: ['Auto', 'Seguimiento'],
  },
]

const partsList = [
  'Kit de frenos Brembo',
  'Juego de neumáticos 17"',
  'Parachoques delantero',
  'Kit de mantenimiento básico',
]

function Home({ onAction }) {
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
          <button className="primary" onClick={() => onAction('Nueva venta creada.')}>Nueva venta</button>
        </div>
      </header>

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
          <h2>Últimas ventas</h2>
          <div className="notes-list">
            {recentNotes.map((note) => (
              <div className="note" key={note.title}>
                <div>
                  <h3>{note.title}</h3>
                  <p>{note.detail}</p>
                  <div className="tags">
                    {note.tags.map((tag) => (
                      <span key={tag}>{tag}</span>
                    ))}
                  </div>
                </div>
                <button className="icon-button" onClick={() => onAction(`Abrimos ${note.title}.`)}>
                  ↗
                </button>
              </div>
            ))}
          </div>
          <div className="parts">
            <h3>Partes destacadas</h3>
            <ul>
              {partsList.map((part) => (
                <li key={part}>{part}</li>
              ))}
            </ul>
            <button className="secondary" onClick={() => onAction('Listado de partes actualizado.')}
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