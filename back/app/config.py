import os


def _split_list(value):
  return [item.strip() for item in value.split(',') if item.strip()]


class Config:
  SQLALCHEMY_DATABASE_URI = os.getenv('DATABASE_URL', 'sqlite:///pipita.db')
  SQLALCHEMY_TRACK_MODIFICATIONS = False
  AUTO_CREATE_DB = os.getenv('AUTO_CREATE_DB', 'True').lower() == 'true'
  JSON_SORT_KEYS = False
  SECRET_KEY = os.getenv('SECRET_KEY', 'dev-secret-key')
  ADMIN_USERNAME = os.getenv('ADMIN_USERNAME', 'admin')
  ADMIN_PASSWORD = os.getenv('ADMIN_PASSWORD', 'admin123')
  CORS_ORIGINS = _split_list(
    os.getenv(
      'CORS_ORIGINS',
      'http://localhost:5173,http://127.0.0.1:5173,http://localhost:5174,http://127.0.0.1:5174',
    )
  )
  NINJA_CARS_API_BASE = os.getenv('NINJA_CARS_API_BASE', 'https://api.api-ninjas.com/v1')
  NINJA_CARS_API_KEY = os.getenv('NINJA_CARS_API_KEY', 'M3JIRj8jwJYQC4iwpSTUtfjnkuiNAX9fq5o3OZcS')
  SESSION_COOKIE_HTTPONLY = True
  SESSION_COOKIE_SAMESITE = os.getenv('SESSION_COOKIE_SAMESITE', 'Lax')
  SESSION_COOKIE_DOMAIN = os.getenv('SESSION_COOKIE_DOMAIN')
  SESSION_COOKIE_SECURE = os.getenv('SESSION_COOKIE_SECURE', 'False').lower() == 'true'