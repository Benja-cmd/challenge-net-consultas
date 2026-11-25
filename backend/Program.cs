using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Servicios;

var builder = WebApplication.CreateBuilder(args);

var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(
    builder.Configuration["ZonaHoraria"] ?? "America/Argentina/Buenos_Aires");
builder.Services.AddSingleton<IReloj>(new Reloj(TimeProvider.System, zonaHoraria));

builder.Services.AddScoped<CupoServicio>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=consultas.db"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    MigradorBaseDatos.Migrar(db);
    DbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<IReloj>().Ahora);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();
app.Run();

// Expuesto para los tests de integración (WebApplicationFactory<Program>).
public partial class Program { }
