import requests # type: ignore


class VehiculosApiService:
  def __init__(self, base_url, api_key):
    self.base_url = base_url.rstrip('/')
    self.api_key = api_key

  def search_cars(self, make, model, trim=None):
    if not make or not model:
      raise ValueError('Marca y modelo requeridos')
    if not self.api_key:
      raise ValueError('API Key de Ninja Cars no configurada')

    url = f'{self.base_url}/cars'
    params = {'make': make, 'model': model}
    if trim:
      params['trim'] = trim
    response = requests.get(
      url,
      params=params,
      headers={'X-Api-Key': self.api_key},
      timeout=10,
    )
    response.raise_for_status()
    payload = response.json()
    if not payload:
      return {}

    result = payload[0]
    return {
      'marca': result.get('make'),
      'modelo': result.get('model'),
      'version': result.get('trim'),
      'anio': result.get('year'),
      'combustible': result.get('fuel_type'),
      'transmision': result.get('transmission'),
      'traccion': result.get('drive'),
      'carroceria': result.get('class'),
      'cilindros': result.get('cylinders'),
      'desplazamiento': result.get('displacement'),
    }