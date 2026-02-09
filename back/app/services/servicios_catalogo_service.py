import json
from pathlib import Path
import re


class ServiciosCatalogoService:
  def __init__(self, data_path):
    self.data_path = Path(data_path)

  def list(self):
    if not self.data_path.exists():
      return []
    with self.data_path.open(encoding='utf-8') as handle:
      data = json.load(handle)
    return data.get('servicios', [])
  
  def _slugify(self, value):
    value = re.sub(r'[^a-z0-9]+', '_', str(value or '').lower())
    return value.strip('_') or 'tarea'

  def _load_data(self):
    if not self.data_path.exists():
      return {
        '_meta': {
          'version': 1,
          'descripcion': 'Catálogo base de servicios frecuentes de taller',
        },
        'servicios': [],
      }
    with self.data_path.open(encoding='utf-8') as handle:
      return json.load(handle)

  def _save_data(self, data):
    self.data_path.parent.mkdir(parents=True, exist_ok=True)
    with self.data_path.open('w', encoding='utf-8') as handle:
      json.dump(data, handle, ensure_ascii=False, indent=2)

  def add(self, nombre, categoria=None, intervalo_km=None):
    if not nombre:
      raise ValueError('Nombre de tarea requerido')

    data = self._load_data()
    servicios = data.setdefault('servicios', [])
    nombre_key = self._slugify(nombre)
    existing = next(
      (item for item in servicios if self._slugify(item.get('nombre')) == nombre_key),
      None,
    )
    if existing:
      raise ValueError('La tarea ya existe en el catálogo')

    entry = {
      'nombre': nombre.strip(),
      'categoria': (categoria or 'General').strip(),
    }
    if intervalo_km not in (None, ''):
      entry['intervalo_km'] = int(intervalo_km)

    servicios.append(entry)
    self._save_data(data)
    return entry

  def template(self):
    return {
      'tareas': [
        {
          'nombre': 'Cambio de aceite y filtro',
          'categoria': 'Mantenimiento',
          'intervalo_km': 10000,
        }
      ],
      'instrucciones': 'Podés cargar nuevas tareas y luego usarlas al registrar cambios de vehículos.',
    }