using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Dtos;
using ConsultasPlan.Models;
using ConsultasPlan.Reglas;

namespace ConsultasPlan.Controllers;

[ApiController]
[Route("[controller]")]
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;

    public ConsultasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var consultas = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .ToListAsync();
        return Ok(consultas);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var consulta = await _context.Consultas
            .Include(c => c.Afiliado)
            .Include(c => c.Profesional)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (consulta == null) return NotFound();
        return Ok(consulta);
    }

    [HttpPost]
    public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaRequest request)
    {
        if (request.FechaHora <= DateTime.Now)
            return BadRequest(new { mensaje = "No se puede agendar una consulta en una fecha pasada." });

        var afiliado = await _context.Afiliados.FindAsync(request.AfiliadoId);
        if (afiliado == null || !afiliado.isActive)
            return NotFound(new { mensaje = "Afiliado no encontrado." });

        if (afiliado.Bloqueado)
            return BadRequest(new { mensaje = "El afiliado se encuentra bloqueado para agendar consultas." });

        var profesionalExiste = await _context.Profesionales.AnyAsync(p => p.Id == request.ProfesionalId);
        if (!profesionalExiste)
            return NotFound(new { mensaje = "Profesional no encontrado." });

        var consultaConflicto = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == request.ProfesionalId &&
            c.FechaHora == request.FechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (consultaConflicto)
            return BadRequest(new { mensaje = "El profesional ya tiene una consulta en ese horario." });

        var afiliadoConflicto = await _context.Consultas.AnyAsync(c =>
            c.AfiliadoId == request.AfiliadoId &&
            c.FechaHora == request.FechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (afiliadoConflicto)
            return BadRequest(new { mensaje = "El afiliado ya tiene una consulta en ese horario." });

        var consulta = new Consulta
        {
            AfiliadoId = request.AfiliadoId,
            ProfesionalId = request.ProfesionalId,
            FechaHora = request.FechaHora!.Value,
            Modalidad = request.Modalidad!.Value,
            Motivo = request.Motivo.Trim(),
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoConsulta.Pendiente
        };
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = consulta.Id }, consulta);
    }

    [HttpPost("{id}/cancelacion")]
    public Task<IActionResult> CancelarConsulta(int id) =>
        CambiarEstado(id, EstadoConsulta.Cancelado);

    [HttpPost("{id}/inasistencia")]
    public Task<IActionResult> MarcarInasistencia(int id) =>
        CambiarEstado(id, EstadoConsulta.Inasistencia);

    [HttpPost("{id}/atencion")]
    public Task<IActionResult> RegistrarAtencion(int id) =>
        CambiarEstado(id, EstadoConsulta.Atendido);

    [HttpPut("{id}/estado")]
    public Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request) =>
        CambiarEstado(id, request.Estado);

    private async Task<IActionResult> CambiarEstado(int id, EstadoConsulta nuevoEstado)
    {
        var consulta = await _context.Consultas.FindAsync(id);
        if (consulta == null) return NotFound();

        var error = TransicionesEstadoConsulta.Validar(consulta, nuevoEstado, DateTime.Now);
        if (error != null)
            return BadRequest(new { mensaje = error });

        consulta.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return Ok(consulta);
    }
}

public class ActualizarEstadoRequest
{
    public EstadoConsulta Estado { get; set; }
}
