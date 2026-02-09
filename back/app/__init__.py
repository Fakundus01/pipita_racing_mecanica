from flask import Flask
from sqlalchemy import inspect, text
from flask_cors import CORS

from app.config import Config
from app.extensions import db, migrate
from app.routes import api


def create_app(config_class=Config):
  app = Flask(__name__)
  app.config.from_object(config_class)

  CORS(
      app,
      supports_credentials=True,
      origins=app.config['CORS_ORIGINS'],
      allow_headers=['Content-Type'],
      methods=['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
    )

  db.init_app(app)
  migrate.init_app(app, db)

  app.register_blueprint(api, url_prefix='/api')

  if app.config.get('AUTO_CREATE_DB', False):
    with app.app_context():
      if app.config['SQLALCHEMY_DATABASE_URI'].startswith('sqlite:'):
        ensure_sqlite_schema()

  return app


def ensure_sqlite_schema():
  inspector = inspect(db.engine)
  table_names = inspector.get_table_names()
  if not table_names:
    db.create_all()
    return

  vehiculos_columns = set()
  if 'vehiculos' in table_names:
    vehiculos_columns = {column['name'] for column in inspector.get_columns('vehiculos')}

  statements = []
  if 'vehiculos' in table_names and 'version' not in vehiculos_columns:
    statements.append('ALTER TABLE vehiculos ADD COLUMN version VARCHAR(80)')
  if 'vehiculos' in table_names and 'patente' not in vehiculos_columns:
    statements.append('ALTER TABLE vehiculos ADD COLUMN patente VARCHAR(20)')
  if 'vehiculos' in table_names and 'cliente_id' not in vehiculos_columns:
    statements.append('ALTER TABLE vehiculos ADD COLUMN cliente_id INTEGER')

  for statement in statements:
    db.session.execute(text(statement))

  if statements:
    db.session.commit()

  inspector = inspect(db.engine)
  if 'servicios' not in inspector.get_table_names():
    db.create_all()