import axios from 'axios'

// Configurable por entorno (ver .env.example); por defecto, el backend local.
const BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export const consultasApi = {
  getAll:             ()          => axios.get(`${BASE_URL}/consultas`),
  getById:            (id)        => axios.get(`${BASE_URL}/consultas/${id}`),
  create:             (data)      => axios.post(`${BASE_URL}/consultas`, data),
  cancelar:           (id)        => axios.post(`${BASE_URL}/consultas/${id}/cancelacion`),
  marcarInasistencia: (id)        => axios.post(`${BASE_URL}/consultas/${id}/inasistencia`),
  registrarAtencion:  (id)        => axios.post(`${BASE_URL}/consultas/${id}/atencion`),
  actualizarEstado:   (id, data)  => axios.put(`${BASE_URL}/consultas/${id}/estado`, data)
}

export const afiliadosApi = {
  getAll:  ()          => axios.get(`${BASE_URL}/afiliados`),
  getById: (id)        => axios.get(`${BASE_URL}/afiliados/${id}`),
  create:  (data)      => axios.post(`${BASE_URL}/afiliados`, data),
  update:  (id, data)  => axios.put(`${BASE_URL}/afiliados/${id}`, data),
  delete:  (id)        => axios.delete(`${BASE_URL}/afiliados/${id}`),
  cupoNutricion: (id, anio) => axios.get(`${BASE_URL}/afiliados/${id}/cupo-nutricion`, { params: { anio } })
}

export const profesionalesApi = {
  getAll: () => axios.get(`${BASE_URL}/profesionales`)
}

export const sedesApi = {
  getAll: () => axios.get(`${BASE_URL}/sedes`)
}
