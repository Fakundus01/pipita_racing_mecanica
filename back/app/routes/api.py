from decimal import Decimal
from io import BytesIO
from datetime import date, datetime

from flask import Blueprint, current_app, jsonify, make_response, request, send_file, session
from sqlalchemy import func #type: ignore
from openpyxl import Workbook #type: ignore

from app.extensions import db
from app.models.cliente import Cliente
from app.models.parte import Parte
from app.models.reporte import Reporte
from app.models.vehiculo import Vehiculo

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
  return VehiculosApiService(
    current_app.config['VEHICULOS_API_JSON_PATH'],
  )

def serialize_excel_value(value):
  if isinstance(value, (datetime, date)):
    return value.isoformat()
  if isinstance(value, Decimal):
    return float(value)
  return value


def add_sheet(workbook, title, records):
  worksheet = workbook.create_sheet(title=title)
  if not records:
    worksheet.append(['Sin datos'])
    return

  columns = [column.name for column in records[0].__table__.columns]
  worksheet.append(columns)
  for record in records:
    row = [serialize_excel_value(getattr(record, column)) for column in columns]
    worksheet.append(row)


def normalize_vehiculo_payload(payload):
  payload = dict(payload)
  if payload.get('patente') == '':
    payload['patente'] = None
  if payload.get('version') == '':
    payload['version'] = None
  if payload.get('anio') == '':
    payload['anio'] = None
  return payload


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


def serialize_cliente(cliente):
  data = cliente.to_dict()
  data['vehiculos'] = [vehiculo.to_dict() for vehiculo in cliente.vehiculos]
  return data


@api.get('/clientes')
def list_clientes():
  clientes = clientes_service.list()
  return jsonify([serialize_cliente(cliente) for cliente in clientes])


@api.get('/clientes/<int:cliente_id>')
def get_cliente(cliente_id):
  cliente = clientes_service.get(cliente_id)
  if not cliente:
    return jsonify({'error': 'Cliente no encontrado'}), 404
  return jsonify(serialize_cliente(cliente))


@api.post('/clientes')
def create_cliente():
  payload = request.get_json(force=True)
  vehiculo_payload = payload.pop('vehiculo', None)

  cliente = Cliente(**payload)
  db.session.add(cliente)
  db.session.flush()

  if vehiculo_payload:
    vehiculo_payload = normalize_vehiculo_payload(vehiculo_payload)
    vehiculo_payload = {**vehiculo_payload, 'cliente_id': cliente.id}
    vehiculo = Vehiculo(**vehiculo_payload)
    db.session.add(vehiculo)

  db.session.commit()
  return jsonify(serialize_cliente(cliente)), 201


@api.put('/clientes/<int:cliente_id>')
@api.patch('/clientes/<int:cliente_id>')
def update_cliente(cliente_id):
  cliente = clientes_service.get(cliente_id)
  if not cliente:
    return jsonify({'error': 'Cliente no encontrado'}), 404
  payload = request.get_json(force=True)
  cliente = clientes_service.update(cliente, payload)
  return jsonify(serialize_cliente(cliente))


@api.delete('/clientes/<int:cliente_id>')
def delete_cliente(cliente_id):
  cliente = clientes_service.get(cliente_id)
  if not cliente:
    return jsonify({'error': 'Cliente no encontrado'}), 404
  clientes_service.delete(cliente)
  return jsonify({'deleted': True})


@api.get('/vehiculos')
def list_vehiculos():
  vehiculos = vehiculos_service.list()
  return jsonify([vehiculo.to_dict() for vehiculo in vehiculos])


@api.get('/vehiculos/<int:vehiculo_id>')
def get_vehiculo(vehiculo_id):
  vehiculo = vehiculos_service.get(vehiculo_id)
  if not vehiculo:
    return jsonify({'error': 'Vehículo no encontrado'}), 404
  return jsonify(vehiculo.to_dict())


