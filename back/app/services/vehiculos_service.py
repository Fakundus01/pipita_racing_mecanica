from app.extensions import db
from app.models import Vehiculo


class VehiculosService:
  def list(self):
    return Vehiculo.query.order_by(Vehiculo.created_at.desc()).all()
  
  def get(self, vehiculo_id):
    return Vehiculo.query.get(vehiculo_id)

  def create(self, data):
    vehiculo = Vehiculo(**data)
    db.session.add(vehiculo)
    db.session.commit()
    return vehiculo
  
  def update(self, vehiculo, data):
    for key, value in data.items():
      setattr(vehiculo, key, value)
    db.session.commit()
    return vehiculo

  def delete(self, vehiculo):
    db.session.delete(vehiculo)
    db.session.commit()