function Clientes({ onAction }) {
  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Clientes activos</h1>
          <p>Gestiona datos, estado y preferencias de tus compradores.</p>
        </div>
        <button className="primary" onClick={() => onAction('Nuevo cliente creado.')}>Nuevo cliente</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Pipeline de clientes</h3>
          <p>3 contactos en seguimiento, 2 listos para cerrar.</p>
          <button className="secondary" onClick={() => onAction('Vista de seguimiento abierta.')}>Ver seguimiento</button>
        </article>
        <article className="page-card">
          <h3>Preferencias guardadas</h3>
          <p>Combustible, precio objetivo y marcas favoritas por cliente.</p>
          <button className="secondary" onClick={() => onAction('Preferencias actualizadas.')}>Actualizar preferencias</button>
        </article>
      </div>
    </section>
  )
}

export default Clientes