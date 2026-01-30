from flask import Blueprint, current_app, jsonify, request, session

from app.services.clientes_service import ClientesService
from app.services.partes_service import PartesService
from app.services.reportes_service import ReportesService
from app.services.vehiculos_service import VehiculosService
from app.services.vehiculos_api_service import VehiculosApiService

api = Blueprint('api', __name__)

clientes_service = ClientesService()
vehiculos_service = VehiculosService()
partes_service = PartesService()
reportes_service = ReportesService()


def is_authenticated():
  return session.get('admin_authenticated') is True


def vehiculos_api():
  return VehiculosApiService(current_app.config['VEHICULOS_API_BASE'])


@api.before_request
def require_authentication():
  public_paths = {
    '/api/health',
    '/api/auth/login',
    '/api/auth/logout',
    '/api/auth/me',
  }
  if request.method == 'OPTIONS' or request.path in public_paths:
    return None
  if not is_authenticated():
    return jsonify({'error': 'No autorizado'}), 401
  return None


@api.get('/health')
def health_check():
  return jsonify({'status': 'ok'})


@api.post('/auth/login')
def login():
  payload = request.get_json(force=True)
  username = payload.get('username', '')
  password = payload.get('password', '')
  if (
    username == current_app.config['ADMIN_USERNAME']
    and password == current_app.config['ADMIN_PASSWORD']
  ):
    session.clear()
    session['admin_authenticated'] = True
    session['admin_username'] = username
    return jsonify({'authenticated': True, 'username': username})
  return jsonify({'authenticated': False, 'error': 'Credenciales inválidas'}), 401


@api.post('/auth/logout')
def logout():
  session.clear()
  return jsonify({'authenticated': False})


@api.get('/auth/me')
def me():
  return jsonify(
    {
      'authenticated': is_authenticated(),
      'username': session.get('admin_username'),
    }
  )


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


@api.get('/vehiculos/decodificar')
def decode_vehiculo_vin():
  vin = request.args.get('vin', '').strip()
  if not vin:
    return jsonify({'error': 'VIN requerido'}), 400
  try:
    data = vehiculos_api().decode_vin(vin)
  except Exception as exc:
    return jsonify({'error': 'No se pudo consultar el VIN', 'detail': str(exc)}), 502
  return jsonify(data)


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