@api.get('/vehiculos/patente/<string:patente>')
def get_vehiculo_by_patente(patente):
  vehiculo = Vehiculo.query.filter(
    func.lower(Vehiculo.patente) == patente.lower()
  ).first()
  if not vehiculo:
    return jsonify({'error': 'Vehículo no encontrado'}), 404
  return jsonify(vehiculo.to_dict())


@api.get('/vehiculos/catalogo/marcas')
def list_catalogo_marcas():
  marcas = vehiculos_api().list_brands()
  return jsonify({'marcas': marcas})


@api.get('/vehiculos/catalogo/modelos')
def list_catalogo_modelos():
  marca = request.args.get('marca', '').strip()
  if not marca:
    return jsonify({'error': 'Marca requerida'}), 400
  modelos = vehiculos_api().list_models(marca)
  return jsonify({'modelos': modelos})


@api.get('/vehiculos/catalogo/versiones')
def list_catalogo_versiones():
  marca = request.args.get('marca', '').strip()
  modelo = request.args.get('modelo', '').strip()
  if not marca or not modelo:
    return jsonify({'error': 'Marca y modelo requeridos'}), 400
  versiones = vehiculos_api().list_versions(marca, modelo)
  return jsonify({'versiones': versiones})


@api.get('/vehiculos/catalogo/anios')
def list_catalogo_anios():
  current_year = datetime.utcnow().year
  anios = list(range(current_year, 1989, -1))
  return jsonify({'anios': anios})


@api.post('/vehiculos')
def create_vehiculo():
  payload = request.get_json(force=True)
  payload = normalize_vehiculo_payload(payload)
  vehiculo = vehiculos_service.create(payload)
  return jsonify(vehiculo.to_dict()), 201


@api.put('/vehiculos/<int:vehiculo_id>')
@api.patch('/vehiculos/<int:vehiculo_id>')
def update_vehiculo(vehiculo_id):
  vehiculo = vehiculos_service.get(vehiculo_id)
  if not vehiculo:
    return jsonify({'error': 'Vehículo no encontrado'}), 404
  payload = request.get_json(force=True)
  payload = normalize_vehiculo_payload(payload)
  vehiculo = vehiculos_service.update(vehiculo, payload)
  return jsonify(vehiculo.to_dict())


@api.delete('/vehiculos/<int:vehiculo_id>')
def delete_vehiculo(vehiculo_id):
  vehiculo = vehiculos_service.get(vehiculo_id)
  if not vehiculo:
    return jsonify({'error': 'Vehículo no encontrado'}), 404
  vehiculos_service.delete(vehiculo)
  return jsonify({'deleted': True})


@api.get('/vehiculos/decodificar')
def decode_vehiculo_vin():
  vin = request.args.get('vin', '').strip()
  make = request.args.get('make', '').strip()
  model = request.args.get('model', '').strip()
  trim = request.args.get('trim', '').strip()
  if not make or not model:
    return jsonify({'error': 'Marca y modelo requeridos.'}), 400
  try:
    data = vehiculos_api().search_cars(make, model, trim)
  except Exception as exc:
    return jsonify({'error': 'No se pudo consultar Ninja Cars', 'detail': str(exc)}), 502
  return jsonify(data)


@api.get('/partes')
def list_partes():
  partes = partes_service.list()
  return jsonify([parte.to_dict() for parte in partes])


@api.get('/partes/<int:parte_id>')
def get_parte(parte_id):
  parte = partes_service.get(parte_id)
  if not parte:
    return jsonify({'error': 'Parte no encontrada'}), 404
  return jsonify(parte.to_dict())


@api.post('/partes')
def create_parte():
  payload = request.get_json(force=True)
  parte = partes_service.create(payload)
  return jsonify(parte.to_dict()), 201


@api.put('/partes/<int:parte_id>')
@api.patch('/partes/<int:parte_id>')
def update_parte(parte_id):
  parte = partes_service.get(parte_id)
  if not parte:
    return jsonify({'error': 'Parte no encontrada'}), 404
  payload = request.get_json(force=True)
  parte = partes_service.update(parte, payload)
  return jsonify(parte.to_dict())


