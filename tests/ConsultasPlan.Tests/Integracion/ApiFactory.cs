using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsultasPlan.Servicios;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ConsultasPlan.Tests.Integracion;

/// <summary>
/// Levanta la API completa en memoria con una base SQLite propia (archivo temporal)
/// y un reloj fijo, para que el seed y las validaciones de fecha sean deterministas.
/// Cada test crea su propia instancia: no comparten datos.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>"Hoy" para el seed y la API: 24/11/2026 10:00 hora de Argentina (13:00 UTC).</summary>
    public static readonly DateTimeOffset AhoraUtc = new(2026, 11, 24, 13, 0, 0, TimeSpan.Zero);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _rutaBase = Path.Combine(Path.GetTempPath(), $"consultas-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_rutaBase}");
        builder.ConfigureTestServices(services =>
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            services.RemoveAll<IReloj>();
            services.AddSingleton<IReloj>(new Reloj(new TimeProviderFijo(AhoraUtc), zona));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_rutaBase); } catch (IOException) { /* best effort */ }
    }

    private sealed class TimeProviderFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}

/// <summary>Atajos para los tests.</summary>
public static class HttpClientExtensiones
{
    public static Task<HttpResponseMessage> CrearConsulta(this HttpClient cliente,
        int afiliadoId, int profesionalId, string modalidad, string fechaHora, string motivo = "Test") =>
        cliente.PostAsJsonAsync("/consultas", new { afiliadoId, profesionalId, modalidad, fechaHora, motivo });

    public static async Task<JsonElement> LeerJson(this HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>(ApiFactory.Json));

    public static async Task<string> Mensaje(this HttpResponseMessage respuesta) =>
        (await respuesta.LeerJson()).GetProperty("mensaje").GetString()!;
}
