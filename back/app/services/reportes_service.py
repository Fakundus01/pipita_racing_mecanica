from app.extensions import db
from app.models.reporte import Reporte


class ReportesService:
  def list(self):
    return Reporte.query.order_by(Reporte.created_at.desc()).all()
  
  def get(self, reporte_id):
    return Reporte.query.get(reporte_id)

  def create(self, data):
    reporte = Reporte(**data)
    db.session.add(reporte)
    db.session.commit()
    return reporte
  
  def update(self, reporte, data):
    for key, value in data.items():
      setattr(reporte, key, value)
    db.session.commit()
    return reporte

  def delete(self, reporte):
    db.session.delete(reporte)
    db.session.commit()