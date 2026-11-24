using System.Globalization;
using System.Text;
using ConsultasPlan.Models;

namespace ConsultasPlan.Reglas;

/// <summary>
/// Política de cupo: el plan incluye 6 consultas de nutrición por año, no más de 1 por mes.
/// Las consultas no usadas no se acumulan. Aplica a consultas presenciales.
///
/// Criterios adoptados (ver documento técnico):
/// - Año y mes calendario, tomados de la fecha de la consulta (FechaHora), no de su creación.
/// - "No se acumulan": el cupo se calcula siempre sobre el año/mes de la consulta,
///   por lo que lo no usado en un mes o año no pasa al siguiente.
/// - Consumen cupo las consultas presenciales de nutrición en cualquier estado salvo
///   Cancelado (Pendiente, Confirmado, Atendido e Inasistencia).
/// - Las consultas virtuales de nutrición no consumen cupo ni están limitadas.
/// </summary>
public static class CupoNutricion
{
    public const int LimiteAnual = 6;
    public const int LimiteMensual = 1;
    public const string EspecialidadNutricion = "Nutrición";

    /// <summary>Compara la especialidad sin distinguir mayúsculas, tildes ni espacios.</summary>
    public static bool EsNutricion(Profesional? profesional) =>
        profesional != null && Normalizar(profesional.Especialidad) == Normalizar(EspecialidadNutricion);

    /// <summary>Indica si una consulta nueva con este profesional y modalidad está alcanzada por el cupo.</summary>
    public static bool Aplica(Profesional? profesional, ModalidadConsulta modalidad) =>
        modalidad == ModalidadConsulta.Presencial && EsNutricion(profesional);

    /// <summary>Indica si una consulta existente consume cupo.</summary>
    public static bool ConsumeCupo(Consulta consulta) =>
        consulta.Estado != EstadoConsulta.Cancelado && Aplica(consulta.Profesional, consulta.Modalidad);

    /// <summary>
    /// Valida si se puede agendar una consulta alcanzada por el cupo en <paramref name="fechaNueva"/>.
    /// </summary>
    /// <param name="fechasQueConsumenCupo">Fechas de las consultas del afiliado que ya consumen cupo.</param>
    /// <returns>Mensaje de error, o null si hay cupo disponible.</returns>
    public static string? Validar(IEnumerable<DateTime> fechasQueConsumenCupo, DateTime fechaNueva)
    {
        var delAnio = fechasQueConsumenCupo.Where(f => f.Year == fechaNueva.Year).ToList();

        if (delAnio.Count >= LimiteAnual)
            return $"El afiliado ya utilizó las {LimiteAnual} consultas presenciales de nutrición que incluye el plan para {fechaNueva.Year}.";

        if (delAnio.Count(f => f.Month == fechaNueva.Month) >= LimiteMensual)
            return $"El afiliado ya tiene una consulta presencial de nutrición en {NombreMes(fechaNueva)} de {fechaNueva.Year}. El plan permite {LimiteMensual} por mes.";

        return null;
    }

    private static string NombreMes(DateTime fecha) =>
        CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.GetMonthName(fecha.Month);

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinTildes = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(sinTildes.ToArray());
    }
}
