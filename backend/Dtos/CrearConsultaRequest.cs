using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos que el cliente puede enviar para crear una consulta.
/// Estado, FechaCreacion, Id y navegaciones los define el servidor.
/// </summary>
public class CrearConsultaRequest
{
    public int AfiliadoId { get; set; }
    public int ProfesionalId { get; set; }
    public DateTime FechaHora { get; set; }
    public ModalidadConsulta Modalidad { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
