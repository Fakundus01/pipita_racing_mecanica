from app.models.cliente import Cliente
from app.models.pago import Pago
from app.models.parte import Parte
from app.models.reporte import Reporte
from app.models.stock_movimiento import StockMovimiento
from app.models.vehiculo import Vehiculo
from app.models.venta import Venta
from app.models.venta_parte import VentaParte


__all__ = [
  'Cliente',
  'Vehiculo',
  'Parte',
  'Reporte',
  'Venta',
  'Pago',
  'VentaParte',
  'StockMovimiento',
]