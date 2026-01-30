from datetime import date

from app.extensions import db
from app.models.Base import BaseModel


class Pago(BaseModel):
  __tablename__ = 'pagos'

  venta_id = db.Column(db.Integer, db.ForeignKey('ventas.id'), nullable=False)
  monto = db.Column(db.Numeric(12, 2), nullable=False)
  metodo = db.Column(db.String(40), default='efectivo')
  estado = db.Column(db.String(40), default='pendiente')
  fecha_pago = db.Column(db.Date, default=date.today, nullable=False)

  venta = db.relationship('Venta', back_populates='pagos')

  def __repr__(self):
    return f'<Pago {self.id} venta={self.venta_id}>'