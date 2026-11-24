using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ConsultasPlan.Data;

/// <summary>
/// Aplica las migraciones pendientes. Contempla bases creadas por versiones
/// anteriores del sistema con EnsureCreated (sin tabla __EFMigrationsHistory):
/// en ese caso registra la migración inicial como ya aplicada, porque su esquema
/// es idéntico, y luego aplica el resto sin recrear tablas ni perder datos.
/// </summary>
public static class MigradorBaseDatos
{
    public static void Migrar(AppDbContext db)
    {
        if (ExisteTabla(db, "Afiliados") && !ExisteTabla(db, "__EFMigrationsHistory"))
            RegistrarMigracionInicialComoAplicada(db);

        db.Database.Migrate();
    }

    private static void RegistrarMigracionInicialComoAplicada(AppDbContext db)
    {
        var historial = db.GetService<IHistoryRepository>();
        var migracionInicial = db.Database.GetMigrations().First();
        var versionEf = typeof(DbContext).Assembly.GetName().Version!.ToString(3);

        db.Database.ExecuteSqlRaw(historial.GetCreateScript());
        db.Database.ExecuteSqlRaw(historial.GetInsertScript(new HistoryRow(migracionInicial, versionEf)));
    }

    private static bool ExisteTabla(AppDbContext db, string nombre)
    {
        var conexion = db.Database.GetDbConnection();
        var abrir = conexion.State != ConnectionState.Open;
        if (abrir) conexion.Open();
        try
        {
            using var comando = conexion.CreateCommand();
            comando.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $nombre";
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "$nombre";
            parametro.Value = nombre;
            comando.Parameters.Add(parametro);
            return Convert.ToInt64(comando.ExecuteScalar()) > 0;
        }
        finally
        {
            if (abrir) conexion.Close();
        }
    }
}
