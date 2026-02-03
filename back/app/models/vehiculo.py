from app.extensions import db
from app.models.base import BaseModel


class Vehiculo(BaseModel):
  __tablename__ = 'vehiculos'

  marca = db.Column(db.String(80), nullable=False)
  modelo = db.Column(db.String(80), nullable=False)
  version = db.Column(db.String(80))
  anio = db.Column(db.Integer)
  patente = db.Column(db.String(20), unique=True)
  estado = db.Column(db.String(40), default='disponible')
  cliente_id = db.Column(db.Integer, db.ForeignKey('clientes.id'))

  def __repr__(self):
    return f'<Vehiculo {self.marca} {self.modelo}>'