using System.Net;

namespace ConsultasPlan.Tests.Integracion;

/// <summary>
/// Altas simultáneas: las validaciones (cupo y superposición de horarios) leen y luego
/// escriben, por lo que sin serialización dos requests concurrentes pueden pasar ambas.
/// </summary>
public class ConcurrenciaApiTests
{
    private const int Simultaneas = 20;

    [Fact]
    public async Task Altas_simultaneas_no_superan_el_cupo_mensual()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        // Gonzalo (1) no tiene consultas de nutrición: 20 pedidos en diciembre, horarios distintos.
        var respuestas = await Task.WhenAll(Enumerable.Range(0, Simultaneas).Select(i =>
            cliente.CrearConsulta(1, 4, "Presencial", $"2026-12-10T{8 + i / 2:00}:{i % 2 * 30:00}:00")));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.All(respuestas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
    }

    [Fact]
    public async Task Altas_simultaneas_no_superponen_turnos_del_profesional()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        // 20 afiliados distintos no pueden ocupar el mismo turno de clínica médica. Se usan
        // los afiliados activos y no bloqueados del seed, repetidos.
        int[] afiliados = [1, 2, 3, 4, 5, 6, 8];
        var respuestas = await Task.WhenAll(Enumerable.Range(0, Simultaneas).Select(i =>
            cliente.CrearConsulta(afiliados[i % afiliados.Length], 1, "Presencial", "2026-12-11T10:00:00")));

        Assert.Equal(1, respuestas.Count(r => r.StatusCode == HttpStatusCode.Created));
    }
}
