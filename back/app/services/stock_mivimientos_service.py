from app.extensions import db
from app.models.stock_movimiento import StockMovimiento


class StockMovimientosService:
  def list(self):
    return StockMovimiento.query.order_by(StockMovimiento.created_at.desc()).all()

  def create(self, data):
    movimiento = StockMovimiento(**data)
    db.session.add(movimiento)
    db.session.commit()
    return movimiento