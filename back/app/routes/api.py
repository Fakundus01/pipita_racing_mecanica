from flask import Blueprint, jsonify, request

from app.services.clientes_service import ClientesService
from app.services.partes_service import PartesService
from app.services.reportes_service import ReportesService
from app.services.vehiculos_service import VehiculosService

api = Blueprint('api', __name__)

clientes_service = ClientesService()
vehiculos_service = VehiculosService()
partes_service = PartesService()
reportes_service = ReportesService()


@api.get('/health')
def health_check():
  return jsonify({'status': 'ok'})


@api.get('/clientes')
def list_clientes():
  clientes = clientes_service.list()
  return jsonify([cliente.to_dict() for cliente in clientes])


@api.post('/clientes')
def create_cliente():
  payload = request.get_json(force=True)
  cliente = clientes_service.create(payload)
  return jsonify(cliente.to_dict()), 201


@api.get('/vehiculos')
def list_vehiculos():
  vehiculos = vehiculos_service.list()
  return jsonify([vehiculo.to_dict() for vehiculo in vehiculos])


@api.post('/vehiculos')
def create_vehiculo():
  payload = request.get_json(force=True)
  vehiculo = vehiculos_service.create(payload)
  return jsonify(vehiculo.to_dict()), 201


@api.get('/partes')
def list_partes():
  partes = partes_service.list()
  return jsonify([parte.to_dict() for parte in partes])


@api.post('/partes')
def create_parte():
  payload = request.get_json(force=True)
  parte = partes_service.create(payload)
  return jsonify(parte.to_dict()), 201


@api.get('/reportes')
def list_reportes():
  reportes = reportes_service.list()
  return jsonify([reporte.to_dict() for reporte in reportes])


@api.post('/reportes')
def create_reporte():
  payload = request.get_json(force=True)
  reporte = reportes_service.create(payload)
  return jsonify(reporte.to_dict()), 201