from app.extensions import db
from app.models.venta import Venta


class VentasService:
  def list(self):
    return Venta.query.order_by(Venta.created_at.desc()).all()

  def create(self, data):
    venta = Venta(**data)
    db.session.add(venta)
    db.session.commit()
    return venta