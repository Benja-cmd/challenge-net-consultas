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
      <div v-if="aplicaCupo && cupo" :class="['cupo', { 'cupo-agotado': avisoCupo }]">
        <strong>Cupo de nutrición presencial {{ cupo.anio }}:</strong>
        {{ cupo.usadas }} de {{ cupo.limiteAnual }} usadas ({{ cupo.disponibles }} disponibles, máximo {{ cupo.limiteMensual }} por mes).
        <span v-if="cupo.mesesUtilizados.length">Meses ya utilizados: {{ nombresMeses(cupo.mesesUtilizados) }}.</span>
        <div v-if="avisoCupo" class="cupo-aviso">{{ avisoCupo }}</div>
      </div>
      <button type="submit" :disabled="guardando">{{ guardando ? 'Guardando...' : 'Confirmar consulta' }}</button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi } from '../services/api'
import { mensajeDeError } from '../services/errores'

const esNutricion = (especialidad = '') =>
  especialidad.trim().toLowerCase().normalize('NFD').replace(/\p{Diacritic}/gu, '') === 'nutricion'

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
      guardando: false,
      cupo: null
    }
  },
  computed: {
    // Valor mínimo para el input datetime-local (hora local, formato yyyy-MM-ddTHH:mm).
    minimoFechaHora() {
      const ahora = new Date()
      ahora.setMinutes(ahora.getMinutes() - ahora.getTimezoneOffset())
      return ahora.toISOString().slice(0, 16)
    },
    profesionalSeleccionado() {
      return this.profesionales.find(p => p.id === this.form.profesionalId)
    },
    // Misma condición que el backend: profesional de nutrición y modalidad presencial.
    aplicaCupo() {
      return Boolean(this.form.afiliadoId)
        && this.form.modalidad === 'Presencial'
        && esNutricion(this.profesionalSeleccionado?.especialidad)
    },
    anioCupo() {
      return this.form.fechaHora ? Number(this.form.fechaHora.slice(0, 4)) : new Date().getFullYear()
    },
    // Aviso anticipado; la validación definitiva la hace el backend al guardar.
    avisoCupo() {
      if (!this.cupo) return ''
      if (this.cupo.disponibles === 0) return 'El afiliado no tiene cupo disponible en este año.'
      const mes = this.form.fechaHora ? Number(this.form.fechaHora.slice(5, 7)) : null
      if (mes && this.cupo.mesesUtilizados.includes(mes)) return 'El afiliado ya tiene una consulta de nutrición presencial en ese mes.'
      return ''
    }
  },
  watch: {
    aplicaCupo() { this.cargarCupo() },
    anioCupo() { this.cargarCupo() },
    'form.afiliadoId'() { this.cargarCupo() }
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
    async cargarCupo() {
      this.cupo = null
      if (!this.aplicaCupo) return
      try {
        const res = await afiliadosApi.cupoNutricion(this.form.afiliadoId, this.anioCupo)
        this.cupo = res.data
      } catch (error) {
        alert(mensajeDeError(error))
      }
    },
    nombresMeses(meses) {
      return meses
        .map(m => new Date(2000, m - 1, 1).toLocaleString('es-AR', { month: 'long' }))
        .join(', ')
    },
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

<style scoped>
.cupo {
  margin-bottom: 18px;
  padding: 12px 14px;
  border-radius: 4px;
  background: #e8f0fe;
  color: #1a3e72;
  font-size: 13px;
  line-height: 1.5;
}
.cupo-agotado {
  background: #fdecea;
  color: #8a1c1c;
}
.cupo-aviso {
  margin-top: 6px;
  font-weight: 600;
}
</style>
