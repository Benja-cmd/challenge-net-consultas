// Traduce un error de axios al mensaje que conviene mostrar al usuario.
// El backend responde de dos formas:
//   - { mensaje: '...' }                       → reglas de negocio
//   - { errors: { Campo: ['...', ...] } }      → validación de datos (ProblemDetails)
export function mensajeDeError(error) {
  const data = error?.response?.data

  if (data?.mensaje) return data.mensaje

  if (data?.errors) {
    return Object.values(data.errors).flat().join('\n')
  }

  if (error?.response?.status === 404) return 'El recurso solicitado no existe.'
  if (!error?.response) return 'No se pudo conectar con el servidor.'

  return 'Error al procesar la solicitud.'
}
