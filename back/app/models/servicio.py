from datetime import date

from app.extensions import db
from app.models.base import BaseModel


class Servicio(BaseModel):
  __tablename__ = 'servicios'

  vehiculo_id = db.Column(db.Integer, db.ForeignKey('vehiculos.id'), nullable=False)
  descripcion = db.Column(db.Text, nullable=False)
  fecha = db.Column(db.Date, default=date.today, nullable=False)
  kilometraje = db.Column(db.Integer)
  costo = db.Column(db.Numeric(12, 2), default=0)
  notas = db.Column(db.Text)

  vehiculo = db.relationship('Vehiculo', back_populates='servicios')

  def __repr__(self):
    return f'<Servicio {self.id} vehiculo={self.vehiculo_id}>'