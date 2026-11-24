using System.ComponentModel.DataAnnotations;

namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos editables de un afiliado. Plan, bloqueo, consultas usadas, alta y
/// estado activo los administra el servidor.
/// </summary>
public class AfiliadoRequest
{
    [Required(ErrorMessage = "Debe indicar el nombre completo.")]
    [StringLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe indicar el DNI.")]
    [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI debe tener 7 u 8 dígitos, sin puntos.")]
    public string DNI { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe indicar el email.")]
    [EmailAddress(ErrorMessage = "El email no es válido.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [RegularExpression(@"^\d{8,15}$", ErrorMessage = "El teléfono debe tener entre 8 y 15 dígitos, sin espacios ni guiones.")]
    public string Telefono { get; set; } = string.Empty;
}
