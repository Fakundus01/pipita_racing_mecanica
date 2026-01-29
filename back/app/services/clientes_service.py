from app.extensions import db
from app.models import Cliente


class ClientesService:
  def list(self):
    return Cliente.query.order_by(Cliente.created_at.desc()).all()

  def create(self, data):
    cliente = Cliente(**data)
    db.session.add(cliente)
    db.session.commit()
    return cliente