@api.delete('/partes/<int:parte_id>')
def delete_parte(parte_id):
  parte = partes_service.get(parte_id)
  if not parte:
    return jsonify({'error': 'Parte no encontrada'}), 404
  partes_service.delete(parte)
  return jsonify({'deleted': True})


@api.get('/reportes')
def list_reportes():
  reportes = reportes_service.list()
  return jsonify([reporte.to_dict() for reporte in reportes])


@api.get('/reportes/<int:reporte_id>')
def get_reporte(reporte_id):
  reporte = reportes_service.get(reporte_id)
  if not reporte:
    return jsonify({'error': 'Reporte no encontrado'}), 404
  return jsonify(reporte.to_dict())


@api.post('/reportes')
def create_reporte():
  payload = request.get_json(force=True)
  reporte = reportes_service.create(payload)
  return jsonify(reporte.to_dict()), 201


@api.put('/reportes/<int:reporte_id>')
@api.patch('/reportes/<int:reporte_id>')
def update_reporte(reporte_id):
  reporte = reportes_service.get(reporte_id)
  if not reporte:
    return jsonify({'error': 'Reporte no encontrado'}), 404
  payload = request.get_json(force=True)
  reporte = reportes_service.update(reporte, payload)
  return jsonify(reporte.to_dict())


@api.delete('/reportes/<int:reporte_id>')
def delete_reporte(reporte_id):
  reporte = reportes_service.get(reporte_id)
  if not reporte:
    return jsonify({'error': 'Reporte no encontrado'}), 404
  reportes_service.delete(reporte)
  return jsonify({'deleted': True})


@api.get('/reportes/<int:reporte_id>/imprimir')
def print_reporte(reporte_id):
  reporte = reportes_service.get(reporte_id)
  if not reporte:
    return jsonify({'error': 'Reporte no encontrado'}), 404
  html = f"""
  <!doctype html>
  <html lang="es">
    <head>
      <meta charset="utf-8" />
      <title>Reporte {reporte.titulo}</title>
      <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; }}
        h1 {{ margin-bottom: 0; }}
        .meta {{ color: #555; margin-top: 4px; }}
      </style>
    </head>
    <body>
      <h1>{reporte.titulo}</h1>
      <p class="meta">Periodo: {reporte.periodo or 'Sin periodo'}</p>
      <p class="meta">Generado el: {reporte.generado_el}</p>
      <hr />
      <p>Impresión de reporte generado desde Pipita Garage.</p>
    </body>
  </html>
  """
  response = make_response(html)
  response.headers['Content-Type'] = 'text/html'
  return response


@api.get('/dashboard')
def dashboard():
  stats = {
    'clientes': db.session.query(Cliente).count(),
    'vehiculos': db.session.query(Vehiculo).count(),
    'partes': db.session.query(Parte).count(),
    'reportes': db.session.query(Reporte).count(),
  }
  recent_vehiculos = (
    Vehiculo.query.order_by(Vehiculo.created_at.desc()).limit(5).all()
  )
  recent_reportes = (
    Reporte.query.order_by(Reporte.created_at.desc()).limit(5).all()
  )
  return jsonify(
    {
      'stats': stats,
      'recent_vehiculos': [vehiculo.to_dict() for vehiculo in recent_vehiculos],
      'recent_reportes': [reporte.to_dict() for reporte in recent_reportes],
    }
  )


@api.get('/export/excel')
def export_excel():
  workbook = Workbook()
  workbook.remove(workbook.active)

  add_sheet(workbook, 'Clientes', clientes_service.list())
  add_sheet(workbook, 'Vehiculos', vehiculos_service.list())
  add_sheet(workbook, 'Partes', partes_service.list())
  add_sheet(workbook, 'Reportes', reportes_service.list())

  output = BytesIO()
  workbook.save(output)
  output.seek(0)

  return send_file(
    output,
    mimetype='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    as_attachment=True,
    download_name='pipita-datos.xlsx',
  )