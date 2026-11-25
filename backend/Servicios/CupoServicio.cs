using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Servicios;

/// <summary>
/// Resuelve qué política de cupo aplica a una consulta (según el plan del afiliado, la
/// especialidad del profesional y la modalidad) y obtiene de la base los datos que
/// necesita <see cref="ReglasCupo"/>.
/// </summary>
public class CupoServicio
{
    private readonly AppDbContext _context;

    public CupoServicio(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Política que aplica, o null si la consulta no está limitada por ningún cupo.</summary>
    public async Task<PoliticaCupo?> ObtenerPoliticaAsync(Afiliado afiliado, Profesional profesional, ModalidadConsulta modalidad)
    {
        // Pocas políticas: se filtran en memoria para comparar texto sin tildes ni mayúsculas.
        var politicas = await _context.PoliticasCupo.AsNoTracking()
            .Where(p => p.Modalidad == modalidad)
            .ToListAsync();

        return politicas.FirstOrDefault(p => ReglasCupo.Aplica(p, afiliado.Plan, profesional, modalidad));
    }

    /// <summary>Consultas del afiliado en el año indicado que consumen el cupo de la política.</summary>
    public async Task<List<Consulta>> ConsultasQueConsumenCupoAsync(int afiliadoId, PoliticaCupo politica, int anio)
    {
        var candidatas = await _context.Consultas
            .Include(c => c.Profesional)
            .Where(c => c.AfiliadoId == afiliadoId
                        && c.Modalidad == politica.Modalidad
                        && c.Estado != EstadoConsulta.Cancelado
                        && c.FechaHora.Year == anio)
            .ToListAsync();

        return candidatas.Where(c => ReglasCupo.ConsumeCupo(politica, c)).ToList();
    }

    /// <summary>Devuelve el mensaje de error si la nueva consulta excede el cupo, o null.</summary>
    public async Task<string?> ValidarAsync(Afiliado afiliado, Profesional profesional, ModalidadConsulta modalidad, DateTime fechaHora)
    {
        var politica = await ObtenerPoliticaAsync(afiliado, profesional, modalidad);
        if (politica == null)
            return null;

        var consumidas = await ConsultasQueConsumenCupoAsync(afiliado.Id, politica, fechaHora.Year);
        return ReglasCupo.Validar(politica, consumidas.Select(c => c.FechaHora), fechaHora);
    }

    /// <summary>Estado del cupo para una consulta hipotética, o null si no aplica ningún cupo.</summary>
    public async Task<CupoResponse?> ObtenerEstadoAsync(Afiliado afiliado, Profesional profesional, ModalidadConsulta modalidad, int anio)
    {
        var politica = await ObtenerPoliticaAsync(afiliado, profesional, modalidad);
        if (politica == null)
            return null;

        var consumidas = await ConsultasQueConsumenCupoAsync(afiliado.Id, politica, anio);
        return new CupoResponse
        {
            Plan = politica.Plan,
            Especialidad = politica.Especialidad,
            Modalidad = politica.Modalidad,
            Descripcion = ReglasCupo.Descripcion(politica),
            Anio = anio,
            LimiteAnual = politica.LimiteAnual,
            LimiteMensual = politica.LimiteMensual,
            InasistenciaConsumeCupo = politica.InasistenciaConsumeCupo,
            Usadas = consumidas.Count,
            Disponibles = Math.Max(0, politica.LimiteAnual - consumidas.Count),
            MesesUtilizados = consumidas.Select(c => c.FechaHora.Month).Distinct().Order().ToList()
        };
    }
}
