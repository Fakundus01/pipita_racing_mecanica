from datetime import date

from app.extensions import db
from app.models.Base import BaseModel


class Venta(BaseModel):
  __tablename__ = 'ventas'

  cliente_id = db.Column(db.Integer, db.ForeignKey('clientes.id'), nullable=False)
  vehiculo_id = db.Column(db.Integer, db.ForeignKey('vehiculos.id'), nullable=False)
  precio_total = db.Column(db.Numeric(12, 2), nullable=False)
  estado = db.Column(db.String(40), default='en_proceso')
  fecha_venta = db.Column(db.Date, default=date.today, nullable=False)
  notas = db.Column(db.Text)

  cliente = db.relationship('Cliente', back_populates='ventas')
  vehiculo = db.relationship('Vehiculo', back_populates='ventas')
  pagos = db.relationship('Pago', back_populates='venta', cascade='all, delete-orphan')
  partes = db.relationship('VentaParte', back_populates='venta', cascade='all, delete-orphan')

  def __repr__(self):
    return f'<Venta {self.id}>'