<template>
  <div>
    <router-link to="/consultas" style="font-size:14px; color:#1a73e8">← Volver a consultas</router-link>
    <div v-if="consulta" class="card" style="margin-top: 20px; max-width: 560px">
      <h2>Consulta #{{ consulta.id }}</h2>
      <div class="detail-row"><span class="label">Afiliado</span><span>{{ consulta.afiliado?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">DNI</span><span>{{ consulta.afiliado?.dni }}</span></div>
      <div class="detail-row"><span class="label">Profesional</span><span>{{ consulta.profesional?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">Especialidad</span><span>{{ consulta.profesional?.especialidad }}</span></div>
      <div class="detail-row"><span class="label">Fecha y hora</span><span>{{ formatFecha(consulta.fechaHora) }}</span></div>
      <div class="detail-row"><span class="label">Modalidad</span><span>{{ consulta.modalidad }}</span></div>
      <div class="detail-row"><span class="label">Estado</span><span>{{ consulta.estado }}</span></div>
      <div class="detail-row"><span class="label">Motivo</span><span>{{ consulta.motivo }}</span></div>

      <div v-if="abierta" style="display: flex; gap: 10px; margin-top: 24px">
        <template v-if="esFutura">
          <button v-if="consulta.estado === 'Pendiente'" @click="confirmar">Confirmar consulta</button>
          <button class="btn-danger" @click="cancelar">Cancelar consulta</button>
        </template>
        <template v-else>
          <button @click="registrarAtencion">Registrar atención</button>
          <button @click="marcarInasistencia">Marcar inasistencia</button>
        </template>
      </div>
      <p v-else class="estado-final">La consulta está en un estado final y no admite cambios.</p>
    </div>
    <p v-else>Cargando...</p>
  </div>
</template>

<script>
import { consultasApi } from '../services/api'
import { mensajeDeError } from '../services/errores'

export default {
  name: 'ConsultaDetalle',
  data() {
    return {
      consulta: null
    }
  },
  computed: {
    abierta() {
      return ['Pendiente', 'Confirmado'].includes(this.consulta?.estado)
    },
    esFutura() {
      return new Date(this.consulta?.fechaHora) > new Date()
    }
  },
  async mounted() {
    await this.cargar()
  },
  methods: {
    formatFecha(fecha) {
      return new Date(fecha).toLocaleString('es-AR')
    },
    async cargar() {
      try {
        const res = await consultasApi.getById(this.$route.params.id)
        this.consulta = res.data
      } catch (error) {
        alert(mensajeDeError(error))
      }
    },
    // Ejecuta una acción sobre la consulta y vuelve a pedirla completa:
    // las respuestas de las acciones no incluyen afiliado ni profesional.
    async ejecutar(accion) {
      try {
        await accion(this.consulta.id)
        await this.cargar()
      } catch (error) {
        alert(mensajeDeError(error))
      }
    },
    confirmar() {
      return this.ejecutar(id => consultasApi.actualizarEstado(id, { estado: 'Confirmado' }))
    },
    registrarAtencion() {
      return this.ejecutar(consultasApi.registrarAtencion)
    },
    cancelar() {
      return this.ejecutar(consultasApi.cancelar)
    },
    marcarInasistencia() {
      return this.ejecutar(consultasApi.marcarInasistencia)
    }
  }
}
</script>

<style scoped>
.detail-row {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
}
.estado-final {
  margin-top: 24px;
  font-size: 13px;
  color: #666;
}
.label {
  width: 130px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}
</style>
