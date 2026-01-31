import requests # type: ignore


class VehiculosApiService:
  def __init__(self, base_url):
    self.base_url = base_url.rstrip('/')

  def decode_vin(self, vin):
    if not vin:
      raise ValueError('VIN requerido')

    url = f'{self.base_url}/DecodeVinValuesExtended/{vin}'
    response = requests.get(url, params={'format': 'json'}, timeout=10)
    response.raise_for_status()
    payload = response.json()
    results = payload.get('Results', [])
    if not results:
      return {}

    result = results[0]
    return {
      'vin': vin,
      'marca': result.get('Make'),
      'modelo': result.get('Model'),
      'anio': result.get('ModelYear'),
      'tipo_carroceria': result.get('BodyClass'),
      'combustible': result.get('FuelTypePrimary'),
      'pais_origen': result.get('PlantCountry'),
    }