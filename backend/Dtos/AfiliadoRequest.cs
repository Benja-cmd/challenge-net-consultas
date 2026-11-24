namespace ConsultasPlan.Dtos;

/// <summary>
/// Datos editables de un afiliado. Plan, bloqueo, consultas usadas, alta y
/// estado activo los administra el servidor.
/// </summary>
public class AfiliadoRequest
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
}
