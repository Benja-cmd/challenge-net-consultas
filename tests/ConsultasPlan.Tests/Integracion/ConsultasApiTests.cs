using System.Net;
using System.Net.Http.Json;

namespace ConsultasPlan.Tests.Integracion;

/// <summary>
/// Tests de punta a punta contra la API con los datos del seed.
/// Fecha actual fija: 24/11/2026 10:00 (ver ApiFactory).
/// Profesionales de nutrición del seed: 4 (Arana) y 5 (Ibarra). Afiliado 7 está bloqueado.
/// </summary>
public class ConsultasApiTests
{
    // ---------- Alta de consultas ----------

    [Fact]
    public async Task Alta_valida_devuelve_201_en_estado_pendiente()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var respuesta = await cliente.CrearConsulta(1, 1, "Presencial", "2026-12-01T10:00:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal("Pendiente", (await respuesta.LeerJson()).GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Alta_ignora_un_afiliado_anidado_en_el_body()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/consultas", new
        {
            afiliadoId = 1, profesionalId = 1, modalidad = "Presencial",
            fechaHora = "2026-12-01T10:00:00", motivo = "x",
            afiliado = new { nombreCompleto = "Inyectado", dni = "1" }
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(1, (await respuesta.LeerJson()).GetProperty("afiliadoId").GetInt32());
        var afiliados = await cliente.GetFromJsonAsync<List<object>>("/afiliados");
        Assert.Equal(8, afiliados!.Count);
    }

    [Fact]
    public async Task Alta_en_fecha_pasada_es_rechazada()
    {
        using var api = new ApiFactory();
        var respuesta = await api.CreateClient().CrearConsulta(1, 1, "Presencial", "2026-11-24T09:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Alta_para_afiliado_bloqueado_es_rechazada()
    {
        using var api = new ApiFactory();
        var respuesta = await api.CreateClient().CrearConsulta(7, 1, "Presencial", "2026-12-01T10:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("bloqueado", await respuesta.Mensaje());
    }

    // ---------- Cupo de nutrición ----------

    [Fact]
    public async Task Afiliado_con_seis_consultas_no_puede_agendar_otra_presencial_pero_si_virtual()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var presencial = await cliente.CrearConsulta(3, 5, "Presencial", "2026-12-02T10:00:00");
        var virtualMismoMes = await cliente.CrearConsulta(3, 5, "Virtual", "2026-12-02T11:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, presencial.StatusCode);
        Assert.Contains("6", await presencial.Mensaje());
        Assert.Equal(HttpStatusCode.Created, virtualMismoMes.StatusCode);
    }

    [Fact]
    public async Task El_cupo_anual_se_renueva_con_el_anio_calendario()
    {
        using var api = new ApiFactory();
        var respuesta = await api.CreateClient().CrearConsulta(3, 5, "Presencial", "2027-01-10T10:00:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Con_cinco_usadas_permite_la_sexta_y_rechaza_la_septima()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var sexta = await cliente.CrearConsulta(2, 4, "Presencial", "2026-11-26T10:00:00");
        var septima = await cliente.CrearConsulta(2, 4, "Presencial", "2026-12-03T10:00:00");

        Assert.Equal(HttpStatusCode.Created, sexta.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, septima.StatusCode);
    }

    [Fact]
    public async Task No_permite_dos_consultas_presenciales_de_nutricion_en_el_mismo_mes()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var mismoMes = await cliente.CrearConsulta(4, 4, "Presencial", "2026-11-28T10:00:00");
        var mesSiguiente = await cliente.CrearConsulta(4, 4, "Presencial", "2026-12-05T10:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, mismoMes.StatusCode);
        Assert.Contains("noviembre", await mismoMes.Mensaje());
        Assert.Equal(HttpStatusCode.Created, mesSiguiente.StatusCode);
    }

    [Fact]
    public async Task Las_consultas_canceladas_y_virtuales_no_consumen_cupo()
    {
        using var api = new ApiFactory();
        // Diego (5): una virtual atendida y una presencial cancelada.
        var respuesta = await api.CreateClient().CrearConsulta(5, 5, "Presencial", "2026-11-27T10:00:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Cancelar_una_consulta_libera_el_cupo_del_mes()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();
        // Laura (6) tiene la consulta 17 de nutrición el 14/12.

        var antes = await cliente.CrearConsulta(6, 5, "Presencial", "2026-12-20T10:00:00");
        var cancelacion = await cliente.PostAsync("/consultas/17/cancelacion", null);
        var despues = await cliente.CrearConsulta(6, 5, "Presencial", "2026-12-20T10:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, antes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, cancelacion.StatusCode);
        Assert.Equal(HttpStatusCode.Created, despues.StatusCode);
    }

    [Fact]
    public async Task Otra_especialidad_no_esta_alcanzada_por_el_cupo()
    {
        using var api = new ApiFactory();
        // Roberto (3) agotó nutrición, pero puede ir a clínica médica.
        var respuesta = await api.CreateClient().CrearConsulta(3, 1, "Presencial", "2026-12-02T10:00:00");

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    // ---------- Estados ----------

    [Fact]
    public async Task Una_consulta_atendida_no_puede_volver_a_pendiente_ni_cancelarse()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var aPendiente = await cliente.PutAsJsonAsync("/consultas/2/estado", new { estado = "Pendiente" });
        var cancelacion = await cliente.PostAsync("/consultas/2/cancelacion", null);

        Assert.Equal(HttpStatusCode.BadRequest, aPendiente.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cancelacion.StatusCode);
    }

    // ---------- Afiliados ----------

    [Fact]
    public async Task La_baja_de_un_afiliado_es_logica_y_conserva_sus_consultas()
    {
        using var api = new ApiFactory();
        var cliente = api.CreateClient();

        var baja = await cliente.DeleteAsync("/afiliados/8");
        var afiliado = await (await cliente.GetAsync("/afiliados/8")).LeerJson();
        var consultas = await (await cliente.GetAsync("/consultas")).LeerJson();

        Assert.Equal(HttpStatusCode.NoContent, baja.StatusCode);
        Assert.False(afiliado.GetProperty("isActive").GetBoolean());
        Assert.Equal(2, consultas.EnumerateArray().Count(c =>
            c.GetProperty("afiliadoId").ValueKind == System.Text.Json.JsonValueKind.Number &&
            c.GetProperty("afiliadoId").GetInt32() == 8));
    }

    [Fact]
    public async Task Alta_de_afiliado_con_dni_duplicado_devuelve_409()
    {
        using var api = new ApiFactory();
        var respuesta = await api.CreateClient().PostAsJsonAsync("/afiliados", new
        {
            nombreCompleto = "Duplicado", dni = "28445123", email = "dup@test.com"
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }
}
