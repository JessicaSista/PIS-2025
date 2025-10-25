using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
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
            string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
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

    /// <summary>
    /// Elimina una visualización por su ID.
    /// </summary>
    [HttpDelete("{idVisualizacion}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteVisualizacion(int idVisualizacion)
    {
        try
        {
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OmniMonitor.Server.Context.ApplicationDbContext>();
                var visualizacion = await db.Visualizaciones
                    .Include(v => v.GrupoDatasets)
                    .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion);
                if (visualizacion == null)
                {
                    return NotFound($"No se encontró la visualización con ID {idVisualizacion}.");
                }
                // Eliminar todos los GrupoVisualizacion asociados
                var gruposVisualizacion = db.GrupoVisualizaciones.Where(gv => gv.IdVisualizacion == idVisualizacion);
                db.GrupoVisualizaciones.RemoveRange(gruposVisualizacion);
                // Eliminar todos los GrupoDataset asociados
                db.GrupoDatasets.RemoveRange(visualizacion.GrupoDatasets);
                db.Visualizaciones.Remove(visualizacion);
                await db.SaveChangesAsync();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno al eliminar la visualización: {ex.Message}");
        }
    }

    /// <summary>
    /// Edita una visualización existente por su ID.
    /// </summary>
    [HttpPut("{idVisualizacion}")]
    [ProducesResponseType(typeof(Visualizacion), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> EditVisualizacion(int idVisualizacion, [FromQuery] string token, [FromBody] CreateVisualizacionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<OmniMonitor.Server.Context.ApplicationDbContext>();
                var visualizacion = await db.Visualizaciones
                    .Include(v => v.GrupoDatasets)
                    .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion);
                if (visualizacion == null)
                    return NotFound($"No se encontró la visualización con ID {idVisualizacion}.");

                // Actualizar campos principales
                visualizacion.Nombre = request.Nombre;
                visualizacion.Username = request.Username;
                visualizacion.FechaDesde = request.FechaDesde;
                visualizacion.FechaHasta = request.FechaHasta;
                visualizacion.JsonDesign = request.JsonDiseñoGeneral;

                // --- Sincronizar GrupoDatasets ---
                var requestDatasetIds = request.Datasets.Select(ds => ds.DatasetId).ToHashSet();
                var toRemove = visualizacion.GrupoDatasets.Where(gd => !requestDatasetIds.Contains(gd.DatasetId)).ToList();
                foreach (var gd in toRemove)
                    db.GrupoDatasets.Remove(gd);

                // Actualizar o agregar los que están en el request
                foreach (var ds in request.Datasets)
                {
                    // Validar que el dataset exista en la tabla DatasetIM
                    var datasetExiste = db.DatasetsIM.Any(d => d.Id == ds.DatasetId);
                    if (!datasetExiste)
                    {
                        return BadRequest($"El dataset con ID {ds.DatasetId} no existe.");
                    }
                    var existing = visualizacion.GrupoDatasets.FirstOrDefault(gd => gd.DatasetId == ds.DatasetId);
                    if (existing != null)
                    {
                        existing.JsonDesign = ds.JsonDiseño;
                    }
                    else
                    {
                        visualizacion.GrupoDatasets.Add(new GrupoDataset
                        {
                            DatasetId = ds.DatasetId,
                            JsonDesign = ds.JsonDiseño
                        });
                    }
                }

                await db.SaveChangesAsync();
                return Ok(visualizacion);
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, $"Error interno al editar la visualización: {ex.Message}");
        }
    }

} // cierre de la clase VisualizacionController
