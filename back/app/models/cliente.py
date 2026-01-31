from app.extensions import db
from app.models.base import BaseModel



class Cliente(BaseModel):
  __tablename__ = 'clientes'

  nombre = db.Column(db.String(120), nullable=False)
  telefono = db.Column(db.String(40))
  email = db.Column(db.String(120))
  estado = db.Column(db.String(40), default='activo')

  def __repr__(self):
    return f'<Cliente {self.nombre}>'