namespace ConsultasPlan.Dtos;

/// <summary>Estado del cupo de nutrición presencial de un afiliado en un año.</summary>
public class CupoNutricionResponse
{
    public int Anio { get; set; }
    public int LimiteAnual { get; set; }
    public int LimiteMensual { get; set; }
    public int Usadas { get; set; }
    public int Disponibles { get; set; }

    /// <summary>Meses (1-12) que ya tienen una consulta que consume cupo.</summary>
    public List<int> MesesUtilizados { get; set; } = new();
}
