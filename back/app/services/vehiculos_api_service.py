import json
import re
from functools import lru_cache
from pathlib import Path


class VehiculosApiService:
  def __init__(self, data_path):
    self.data_path = Path(data_path)

  @lru_cache(maxsize=1)
  def _load_data(self):
    with self.data_path.open(encoding='utf-8') as handle:
      return json.load(handle)

  @staticmethod
  def _normalize(value):
    return re.sub(r'[^a-z0-9]+', '', str(value or '').lower())

  @staticmethod
  def _map_fuel(attributes):
    if not attributes:
      return None
    if 'EV' in attributes:
      return 'Eléctrico'
    if 'Hybrid' in attributes:
      return 'Híbrido'
    return None

  def search_cars(self, make, model, trim=None):
    if not make or not model:
      raise ValueError('Marca y modelo requeridos')
    data = self._load_data()
    make_key = self._normalize(make)
    model_key = self._normalize(model)
    for brand in data.get('brands', []):
      if self._normalize(brand.get('brand')) != make_key and self._normalize(brand.get('brand_id')) != make_key:
        continue
      models = brand.get('markets', {}).get('AR', {}).get('models', [])
      for item in models:
        if self._normalize(item.get('name')) != model_key and self._normalize(item.get('model_id')) != model_key:
          continue
        body = item.get('body')
        segment = item.get('segment')
        attributes = item.get('attributes') or []
        tipo_carroceria = body
        if body and segment:
          tipo_carroceria = f'{body} · {segment}'
        origin = item.get('origin')
        pais_origen = origin if origin and origin != 'unknown' else None
        result = {
          'marca': brand.get('brand') or make,
          'modelo': item.get('name') or model,
          'version': trim,
          'anio': None,
          'combustible': self._map_fuel(attributes),
          'tipo_carroceria': tipo_carroceria,
          'pais_origen': pais_origen,
          'atributos': attributes,
          'estado': item.get('status'),
        }
        return result

    return {
      'marca': make,
      'modelo': model,
      'version': trim,
      'anio': None,
      'combustible': None,
      'tipo_carroceria': None,
      'pais_origen': None,
      'atributos': [],
      'estado': None,
    }