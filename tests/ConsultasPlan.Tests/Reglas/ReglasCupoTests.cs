using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Tests.Reglas;

public class ReglasCupoTests
{
    private static readonly Profesional Nutricionista = new() { Especialidad = "Nutrición" };
    private static readonly Profesional Cardiologo = new() { Especialidad = "Cardiología" };

    /// <summary>Política del challenge: Plan Integral, Nutrición presencial, 6 por año, 1 por mes.</summary>
    private static PoliticaCupo Politica(bool inasistenciaConsume = true) => new()
    {
        Plan = "Plan Integral",
        Especialidad = "Nutrición",
        Modalidad = ModalidadConsulta.Presencial,
        LimiteAnual = 6,
        LimiteMensual = 1,
        InasistenciaConsumeCupo = inasistenciaConsume
    };

    private static DateTime Fecha(int anio, int mes, int dia = 10) => new(anio, mes, dia, 10, 0, 0);

    private static string? Validar(IEnumerable<DateTime> previas, DateTime nueva) =>
        ReglasCupo.Validar(Politica(), previas, nueva);

    // ---------- Validar: límites ----------

    [Fact]
    public void Sin_consultas_previas_hay_cupo()
    {
        Assert.Null(Validar([], Fecha(2026, 3)));
    }

    [Fact]
    public void Con_cinco_consultas_en_otros_meses_permite_la_sexta()
    {
        var previas = Enumerable.Range(1, 5).Select(m => Fecha(2026, m));

        Assert.Null(Validar(previas, Fecha(2026, 6)));
    }

    [Fact]
    public void Con_seis_consultas_en_el_anio_rechaza_la_septima()
    {
        var previas = Enumerable.Range(1, 6).Select(m => Fecha(2026, m));

        var error = Validar(previas, Fecha(2026, 9));

        Assert.NotNull(error);
        Assert.Contains("6 consultas presenciales de nutrición", error);
        Assert.Contains("2026", error);
    }

    [Fact]
    public void Rechaza_una_segunda_consulta_en_el_mismo_mes()
    {
        var error = Validar([Fecha(2026, 4, 2)], Fecha(2026, 4, 28));

        Assert.NotNull(error);
        Assert.Contains("abril", error);
    }

    [Fact]
    public void Permite_una_consulta_en_el_mes_siguiente()
    {
        Assert.Null(Validar([Fecha(2026, 4, 30)], Fecha(2026, 5, 1)));
    }

    [Fact]
    public void Respeta_los_limites_configurados_en_la_politica()
    {
        var politica = Politica();
        politica.LimiteAnual = 12;
        politica.LimiteMensual = 2;

        Assert.Null(ReglasCupo.Validar(politica, [Fecha(2026, 4, 2)], Fecha(2026, 4, 20)));
        Assert.NotNull(ReglasCupo.Validar(politica, [Fecha(2026, 4, 2), Fecha(2026, 4, 9)], Fecha(2026, 4, 20)));
    }

    // ---------- Validar: no acumulación ----------

    [Fact]
    public void Las_consultas_del_anio_anterior_no_consumen_cupo_del_actual()
    {
        var anioAnterior = Enumerable.Range(7, 6).Select(m => Fecha(2025, m));

        Assert.Null(Validar(anioAnterior, Fecha(2026, 1)));
    }

    [Fact]
    public void El_mismo_mes_de_otro_anio_no_cuenta_para_el_limite_mensual()
    {
        Assert.Null(Validar([Fecha(2025, 12)], Fecha(2026, 12)));
    }

    [Fact]
    public void Los_meses_sin_usar_no_habilitan_dos_consultas_en_un_mismo_mes()
    {
        // Sin consultas de enero a octubre: igual no puede tener 2 en noviembre.
        Assert.NotNull(Validar([Fecha(2026, 11, 3)], Fecha(2026, 11, 20)));
    }

    [Fact]
    public void Agendar_en_diciembre_para_enero_usa_el_cupo_del_anio_siguiente()
    {
        var anioActualCompleto = Enumerable.Range(1, 6).Select(m => Fecha(2026, m));

        Assert.Null(Validar(anioActualCompleto, Fecha(2027, 1)));
    }

    // ---------- Aplica ----------

    [Theory]
    [InlineData("Plan Integral", "Nutrición")]
    [InlineData("plan integral", "nutricion")]
    [InlineData("  PLAN INTEGRAL ", "  NUTRICIÓN ")]
    public void Aplica_sin_importar_tildes_ni_mayusculas(string plan, string especialidad)
    {
        Assert.True(ReglasCupo.Aplica(Politica(), plan, new Profesional { Especialidad = especialidad }, ModalidadConsulta.Presencial));
    }

    [Fact]
    public void Aplica_solo_al_plan_especialidad_y_modalidad_de_la_politica()
    {
        var politica = Politica();

        Assert.True(ReglasCupo.Aplica(politica, "Plan Integral", Nutricionista, ModalidadConsulta.Presencial));
        Assert.False(ReglasCupo.Aplica(politica, "Plan Básico", Nutricionista, ModalidadConsulta.Presencial));
        Assert.False(ReglasCupo.Aplica(politica, "Plan Integral", Nutricionista, ModalidadConsulta.Virtual));
        Assert.False(ReglasCupo.Aplica(politica, "Plan Integral", Cardiologo, ModalidadConsulta.Presencial));
        Assert.False(ReglasCupo.Aplica(politica, "Plan Integral", null, ModalidadConsulta.Presencial));
    }

    // ---------- ConsumeCupo ----------

    private static Consulta Consulta(EstadoConsulta estado, ModalidadConsulta modalidad = ModalidadConsulta.Presencial) =>
        new() { Profesional = Nutricionista, Modalidad = modalidad, Estado = estado };

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, true)]
    [InlineData(EstadoConsulta.Confirmado, true)]
    [InlineData(EstadoConsulta.Atendido, true)]
    [InlineData(EstadoConsulta.Inasistencia, true)]
    [InlineData(EstadoConsulta.Cancelado, false)]
    public void Por_defecto_consumen_cupo_todos_los_estados_excepto_cancelado(EstadoConsulta estado, bool consume)
    {
        Assert.Equal(consume, ReglasCupo.ConsumeCupo(Politica(), Consulta(estado)));
    }

    [Fact]
    public void Si_la_politica_lo_indica_la_inasistencia_no_consume_cupo()
    {
        Assert.False(ReglasCupo.ConsumeCupo(Politica(inasistenciaConsume: false), Consulta(EstadoConsulta.Inasistencia)));
        Assert.True(ReglasCupo.ConsumeCupo(Politica(inasistenciaConsume: false), Consulta(EstadoConsulta.Atendido)));
    }

    [Fact]
    public void Una_consulta_de_otra_modalidad_o_especialidad_no_consume_cupo()
    {
        var virtualDeNutricion = Consulta(EstadoConsulta.Atendido, ModalidadConsulta.Virtual);
        var cardiologia = new Consulta { Profesional = Cardiologo, Modalidad = ModalidadConsulta.Presencial, Estado = EstadoConsulta.Atendido };

        Assert.False(ReglasCupo.ConsumeCupo(Politica(), virtualDeNutricion));
        Assert.False(ReglasCupo.ConsumeCupo(Politica(), cardiologia));
    }
}
