import json
from pathlib import Path


class ServiciosCatalogoService:
  def __init__(self, data_path):
    self.data_path = Path(data_path)

  def list(self):
    if not self.data_path.exists():
      return []
    with self.data_path.open(encoding='utf-8') as handle:
      data = json.load(handle)
    return data.get('servicios', [])