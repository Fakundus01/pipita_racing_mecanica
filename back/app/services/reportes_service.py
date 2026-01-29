from app.extensions import db
from app.models import Reporte


class ReportesService:
  def list(self):
    return Reporte.query.order_by(Reporte.created_at.desc()).all()

  def create(self, data):
    reporte = Reporte(**data)
    db.session.add(reporte)
    db.session.commit()
    return reporte