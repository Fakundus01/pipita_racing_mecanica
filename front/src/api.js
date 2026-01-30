const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

const buildError = async (response) => {
  let detail = ''
  try {
    const data = await response.json()
    detail = data?.error || data?.detail || ''
  } catch (error) {
    detail = ''
  }
  const message = detail || `Error ${response.status}`
  const err = new Error(message)
  err.status = response.status
  return err
}

const apiFetch = async (path, options = {}) => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    },
    ...options,
  })

  if (!response.ok) {
    throw await buildError(response)
  }

  if (response.status === 204) {
    return null
  }
  return response.json()
}

export const login = (payload) =>
  apiFetch('/auth/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const logout = () =>
  apiFetch('/auth/logout', {
    method: 'POST',
  })

export const getSession = () => apiFetch('/auth/me')

export const listClientes = () => apiFetch('/clientes')
export const createCliente = (payload) =>
  apiFetch('/clientes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const listVehiculos = () => apiFetch('/vehiculos')
export const createVehiculo = (payload) =>
  apiFetch('/vehiculos', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const decodeVin = (vin) =>
  apiFetch(`/vehiculos/decodificar?vin=${encodeURIComponent(vin)}`)

export const listPartes = () => apiFetch('/partes')
export const createParte = (payload) =>
  apiFetch('/partes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

export const listReportes = () => apiFetch('/reportes')
export const createReporte = (payload) =>
  apiFetch('/reportes', {
    method: 'POST',
    body: JSON.stringify(payload),
  })