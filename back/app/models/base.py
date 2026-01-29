from datetime import datetime

from app.extensions import db


class BaseModel(db.Model):
  __abstract__ = True

  id = db.Column(db.Integer, primary_key=True)
  created_at = db.Column(db.DateTime, default=datetime.utcnow, nullable=False)
  updated_at = db.Column(
    db.DateTime,
    default=datetime.utcnow,
    onupdate=datetime.utcnow,
    nullable=False,
  )

  def to_dict(self):
    return {column.name: getattr(self, column.name) for column in self.__table__.columns}