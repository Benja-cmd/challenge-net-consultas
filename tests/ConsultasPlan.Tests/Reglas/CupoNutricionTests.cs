using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Tests.Reglas;

public class CupoNutricionTests
{
    private static readonly Profesional Nutricionista = new() { Especialidad = "Nutrición" };
    private static readonly Profesional Cardiologo = new() { Especialidad = "Cardiología" };

    private static DateTime Fecha(int anio, int mes, int dia = 10) => new(anio, mes, dia, 10, 0, 0);

    // ---------- Validar: límites ----------

    [Fact]
    public void Sin_consultas_previas_hay_cupo()
    {
        Assert.Null(CupoNutricion.Validar([], Fecha(2026, 3)));
    }

    [Fact]
    public void Con_cinco_consultas_en_otros_meses_permite_la_sexta()
    {
        var previas = Enumerable.Range(1, 5).Select(m => Fecha(2026, m));

        Assert.Null(CupoNutricion.Validar(previas, Fecha(2026, 6)));
    }

    [Fact]
    public void Con_seis_consultas_en_el_anio_rechaza_la_septima()
    {
        var previas = Enumerable.Range(1, 6).Select(m => Fecha(2026, m));

        var error = CupoNutricion.Validar(previas, Fecha(2026, 9));

        Assert.NotNull(error);
        Assert.Contains("6", error);
        Assert.Contains("2026", error);
    }

    [Fact]
    public void Rechaza_una_segunda_consulta_en_el_mismo_mes()
    {
        var error = CupoNutricion.Validar([Fecha(2026, 4, 2)], Fecha(2026, 4, 28));

        Assert.NotNull(error);
        Assert.Contains("abril", error);
    }

    [Fact]
    public void Permite_una_consulta_en_el_mes_siguiente()
    {
        Assert.Null(CupoNutricion.Validar([Fecha(2026, 4, 30)], Fecha(2026, 5, 1)));
    }

    // ---------- Validar: no acumulación ----------

    [Fact]
    public void Las_consultas_del_anio_anterior_no_consumen_cupo_del_actual()
    {
        var anioAnterior = Enumerable.Range(7, 6).Select(m => Fecha(2025, m));

        Assert.Null(CupoNutricion.Validar(anioAnterior, Fecha(2026, 1)));
    }

    [Fact]
    public void El_mismo_mes_de_otro_anio_no_cuenta_para_el_limite_mensual()
    {
        Assert.Null(CupoNutricion.Validar([Fecha(2025, 12)], Fecha(2026, 12)));
    }

    [Fact]
    public void Los_meses_sin_usar_no_habilitan_dos_consultas_en_un_mismo_mes()
    {
        // Sin consultas de enero a octubre: igual no puede tener 2 en noviembre.
        var error = CupoNutricion.Validar([Fecha(2026, 11, 3)], Fecha(2026, 11, 20));

        Assert.NotNull(error);
    }

    [Fact]
    public void Agendar_en_diciembre_para_enero_usa_el_cupo_del_anio_siguiente()
    {
        var anioActualCompleto = Enumerable.Range(1, 6).Select(m => Fecha(2026, m));

        Assert.Null(CupoNutricion.Validar(anioActualCompleto, Fecha(2027, 1)));
    }

    // ---------- Aplica / ConsumeCupo ----------

    [Theory]
    [InlineData("Nutrición")]
    [InlineData("nutricion")]
    [InlineData("  NUTRICIÓN ")]
    public void Reconoce_la_especialidad_nutricion_sin_importar_tildes_ni_mayusculas(string especialidad)
    {
        Assert.True(CupoNutricion.EsNutricion(new Profesional { Especialidad = especialidad }));
    }

    [Fact]
    public void Aplica_solo_a_nutricion_presencial()
    {
        Assert.True(CupoNutricion.Aplica(Nutricionista, ModalidadConsulta.Presencial));
        Assert.False(CupoNutricion.Aplica(Nutricionista, ModalidadConsulta.Virtual));
        Assert.False(CupoNutricion.Aplica(Cardiologo, ModalidadConsulta.Presencial));
        Assert.False(CupoNutricion.Aplica(null, ModalidadConsulta.Presencial));
    }

    [Theory]
    [InlineData(EstadoConsulta.Pendiente, true)]
    [InlineData(EstadoConsulta.Confirmado, true)]
    [InlineData(EstadoConsulta.Atendido, true)]
    [InlineData(EstadoConsulta.Inasistencia, true)]
    [InlineData(EstadoConsulta.Cancelado, false)]
    public void Consumen_cupo_todos_los_estados_excepto_cancelado(EstadoConsulta estado, bool consume)
    {
        var consulta = new Consulta
        {
            Profesional = Nutricionista,
            Modalidad = ModalidadConsulta.Presencial,
            Estado = estado
        };

        Assert.Equal(consume, CupoNutricion.ConsumeCupo(consulta));
    }

    [Fact]
    public void Una_consulta_virtual_de_nutricion_no_consume_cupo()
    {
        var consulta = new Consulta
        {
            Profesional = Nutricionista,
            Modalidad = ModalidadConsulta.Virtual,
            Estado = EstadoConsulta.Atendido
        };

        Assert.False(CupoNutricion.ConsumeCupo(consulta));
    }
}
