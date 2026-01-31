from app.extensions import db
from app.models.parte import Parte


class PartesService:
  def list(self):
    return Parte.query.order_by(Parte.created_at.desc()).all()
  
  def get(self, parte_id):
    return Parte.query.get(parte_id)

  def create(self, data):
    parte = Parte(**data)
    db.session.add(parte)
    db.session.commit()
    return parte
  
  def update(self, parte, data):
    for key, value in data.items():
      setattr(parte, key, value)
    db.session.commit()
    return parte

  def delete(self, parte):
    db.session.delete(parte)
    db.session.commit()