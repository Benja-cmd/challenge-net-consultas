using ConsultasPlan.Models;

namespace ConsultasPlan.Reglas;

/// <summary>
/// Reglas de negocio que determinan si una consulta puede pasar a un nuevo estado.
/// Cada método devuelve el mensaje de error si la transición no es válida, o null si lo es.
/// </summary>
public static class TransicionesEstadoConsulta
{
    public static readonly TimeSpan AnticipacionMinimaCancelacion = TimeSpan.FromHours(24);

    public static string? Validar(Consulta consulta, EstadoConsulta nuevoEstado, DateTime ahora) => nuevoEstado switch
    {
        EstadoConsulta.Cancelado    => ValidarCancelacion(consulta, ahora),
        EstadoConsulta.Inasistencia => ValidarInasistencia(consulta, ahora),
        EstadoConsulta.Atendido     => ValidarAtencion(consulta, ahora),
        EstadoConsulta.Confirmado   => ValidarConfirmacion(consulta, ahora),
        EstadoConsulta.Pendiente    => "Una consulta no puede volver al estado Pendiente.",
        _                           => $"Estado inválido: {nuevoEstado}."
    };

    public static string? ValidarConfirmacion(Consulta consulta, DateTime ahora)
    {
        if (consulta.Estado != EstadoConsulta.Pendiente)
            return $"Solo se pueden confirmar consultas pendientes (estado actual: {consulta.Estado}).";

        if (consulta.FechaHora <= ahora)
            return "No se puede confirmar una consulta cuya fecha ya pasó.";

        return null;
    }

    /// <summary>Solo las consultas Pendientes o Confirmadas están "abiertas"; el resto son estados finales.</summary>
    public static bool EstaAbierta(Consulta consulta) =>
        consulta.Estado is EstadoConsulta.Pendiente or EstadoConsulta.Confirmado;

    public static string? ValidarCancelacion(Consulta consulta, DateTime ahora)
    {
        if (!EstaAbierta(consulta))
            return $"No se puede cancelar una consulta en estado {consulta.Estado}.";

        if (consulta.FechaHora - ahora < AnticipacionMinimaCancelacion)
            return "No se puede cancelar con menos de 24 horas de anticipación.";

        return null;
    }

    public static string? ValidarInasistencia(Consulta consulta, DateTime ahora)
    {
        if (!EstaAbierta(consulta))
            return $"No se puede registrar una inasistencia sobre una consulta en estado {consulta.Estado}.";

        if (consulta.FechaHora > ahora)
            return "No se puede registrar una inasistencia sobre una consulta futura.";

        return null;
    }

    public static string? ValidarAtencion(Consulta consulta, DateTime ahora)
    {
        if (!EstaAbierta(consulta))
            return $"No se puede registrar la atención de una consulta en estado {consulta.Estado}.";

        if (consulta.FechaHora > ahora)
            return "No se puede registrar la atención de una consulta futura.";

        if (consulta.FechaHora.Year != ahora.Year)
            return "Solo se pueden registrar atenciones de consultas del año en curso.";

        return null;
    }
}
