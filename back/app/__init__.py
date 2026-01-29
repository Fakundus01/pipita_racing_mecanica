from flask import Flask

from app.config import Config
from app.extensions import db, migrate
from app.routes import api


def create_app(config_class=Config):
  app = Flask(__name__)
  app.config.from_object(config_class)

  db.init_app(app)
  migrate.init_app(app, db)

  app.register_blueprint(api, url_prefix='/api')

  return app