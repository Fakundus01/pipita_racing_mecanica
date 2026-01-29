from app.extensions import db
from app.models.base import BaseModel


class Parte(BaseModel):
  __tablename__ = 'partes'

  nombre = db.Column(db.String(120), nullable=False)
  stock = db.Column(db.Integer, default=0)
  costo = db.Column(db.Numeric(12, 2), default=0)

  def __repr__(self):
    return f'<Parte {self.nombre}>'