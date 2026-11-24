using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Reglas;
using ConsultasPlan.Servicios;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IReloj _reloj;
    private readonly CupoNutricionServicio _cupoNutricion;

    public AfiliadosController(AppDbContext context, IReloj reloj, CupoNutricionServicio cupoNutricion)
    {
        _context = context;
        _reloj = reloj;
        _cupoNutricion = cupoNutricion;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var afiliados = await _context.Afiliados.Where(a => a.isActive).ToListAsync();
        return Ok(afiliados);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null) return NotFound();
        return Ok(afiliado);
    }

    /// <summary>Cupo de consultas presenciales de nutrición del afiliado. Por defecto, el año en curso.</summary>
    [HttpGet("{id}/cupo-nutricion")]
    public async Task<ActionResult<CupoNutricionResponse>> GetCupoNutricion(int id, [FromQuery] int? anio)
    {
        if (!await _context.Afiliados.AnyAsync(a => a.Id == id)) return NotFound();

        var anioConsulta = anio ?? _reloj.Ahora.Year;
        var consumidas = await _cupoNutricion.ConsultasQueConsumenCupoAsync(id, anioConsulta);

        return new CupoNutricionResponse
        {
            Anio = anioConsulta,
            LimiteAnual = CupoNutricion.LimiteAnual,
            LimiteMensual = CupoNutricion.LimiteMensual,
            Usadas = consumidas.Count,
            Disponibles = Math.Max(0, CupoNutricion.LimiteAnual - consumidas.Count),
            MesesUtilizados = consumidas.Select(c => c.FechaHora.Month).Distinct().Order().ToList()
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AfiliadoRequest request)
    {
        if (await _context.Afiliados.AnyAsync(a => a.DNI == request.DNI))
            return Conflict(new { mensaje = "Ya existe un afiliado con ese DNI." });

        var afiliado = new Afiliado
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            DNI = request.DNI,
            Email = request.Email.Trim(),
            Telefono = request.Telefono,
            createdAt = _reloj.Ahora,
            isActive = true
        };
        _context.Afiliados.Add(afiliado);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = afiliado.Id }, afiliado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] AfiliadoRequest request)
    {
        var existing = await _context.Afiliados.FindAsync(id);
        if (existing == null || !existing.isActive) return NotFound();

        if (await _context.Afiliados.AnyAsync(a => a.DNI == request.DNI && a.Id != id))
            return Conflict(new { mensaje = "Ya existe otro afiliado con ese DNI." });

        existing.NombreCompleto = request.NombreCompleto.Trim();
        existing.DNI = request.DNI;
        existing.Email = request.Email.Trim();
        existing.Telefono = request.Telefono;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null || !afiliado.isActive) return NotFound();

        // Baja lógica: se conserva el registro para no perder el historial de consultas.
        afiliado.isActive = false;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
