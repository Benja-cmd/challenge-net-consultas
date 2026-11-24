// Formato único de fecha y hora para toda la app.
// Se fuerza el ciclo de 24 hs: con 'es-AR' algunos navegadores usan h12 sin
// indicar a. m./p. m., y una consulta de las 15:00 se mostraba como "03:00".
const formatter = new Intl.DateTimeFormat('es-AR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23'
})

export function formatFechaHora(fecha) {
  return fecha ? formatter.format(new Date(fecha)) : ''
}
