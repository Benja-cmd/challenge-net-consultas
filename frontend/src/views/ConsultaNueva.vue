<template>
  <div class="card" style="max-width: 560px">
    <h2>Nueva consulta</h2>
    <form @submit.prevent="guardar">
      <div class="form-group">
        <label>Afiliado</label>
        <select v-model="form.afiliadoId" required>
          <option value="">Seleccioná un afiliado</option>
          <option v-for="a in afiliados" :key="a.id" :value="a.id" :disabled="a.bloqueado">
            {{ a.nombreCompleto }} — DNI {{ a.dni }}{{ a.bloqueado ? ' (bloqueado)' : '' }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Profesional</label>
        <select v-model="form.profesionalId" required>
          <option value="">Seleccioná un profesional</option>
          <option v-for="p in profesionales" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — {{ p.especialidad }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Modalidad</label>
        <select v-model="form.modalidad" required>
          <option value="Presencial">Presencial</option>
          <option value="Virtual">Virtual</option>
        </select>
      </div>
      <div class="form-group">
        <label>Fecha y hora</label>
        <input type="datetime-local" v-model="form.fechaHora" :min="minimoFechaHora" required />
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" placeholder="Motivo de la consulta" maxlength="500" required />
      </div>
      <button type="submit" :disabled="guardando">{{ guardando ? 'Guardando...' : 'Confirmar consulta' }}</button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi } from '../services/api'
import { mensajeDeError } from '../services/errores'

export default {
  name: 'ConsultaNueva',
  data() {
    return {
      form: {
        afiliadoId: '',
        profesionalId: '',
        modalidad: 'Presencial',
        fechaHora: '',
        motivo: ''
      },
      afiliados: [],
      profesionales: [],
      guardando: false
    }
  },
  computed: {
    // Valor mínimo para el input datetime-local (hora local, formato yyyy-MM-ddTHH:mm).
    minimoFechaHora() {
      const ahora = new Date()
      ahora.setMinutes(ahora.getMinutes() - ahora.getTimezoneOffset())
      return ahora.toISOString().slice(0, 16)
    }
  },
  async mounted() {
    try {
      const [aRes, pRes] = await Promise.all([afiliadosApi.getAll(), profesionalesApi.getAll()])
      this.afiliados = aRes.data
      this.profesionales = pRes.data
    } catch (error) {
      alert(mensajeDeError(error))
    }
  },
  methods: {
    async guardar() {
      if (this.guardando) return
      this.guardando = true
      try {
        await consultasApi.create({
          afiliadoId: Number(this.form.afiliadoId),
          profesionalId: Number(this.form.profesionalId),
          modalidad: this.form.modalidad,
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo
        })
        this.$router.push('/consultas')
      } catch (error) {
        alert(mensajeDeError(error))
      } finally {
        this.guardando = false
      }
    }
  }
}
</script>
