namespace ConsultasPlan.Models;

/// <summary>
/// Límite de consultas que incluye un plan para una especialidad y modalidad.
/// Ej.: "Plan Integral" incluye 6 consultas presenciales de Nutrición por año, máximo 1 por mes.
/// Los períodos son año y mes calendario según la fecha de la consulta; lo no usado no se acumula.
/// </summary>
public class PoliticaCupo
{
    public int Id { get; set; }

    /// <summary>Nombre del plan al que aplica (coincide con Afiliado.Plan).</summary>
    public string Plan { get; set; } = string.Empty;

    /// <summary>Especialidad del profesional (coincide con Profesional.Especialidad).</summary>
    public string Especialidad { get; set; } = string.Empty;

    public ModalidadConsulta Modalidad { get; set; }

    public int LimiteAnual { get; set; }

    public int LimiteMensual { get; set; }

    /// <summary>Si una inasistencia consume el turno del cupo (por defecto sí).</summary>
    public bool InasistenciaConsumeCupo { get; set; } = true;
}
