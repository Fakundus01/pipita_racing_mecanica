from flask_migrate import Migrate #type: ignore
from flask_sqlalchemy import SQLAlchemy


db = SQLAlchemy()
migrate = Migrate()