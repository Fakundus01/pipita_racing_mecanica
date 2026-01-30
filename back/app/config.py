import os


class Config:
  SQLALCHEMY_DATABASE_URI = os.getenv('DATABASE_URL', 'sqlite:///pipita.db')
  SQLALCHEMY_TRACK_MODIFICATIONS = False
  JSON_SORT_KEYS = False
  SECRET_KEY = os.getenv('SECRET_KEY', 'dev-secret-key')
  ADMIN_USERNAME = os.getenv('ADMIN_USERNAME', 'admin')
  ADMIN_PASSWORD = os.getenv('ADMIN_PASSWORD', 'admin123')
  CORS_ORIGINS = os.getenv('CORS_ORIGINS', 'http://localhost:5173').split(',')
  VEHICULOS_API_BASE = os.getenv(
    'VEHICULOS_API_BASE',
    'https://vpic.nhtsa.dot.gov/api/vehicles',
  )
  SESSION_COOKIE_HTTPONLY = True
  SESSION_COOKIE_SAMESITE = 'Lax'