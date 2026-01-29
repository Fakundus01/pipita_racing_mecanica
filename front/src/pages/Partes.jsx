function Partes({ onAction }) {
  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Partes & accesorios</h1>
          <p>Controla stock, costos y partes vinculadas a cada venta.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nuevo repuesto agregado.')}>Agregar repuesto</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Inventario crítico</h3>
          <p>2 piezas por debajo del stock mínimo.</p>
          <button className="secondary" onClick={() => onAction('Alerta de stock enviada.')}>Reponer stock</button>
        </article>
        <article className="page-card">
          <h3>Paquetes destacados</h3>
          <p>Kits de mantenimiento listos para imprimir.</p>
          <button className="secondary" onClick={() => onAction('Paquetes actualizados.')}>Editar paquetes</button>
        </article>
      </div>
    </section>
  )
}

export default Partes