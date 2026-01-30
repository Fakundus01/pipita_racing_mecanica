from app.extensions import db
from app.models import VentaParte


class VentasPartesService:
  def list(self):
    return VentaParte.query.order_by(VentaParte.created_at.desc()).all()

  def create(self, data):
    venta_parte = VentaParte(**data)
    db.session.add(venta_parte)
    db.session.commit()
    return venta_parte