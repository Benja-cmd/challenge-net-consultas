using ConsultasPlan.Models;

namespace ConsultasPlan.Dtos;

/// <summary>Estado del cupo de un afiliado para una especialidad y modalidad en un año.</summary>
public class CupoResponse
{
    public string Plan { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public ModalidadConsulta Modalidad { get; set; }

    /// <summary>Texto para mostrar, ej.: "consultas presenciales de nutrición".</summary>
    public string Descripcion { get; set; } = string.Empty;

    public int Anio { get; set; }
    public int LimiteAnual { get; set; }
    public int LimiteMensual { get; set; }
    public bool InasistenciaConsumeCupo { get; set; }
    public int Usadas { get; set; }
    public int Disponibles { get; set; }

    /// <summary>Meses (1-12) que ya tienen una consulta que consume cupo.</summary>
    public List<int> MesesUtilizados { get; set; } = new();
}
