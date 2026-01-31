from app.extensions import db
from app.models.base import BaseModel


class VentaParte(BaseModel):
  __tablename__ = 'ventas_partes'

  venta_id = db.Column(db.Integer, db.ForeignKey('ventas.id'), nullable=False)
  parte_id = db.Column(db.Integer, db.ForeignKey('partes.id'), nullable=False)
  cantidad = db.Column(db.Integer, nullable=False, default=1)
  precio_unitario = db.Column(db.Numeric(12, 2), nullable=False, default=0)

  venta = db.relationship('Venta', back_populates='partes')
  parte = db.relationship('Parte', back_populates='ventas')

  def __repr__(self):
    return f'<VentaParte venta={self.venta_id} parte={self.parte_id}>'