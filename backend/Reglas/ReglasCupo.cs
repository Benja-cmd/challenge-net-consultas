using System.Globalization;
using System.Text;
using ConsultasPlan.Models;

namespace ConsultasPlan.Reglas;

/// <summary>
/// Reglas de cupo de consultas, parametrizadas por una <see cref="PoliticaCupo"/>.
/// Clase pura (sin acceso a datos ni a la hora actual) para poder testearla aislada.
///
/// Criterios (ver documento técnico):
/// - Año y mes calendario, tomados de la fecha de la consulta (FechaHora), no de su creación.
/// - "No se acumulan": el cupo se calcula siempre sobre el año/mes de la consulta,
///   por lo que lo no usado en un mes o año no pasa al siguiente.
/// - Consumen cupo las consultas alcanzadas por la política en cualquier estado salvo
///   Cancelado; la Inasistencia según la política (por defecto, sí).
/// </summary>
public static class ReglasCupo
{
    /// <summary>Indica si la política aplica a un afiliado de <paramref name="plan"/> que se atiende con el profesional y modalidad dados.</summary>
    public static bool Aplica(PoliticaCupo politica, string plan, Profesional? profesional, ModalidadConsulta modalidad) =>
        profesional != null
        && politica.Modalidad == modalidad
        && MismoTexto(politica.Plan, plan)
        && MismoTexto(politica.Especialidad, profesional.Especialidad);

    /// <summary>Indica si una consulta existente del afiliado consume el cupo de la política.</summary>
    public static bool ConsumeCupo(PoliticaCupo politica, Consulta consulta) =>
        consulta.Profesional != null
        && consulta.Modalidad == politica.Modalidad
        && MismoTexto(politica.Especialidad, consulta.Profesional.Especialidad)
        && consulta.Estado switch
        {
            EstadoConsulta.Cancelado    => false,
            EstadoConsulta.Inasistencia => politica.InasistenciaConsumeCupo,
            _                           => true
        };

    /// <summary>
    /// Valida si se puede agendar una consulta alcanzada por la política en <paramref name="fechaNueva"/>.
    /// </summary>
    /// <param name="fechasQueConsumenCupo">Fechas de las consultas del afiliado que ya consumen este cupo.</param>
    /// <returns>Mensaje de error, o null si hay cupo disponible.</returns>
    public static string? Validar(PoliticaCupo politica, IEnumerable<DateTime> fechasQueConsumenCupo, DateTime fechaNueva)
    {
        var delAnio = fechasQueConsumenCupo.Where(f => f.Year == fechaNueva.Year).ToList();
        var consultas = Descripcion(politica);

        if (delAnio.Count >= politica.LimiteAnual)
            return $"El afiliado ya utilizó las {politica.LimiteAnual} {consultas} que incluye su plan para {fechaNueva.Year}.";

        if (delAnio.Count(f => f.Month == fechaNueva.Month) >= politica.LimiteMensual)
            return $"El afiliado ya utilizó el cupo de {consultas} de {NombreMes(fechaNueva)} de {fechaNueva.Year}. Su plan permite {politica.LimiteMensual} por mes.";

        return null;
    }

    /// <summary>Ej.: "consultas presenciales de nutrición".</summary>
    public static string Descripcion(PoliticaCupo politica)
    {
        var modalidad = politica.Modalidad == ModalidadConsulta.Presencial ? "presenciales" : "virtuales";
        return $"consultas {modalidad} de {politica.Especialidad.Trim().ToLower(CultureInfo.GetCultureInfo("es-AR"))}";
    }

    /// <summary>Compara sin distinguir mayúsculas, tildes ni espacios en los extremos.</summary>
    public static bool MismoTexto(string? a, string? b) => Normalizar(a) == Normalizar(b);

    private static string NombreMes(DateTime fecha) =>
        CultureInfo.GetCultureInfo("es-AR").DateTimeFormat.GetMonthName(fecha.Month);

    private static string Normalizar(string? texto)
    {
        var descompuesto = (texto ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sinTildes = descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(sinTildes.ToArray());
    }
}
