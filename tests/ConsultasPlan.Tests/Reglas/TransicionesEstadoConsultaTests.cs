using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Tests.Reglas;

public class TransicionesEstadoConsultaTests
{
    private static readonly DateTime Ahora = new(2026, 6, 15, 12, 0, 0);

    private static Consulta Consulta(EstadoConsulta estado, DateTime fechaHora) =>
        new() { Estado = estado, FechaHora = fechaHora };

    [Fact]
    public void Cancelar_con_mas_de_24_horas_es_valido()
    {
        var consulta = Consulta(EstadoConsulta.Pendiente, Ahora.AddHours(25));

        Assert.Null(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Cancelado, Ahora));
    }

    [Fact]
    public void Cancelar_con_menos_de_24_horas_es_rechazado()
    {
        var consulta = Consulta(EstadoConsulta.Confirmado, Ahora.AddHours(23));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Cancelado, Ahora));
    }

    [Theory]
    [InlineData(EstadoConsulta.Cancelado)]
    [InlineData(EstadoConsulta.Atendido)]
    [InlineData(EstadoConsulta.Inasistencia)]
    public void Los_estados_finales_no_admiten_ninguna_transicion(EstadoConsulta estadoFinal)
    {
        var futura = Consulta(estadoFinal, Ahora.AddDays(5));
        var pasada = Consulta(estadoFinal, Ahora.AddDays(-5));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(futura, EstadoConsulta.Cancelado, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(futura, EstadoConsulta.Confirmado, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(pasada, EstadoConsulta.Atendido, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(pasada, EstadoConsulta.Inasistencia, Ahora));
    }

    [Fact]
    public void No_se_puede_registrar_atencion_ni_inasistencia_de_una_consulta_futura()
    {
        var consulta = Consulta(EstadoConsulta.Pendiente, Ahora.AddHours(1));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Atendido, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Inasistencia, Ahora));
    }

    [Fact]
    public void Una_consulta_pasada_abierta_puede_atenderse_o_marcarse_inasistencia()
    {
        var consulta = Consulta(EstadoConsulta.Confirmado, Ahora.AddHours(-1));

        Assert.Null(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Atendido, Ahora));
        Assert.Null(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Inasistencia, Ahora));
    }

    [Fact]
    public void No_se_registra_atencion_de_consultas_de_otro_anio()
    {
        var consulta = Consulta(EstadoConsulta.Pendiente, new DateTime(2025, 12, 30, 10, 0, 0));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Atendido, Ahora));
    }

    [Fact]
    public void Solo_se_confirma_una_consulta_pendiente_y_futura()
    {
        Assert.Null(TransicionesEstadoConsulta.Validar(
            Consulta(EstadoConsulta.Pendiente, Ahora.AddDays(1)), EstadoConsulta.Confirmado, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(
            Consulta(EstadoConsulta.Pendiente, Ahora.AddDays(-1)), EstadoConsulta.Confirmado, Ahora));
        Assert.NotNull(TransicionesEstadoConsulta.Validar(
            Consulta(EstadoConsulta.Confirmado, Ahora.AddDays(1)), EstadoConsulta.Confirmado, Ahora));
    }

    [Fact]
    public void Ninguna_consulta_vuelve_a_pendiente()
    {
        var consulta = Consulta(EstadoConsulta.Confirmado, Ahora.AddDays(1));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, EstadoConsulta.Pendiente, Ahora));
    }

    [Fact]
    public void Un_estado_fuera_del_enum_es_rechazado()
    {
        var consulta = Consulta(EstadoConsulta.Pendiente, Ahora.AddDays(1));

        Assert.NotNull(TransicionesEstadoConsulta.Validar(consulta, (EstadoConsulta)99, Ahora));
    }
}
