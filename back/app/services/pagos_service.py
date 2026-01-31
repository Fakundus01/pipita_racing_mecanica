from app.extensions import db
from app.models.pago import Pago


class PagosService:
  def list(self):
    return Pago.query.order_by(Pago.created_at.desc()).all()

  def create(self, data):
    pago = Pago(**data)
    db.session.add(pago)
    db.session.commit()
    return pago