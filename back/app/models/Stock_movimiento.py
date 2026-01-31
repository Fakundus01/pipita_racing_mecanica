from datetime import date

from app.extensions import db
from app.models.base import BaseModel


class StockMovimiento(BaseModel):
  __tablename__ = 'stock_movimientos'

  parte_id = db.Column(db.Integer, db.ForeignKey('partes.id'), nullable=False)
  venta_id = db.Column(db.Integer, db.ForeignKey('ventas.id'))
  tipo = db.Column(db.String(20), nullable=False)
  cantidad = db.Column(db.Integer, nullable=False)
  motivo = db.Column(db.String(120))
  fecha = db.Column(db.Date, default=date.today, nullable=False)

  parte = db.relationship('Parte', back_populates='movimientos')

  def __repr__(self):
    return f'<StockMovimiento {self.id} parte={self.parte_id}>'