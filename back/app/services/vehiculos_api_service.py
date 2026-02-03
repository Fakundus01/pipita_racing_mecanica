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

  def _slugify(self, value):
    value = re.sub(r'[^a-z0-9]+', '_', str(value or '').lower())
    return value.strip('_') or 'unknown'

  def _save_data(self, data):
    self.data_path.parent.mkdir(parents=True, exist_ok=True)
    with self.data_path.open('w', encoding='utf-8') as handle:
      json.dump(data, handle, ensure_ascii=False, indent=2)
    self._load_data.cache_clear()

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
  
  def list_brands(self):
    data = self._load_data()
    brands = data.get('brands', [])
    return sorted(
      {brand.get('brand') for brand in brands if brand.get('brand')}
    )

  def list_models(self, make):
    data = self._load_data()
    make_key = self._normalize(make)
    for brand in data.get('brands', []):
      if self._normalize(brand.get('brand')) != make_key and self._normalize(brand.get('brand_id')) != make_key:
        continue
      models = brand.get('markets', {}).get('AR', {}).get('models', [])
      return sorted({item.get('name') for item in models if item.get('name')})
    return []

  def list_versions(self, make, model):
    if not make or not model:
      return []
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
        attributes = item.get('attributes') or []
        versions = [attr for attr in attributes if isinstance(attr, str)]
        if not versions:
          versions = ['Sin versión']
        return sorted(set(versions))
    return ['Sin versión']

  def add_catalog_entry(self, marca, modelo, version=None, anio=None):
    if not marca or not modelo:
      raise ValueError('Marca y modelo requeridos')

    data = self._load_data()
    brands = data.setdefault('brands', [])
    marca_key = self._normalize(marca)
    brand = next(
      (
        item for item in brands
        if self._normalize(item.get('brand')) == marca_key
        or self._normalize(item.get('brand_id')) == marca_key
      ),
      None,
    )
    if not brand:
      brand = {
        'brand': marca,
        'brand_id': self._slugify(marca),
        'markets': {'AR': {'models': []}},
      }
      brands.append(brand)

    models = brand.setdefault('markets', {}).setdefault('AR', {}).setdefault('models', [])
    modelo_key = self._normalize(modelo)
    model_entry = next(
      (
        item for item in models
        if self._normalize(item.get('name')) == modelo_key
        or self._normalize(item.get('model_id')) == modelo_key
      ),
      None,
    )
    if not model_entry:
      model_entry = {
        'model_id': self._slugify(modelo),
        'name': modelo,
        'body': None,
        'segment': None,
        'attributes': [],
        'status': 'unknown',
        'origin': 'unknown',
      }
      models.append(model_entry)

    if version:
      attributes = model_entry.setdefault('attributes', [])
      if version not in attributes:
        attributes.append(version)

    if anio:
      years = model_entry.setdefault('years', [])
      if anio not in years:
        years.append(anio)

    self._save_data(data)
    return model_entry