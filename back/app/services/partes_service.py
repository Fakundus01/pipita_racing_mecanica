from app.extensions import db
from app.models import Parte


class PartesService:
  def list(self):
    return Parte.query.order_by(Parte.created_at.desc()).all()

  def create(self, data):
    parte = Parte(**data)
    db.session.add(parte)
    db.session.commit()
    return parte