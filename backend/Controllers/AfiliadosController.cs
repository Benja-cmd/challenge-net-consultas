using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Servicios;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class AfiliadosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IReloj _reloj;
    private readonly CupoServicio _cupo;

    public AfiliadosController(AppDbContext context, IReloj reloj, CupoServicio cupo)
    {
        _context = context;
        _reloj = reloj;
        _cupo = cupo;
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

    /// <summary>
    /// Estado del cupo que aplicaría a una consulta del afiliado con ese profesional y modalidad
    /// (según la política de su plan). Por defecto, el año en curso.
    /// Devuelve 204 si esa combinación no está limitada por ningún cupo.
    /// </summary>
    [HttpGet("{id}/cupo")]
    [ProducesResponseType(typeof(CupoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetCupo(int id, [FromQuery] int profesionalId,
        [FromQuery] ModalidadConsulta? modalidad, [FromQuery] int? anio)
    {
        if (modalidad == null)
            return BadRequest(new { mensaje = "Debe indicar la modalidad." });

        var afiliado = await _context.Afiliados.FindAsync(id);
        if (afiliado == null) return NotFound(new { mensaje = "Afiliado no encontrado." });

        var profesional = await _context.Profesionales.FindAsync(profesionalId);
        if (profesional == null) return NotFound(new { mensaje = "Profesional no encontrado." });

        var cupo = await _cupo.ObtenerEstadoAsync(afiliado, profesional, modalidad.Value, anio ?? _reloj.Ahora.Year);
        return cupo == null ? NoContent() : Ok(cupo);
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
