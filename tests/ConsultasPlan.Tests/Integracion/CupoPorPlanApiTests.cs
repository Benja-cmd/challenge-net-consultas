using System.Net;
using ConsultasPlan.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsultasPlan.Tests.Integracion;

/// <summary>
/// El cupo depende de la política configurada para el plan del afiliado.
/// Seed: todos los afiliados tienen "Plan Integral"; la migración carga la política
/// Plan Integral / Nutrición / Presencial / 6 por año / 1 por mes.
/// </summary>
public class CupoPorPlanApiTests
{
    private static async Task Modificar(ApiFactory api, Func<AppDbContext, Task> cambio)
    {
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await cambio(db);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Un_afiliado_de_un_plan_sin_politica_de_nutricion_no_tiene_limite()
    {
        using var api = new ApiFactory();
        // Roberto (3) agotó el cupo del Plan Integral; se lo pasa a otro plan.
        await Modificar(api, async db => (await db.Afiliados.FindAsync(3))!.Plan = "Plan Básico");

        var respuesta = await api.CreateClient().CrearConsulta(3, 5, "Presencial", "2026-12-02T10:00:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Los_limites_se_toman_de_la_politica_configurada()
    {
        using var api = new ApiFactory();
        await Modificar(api, async db => (await db.PoliticasCupo.SingleAsync()).LimiteAnual = 5);

        // Mariana (2) tiene 5: con límite 5 ya no puede agendar.
        var respuesta = await api.CreateClient().CrearConsulta(2, 4, "Presencial", "2026-11-26T10:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("5 consultas presenciales de nutrición", await respuesta.Mensaje());
    }

    [Fact]
    public async Task Endpoint_de_cupo_informa_usadas_disponibles_y_meses()
    {
        using var api = new ApiFactory();

        var respuesta = await api.CreateClient().GetAsync("/afiliados/3/cupo?profesionalId=5&modalidad=Presencial");
        var cupo = await respuesta.LeerJson();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2026, cupo.GetProperty("anio").GetInt32());
        Assert.Equal(6, cupo.GetProperty("usadas").GetInt32());
        Assert.Equal(0, cupo.GetProperty("disponibles").GetInt32());
        Assert.Equal([1, 2, 3, 4, 5, 6], cupo.GetProperty("mesesUtilizados").EnumerateArray().Select(m => m.GetInt32()));
    }

    [Theory]
    [InlineData("/afiliados/3/cupo?profesionalId=1&modalidad=Presencial")] // clínica médica
    [InlineData("/afiliados/3/cupo?profesionalId=5&modalidad=Virtual")]    // nutrición virtual
    public async Task Endpoint_de_cupo_devuelve_204_si_no_aplica_ningun_cupo(string url)
    {
        using var api = new ApiFactory();

        var respuesta = await api.CreateClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_politica_define_si_la_inasistencia_consume_cupo()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();
        // Laura (6): inasistencia en octubre + consulta pendiente en diciembre.
        const string url = "/afiliados/6/cupo?profesionalId=4&modalidad=Presencial";

        var porDefecto = await (await cliente.GetAsync(url)).LeerJson();
        await Modificar(api, async db => (await db.PoliticasCupo.SingleAsync()).InasistenciaConsumeCupo = false);
        var sinInasistencia = await (await cliente.GetAsync(url)).LeerJson();

        Assert.Equal(2, porDefecto.GetProperty("usadas").GetInt32());
        Assert.Equal(1, sinInasistencia.GetProperty("usadas").GetInt32());
    }
}
