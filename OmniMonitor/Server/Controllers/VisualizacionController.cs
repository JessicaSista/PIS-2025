using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Tokenizer;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class VisualizacionController : ControllerBase
{
    private readonly IVisualizacionService _visualizacionService;
    private readonly ISondaAuthService _sondaAuthService;

    public VisualizacionController(IVisualizacionService visualizacionService, ISondaAuthService sondaAuthService)
    {
        _visualizacionService = visualizacionService;
        _sondaAuthService = sondaAuthService;
    }

    /// <summary>
    /// Crea una nueva visualización.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Visualizacion), 201)] // 201 Created
    [ProducesResponseType(400)] // Bad Request
    [ProducesResponseType(500)]
    public async Task<ActionResult<Visualizacion>> CreateVisualizacion([FromBody] CreateVisualizacionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var nuevaVisualizacion = await _visualizacionService.CreateVisualizacionAsync(request);
            // Devuelve una respuesta 201 Created con la ubicación del nuevo recurso
            return CreatedAtAction(nameof(GetVisualizacionById), new { idVisualizacion = nuevaVisualizacion.IdVisualizacion, username = nuevaVisualizacion.Username }, nuevaVisualizacion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al crear la visualización: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene todas las visualizaciones para un usuario específico.
    /// </summary>
    [HttpGet("GetAllVisualizaciones")]
    [ProducesResponseType(typeof(List<Visualizacion>), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<Visualizacion>>> GetAllVisualizaciones(string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            var visualizaciones = await _visualizacionService.GetAllVisualizacionesAsync(username);
            return Ok(visualizaciones);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener las visualizaciones: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene una visualización específica por su ID y nombre de usuario.
    /// </summary>
    [HttpGet("GetVisualizacionById")]
    [ProducesResponseType(typeof(Visualizacion), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Visualizacion>> GetVisualizacionById(int idVisualizacion, string token)
    {
        try
        {
            var (username, password) = await _sondaAuthService.GetUserByTokenOMAsync(token);
            var visualizacion = await _visualizacionService.GetVisualizacionByIdAsync(idVisualizacion, username);
            if (visualizacion == null)
            {
                return NotFound($"No se encontró la visualización con ID {idVisualizacion} para el usuario {username}.");
            }
            return Ok(visualizacion);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al obtener la visualización: {ex.Message}");
        }
    }
}
