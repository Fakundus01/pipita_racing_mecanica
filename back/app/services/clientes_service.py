from app.extensions import db
from app.models.cliente import Cliente

class ClientesService:
  def list(self):
    return Cliente.query.order_by(Cliente.created_at.desc()).all()
  
  def get(self, cliente_id):
    return Cliente.query.get(cliente_id)

  def create(self, data):
    cliente = Cliente(**data)
    db.session.add(cliente)
    db.session.commit()
    return cliente
  
  def update(self, cliente, data):
    for key, value in data.items():
      setattr(cliente, key, value)
    db.session.commit()
    return cliente

  def delete(self, cliente):
    db.session.delete(cliente)
    db.session.commit()