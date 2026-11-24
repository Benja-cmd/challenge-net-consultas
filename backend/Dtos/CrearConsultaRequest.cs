using System.ComponentModel.DataAnnotations;
using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos que el cliente puede enviar para crear una consulta.
/// Estado, FechaCreacion, Id y navegaciones los define el servidor.
/// </summary>
public class CrearConsultaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un afiliado.")]
    public int AfiliadoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un profesional.")]
    public int ProfesionalId { get; set; }

    [Required(ErrorMessage = "Debe indicar la fecha y hora de la consulta.")]
    public DateTime? FechaHora { get; set; }

    [Required(ErrorMessage = "Debe indicar la modalidad de la consulta.")]
    [EnumDataType(typeof(ModalidadConsulta), ErrorMessage = "Modalidad inválida.")]
    public ModalidadConsulta? Modalidad { get; set; }

    [Required(ErrorMessage = "Debe indicar el motivo de la consulta.")]
    [StringLength(500, ErrorMessage = "El motivo no puede superar los 500 caracteres.")]
    public string Motivo { get; set; } = string.Empty;
}
