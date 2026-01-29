function Reportes({ onAction }) {
  return (
    <section className="page">
      <header className="page-header">
        <div>
          <h1>Reportes y planillas</h1>
          <p>Genera PDFs listos para imprimir y compartir.</p>
        </div>
        <button className="primary" onClick={() => onAction('Reporte mensual generado.')}>Generar reporte</button>
      </header>
      <div className="page-grid">
        <article className="page-card">
          <h3>Resumen mensual</h3>
          <p>Ventas, ingresos y vehículos más consultados.</p>
          <button className="secondary" onClick={() => onAction('Resumen descargado.')}>Descargar resumen</button>
        </article>
        <article className="page-card">
          <h3>Planillas personalizadas</h3>
          <p>Crea formatos distintos para autos, motos o partes.</p>
          <button className="secondary" onClick={() => onAction('Plantilla guardada.')}>Editar plantilla</button>
        </article>
      </div>
    </section>
  )
}

export default Reportes