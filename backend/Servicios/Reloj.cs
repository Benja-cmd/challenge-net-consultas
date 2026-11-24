namespace ConsultasPlan.Servicios;

/// <summary>
/// Fuente única de la fecha/hora actual del sistema.
/// Todas las fechas de negocio (turnos, creación, alta) se manejan en la hora local
/// de la zona configurada (por defecto Argentina), independientemente de la zona
/// horaria del servidor donde corra la API.
/// </summary>
public interface IReloj
{
    /// <summary>Fecha y hora actual en la zona horaria del sistema (Kind = Unspecified).</summary>
    DateTime Ahora { get; }

    /// <summary>
    /// Lleva una fecha recibida a la zona horaria del sistema. Si viene sin zona
    /// (Kind = Unspecified) se asume que ya está expresada en hora local del sistema.
    /// </summary>
    DateTime ALaZonaDelSistema(DateTime fecha);
}

public class Reloj : IReloj
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _zona;

    public Reloj(TimeProvider timeProvider, TimeZoneInfo zona)
    {
        _timeProvider = timeProvider;
        _zona = zona;
    }

    public DateTime Ahora => TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _zona).DateTime;

    public DateTime ALaZonaDelSistema(DateTime fecha) =>
        fecha.Kind == DateTimeKind.Unspecified
            ? fecha
            : DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(fecha, _zona), DateTimeKind.Unspecified);
}
