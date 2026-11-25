using System.Data;
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
public class ConsultasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IReloj _reloj;
    private readonly CupoServicio _cupo;

    public ConsultasController(AppDbContext context, IReloj reloj, CupoServicio cupo)
    {
        _context = context;
        _reloj = reloj;
        _cupo = cupo;
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
        var fechaHora = _reloj.ALaZonaDelSistema(request.FechaHora!.Value);
        if (fechaHora <= _reloj.Ahora)
            return BadRequest(new { mensaje = "No se puede agendar una consulta en una fecha pasada." });

        // Las validaciones de horario y cupo leen y luego escriben: sin serializar, dos altas
        // simultáneas pueden pasar ambas. En SQLite, Serializable abre la transacción con
        // BEGIN IMMEDIATE (toma el lock de escritura antes de leer); las demás esperan.
        await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var afiliado = await _context.Afiliados.FindAsync(request.AfiliadoId);
        if (afiliado == null || !afiliado.isActive)
            return NotFound(new { mensaje = "Afiliado no encontrado." });

        if (afiliado.Bloqueado)
            return BadRequest(new { mensaje = "El afiliado se encuentra bloqueado para agendar consultas." });

        var profesional = await _context.Profesionales.FindAsync(request.ProfesionalId);
        if (profesional == null)
            return NotFound(new { mensaje = "Profesional no encontrado." });

        var consultaConflicto = await _context.Consultas.AnyAsync(c =>
            c.ProfesionalId == request.ProfesionalId &&
            c.FechaHora == fechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (consultaConflicto)
            return BadRequest(new { mensaje = "El profesional ya tiene una consulta en ese horario." });

        var afiliadoConflicto = await _context.Consultas.AnyAsync(c =>
            c.AfiliadoId == request.AfiliadoId &&
            c.FechaHora == fechaHora &&
            c.Estado != EstadoConsulta.Cancelado);
        if (afiliadoConflicto)
            return BadRequest(new { mensaje = "El afiliado ya tiene una consulta en ese horario." });

        var errorCupo = await _cupo.ValidarAsync(afiliado, profesional, request.Modalidad!.Value, fechaHora);
        if (errorCupo != null)
            return BadRequest(new { mensaje = errorCupo });

        var consulta = new Consulta
        {
            AfiliadoId = request.AfiliadoId,
            ProfesionalId = request.ProfesionalId,
            FechaHora = fechaHora,
            Modalidad = request.Modalidad!.Value,
            Motivo = request.Motivo.Trim(),
            FechaCreacion = _reloj.Ahora,
            Estado = EstadoConsulta.Pendiente
        };
        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();
        await transaccion.CommitAsync();
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

        var error = TransicionesEstadoConsulta.Validar(consulta, nuevoEstado, _reloj.Ahora);
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
