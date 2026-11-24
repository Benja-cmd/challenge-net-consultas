<template>
  <div>
    <h2>Afiliados</h2>
    <table v-if="afiliados.length">
      <thead>
        <tr>
          <th>#</th>
          <th>Nombre</th>
          <th>DNI</th>
          <th>Email</th>
          <th>Teléfono</th>
          <th>Plan</th>
          <th>Consultas usadas</th>
          <th>Bloqueado</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in afiliados" :key="a.id">
          <td>{{ a.id }}</td>
          <td>{{ a.nombreCompleto }}</td>
          <td>{{ a.dni }}</td>
          <td>{{ a.email }}</td>
          <td>{{ a.telefono }}</td>
          <td>{{ a.plan }}</td>
          <td>{{ a.consultasUsadas }}</td>
          <td>
            <span v-if="a.bloqueado" style="color: #d32f2f; font-weight: 600">Sí</span>
            <span v-else style="color: #388e3c">No</span>
          </td>
          <td>
            <button class="btn-danger" @click="darDeBaja(a)">Dar de baja</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-else>No hay afiliados registrados.</p>
  </div>
</template>

<script>
import { afiliadosApi } from '../services/api'
import { mensajeDeError } from '../services/errores'

export default {
  name: 'AfiliadosList',
  data() {
    return {
      afiliados: []
    }
  },
  async mounted() {
    try {
      const res = await afiliadosApi.getAll()
      this.afiliados = res.data
    } catch (error) {
      alert(mensajeDeError(error))
    }
  },
  methods: {
    async darDeBaja(afiliado) {
      const confirmado = confirm(
        `¿Dar de baja a ${afiliado.nombreCompleto} (DNI ${afiliado.dni})?\n` +
        'Dejará de aparecer en el listado y no podrá agendar consultas. Su historial se conserva.'
      )
      if (!confirmado) return

      try {
        await afiliadosApi.delete(afiliado.id)
        this.afiliados = this.afiliados.filter(a => a.id !== afiliado.id)
      } catch (error) {
        alert(mensajeDeError(error))
      }
    }
  }
}
</script>
