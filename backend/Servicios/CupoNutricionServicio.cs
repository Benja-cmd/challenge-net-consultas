using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Servicios;

/// <summary>Obtiene de la base los datos que necesita la regla de cupo.</summary>
public class CupoNutricionServicio
{
    // Transitorio: la política pasa a leerse de la base según el plan en el próximo cambio.
    public static readonly PoliticaCupo PoliticaNutricion = new()
    {
        Plan = "Plan Integral", Especialidad = "Nutrición", Modalidad = ModalidadConsulta.Presencial,
        LimiteAnual = 6, LimiteMensual = 1, InasistenciaConsumeCupo = true
    };

    private readonly AppDbContext _context;

    public CupoNutricionServicio(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Consultas del afiliado en el año indicado que consumen cupo de nutrición.</summary>
    public async Task<List<Consulta>> ConsultasQueConsumenCupoAsync(int afiliadoId, int anio)
    {
        // El filtro de especialidad se aplica en memoria porque normaliza tildes/mayúsculas;
        // el volumen por afiliado y año es acotado.
        var candidatas = await _context.Consultas
            .Include(c => c.Profesional)
            .Where(c => c.AfiliadoId == afiliadoId
                        && c.Modalidad == ModalidadConsulta.Presencial
                        && c.Estado != EstadoConsulta.Cancelado
                        && c.FechaHora.Year == anio)
            .ToListAsync();

        return candidatas.Where(c => ReglasCupo.ConsumeCupo(PoliticaNutricion, c)).ToList();
    }

    /// <summary>Devuelve el mensaje de error si la nueva consulta excede el cupo, o null.</summary>
    public async Task<string?> ValidarAsync(int afiliadoId, Profesional profesional, ModalidadConsulta modalidad, DateTime fechaHora)
    {
        if (modalidad != PoliticaNutricion.Modalidad || !ReglasCupo.MismoTexto(profesional.Especialidad, PoliticaNutricion.Especialidad))
            return null;

        var consumidas = await ConsultasQueConsumenCupoAsync(afiliadoId, fechaHora.Year);
        return ReglasCupo.Validar(PoliticaNutricion, consumidas.Select(c => c.FechaHora), fechaHora);
    }
}
