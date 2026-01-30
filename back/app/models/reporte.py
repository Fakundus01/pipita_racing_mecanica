from datetime import date

from app.extensions import db
from app.models.Base import BaseModel


class Reporte(BaseModel):
  __tablename__ = 'reportes'

  titulo = db.Column(db.String(120), nullable=False)
  periodo = db.Column(db.String(40))
  generado_el = db.Column(db.Date, default=date.today, nullable=False)

  def __repr__(self):
    return f'<Reporte {self.titulo}>'