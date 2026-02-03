from flask import Flask
from sqlalchemy import inspect
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
    )

  db.init_app(app)
  migrate.init_app(app, db)

  app.register_blueprint(api, url_prefix='/api')

  if app.config.get('AUTO_CREATE_DB', False):
    with app.app_context():
      if app.config['SQLALCHEMY_DATABASE_URI'].startswith('sqlite:'):
        inspector = inspect(db.engine)
        if not inspector.get_table_names():
          db.create_all()

  return app