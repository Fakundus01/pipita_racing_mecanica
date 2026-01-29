from app.extensions import db
from app.models import Vehiculo


class VehiculosService:
  def list(self):
    return Vehiculo.query.order_by(Vehiculo.created_at.desc()).all()

  def create(self, data):
    vehiculo = Vehiculo(**data)
    db.session.add(vehiculo)
    db.session.commit()
    return vehiculo