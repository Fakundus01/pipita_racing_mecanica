from app.extensions import db
from app.models.servicio import Servicio


class ServiciosService:
  def list(self):
    return Servicio.query.order_by(Servicio.fecha.desc(), Servicio.created_at.desc()).all()

  def list_by_vehiculo(self, vehiculo_id):
    return (
      Servicio.query.filter_by(vehiculo_id=vehiculo_id)
      .order_by(Servicio.fecha.desc(), Servicio.created_at.desc())
      .all()
    )

  def get(self, servicio_id):
    return Servicio.query.get(servicio_id)

  def create(self, data):
    servicio = Servicio(**data)
    db.session.add(servicio)
    db.session.commit()
    return servicio

  def update(self, servicio, data):
    for key, value in data.items():
      setattr(servicio, key, value)
    db.session.commit()
    return servicio

  def delete(self, servicio):
    db.session.delete(servicio)
    db.session.commit()