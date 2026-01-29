function Vehiculos({ onAction }) {
  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Vehículos en catálogo</h1>
          <p>Organiza inventario y carga fichas con datos de API.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nueva ficha de vehículo iniciada.')}>Nueva ficha</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Sincronizar API</h3>
          <p>Autocompleta modelos, versiones y valores estimados.</p>
          <button className="secondary" onClick={() => onAction('Sincronización programada.')}>Sincronizar</button>
        </article>
        <article className="page-card">
          <h3>Checklist de revisión</h3>
          <p>Estado mecánico, estética, documentación y fotos.</p>
          <button className="secondary" onClick={() => onAction('Checklist actualizado.')}>Editar checklist</button>
        </article>
      </div>
    </section>
  )
}

export default Vehiculos