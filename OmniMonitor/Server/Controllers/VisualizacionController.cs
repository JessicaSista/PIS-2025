using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
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

                Visualizacion nuevaVisualizacion = await _visualizacionService.CreateVisualizacionAsync(request);

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
                List<Visualizacion> visualizaciones = await _visualizacionService.GetAllVisualizacionesAsync(username);
                return Ok(visualizaciones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener las visualizaciones: {ex.Message}");
            }
        }

                /// <summary>
        /// Obtiene todas las visualizaciones de un usuario específico con paginación.
        /// </summary>
        [HttpGet("GetAllVisualizacionesPaginated")]
        [ProducesResponseType(typeof(PaginatedVisualizacionDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> GetAllVisualizacionesPaginated(
            string token,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? query = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token requerido.");
            if (page <= 0 || pageSize <= 0)
                return BadRequest("La página y el tamaño deben ser mayores a 0.");
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest("Token inválido o usuario no encontrado.");

                var items = await _visualizacionService.GetAllVisualizacionesPaginatedAsync(username, page, pageSize, query);
                var totalCount = await _visualizacionService.GetVisualizacionesCountAsync(username, query);
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                var result = new {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };
                return Ok(result);
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
                Visualizacion? visualizacion = await _visualizacionService.GetVisualizacionByIdAsync(idVisualizacion, username);
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

        [HttpGet("GetVisualizacionByIdSinToken")]
        [ProducesResponseType(typeof(Visualizacion), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Visualizacion>> GetVisualizacionByIdSinToken(int idVisualizacion)
        {
            try
            {
                Visualizacion? visualizacion = await _visualizacionService.GetVisualizacionByIdAsyncSinToken(idVisualizacion);
                if (visualizacion == null)
                {
                    return NotFound($"No se encontró la visualización con ID {idVisualizacion}");
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
        public async Task<IActionResult> DeleteVisualizacion(int idVisualizacion, [FromQuery] string token)
        {
            ArgumentNullException.ThrowIfNull(token);
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Token inválido o usuario no encontrado.");
                }

                using (IServiceScope scope = HttpContext.RequestServices.CreateScope())
                {
                    Context.ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<Context.ApplicationDbContext>();
                    Visualizacion? visualizacion = await db.Visualizaciones
                        .Include(v => v.GrupoDatasets)
                        .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion);
                    if (visualizacion == null)
                    {
                        return NotFound($"No se encontró la visualización con ID {idVisualizacion}.");
                    }

                    // Eliminar todos los GrupoVisualizacion asociados
                    IQueryable<GrupoVisualizacion> gruposVisualizacion = db.GrupoVisualizaciones.Where(gv => gv.IdVisualizacion == idVisualizacion);
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
            ArgumentNullException.ThrowIfNull(token);
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);
                if (string.IsNullOrEmpty(username))
                {
                    return Unauthorized("Token inválido o usuario no encontrado.");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                using IServiceScope scope = HttpContext.RequestServices.CreateScope();
                Context.ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<Context.ApplicationDbContext>();
                Visualizacion? visualizacion = await db.Visualizaciones
                    .Include(v => v.GrupoDatasets)
                    .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion);
                if (visualizacion == null)
                {
                    return NotFound($"No se encontró la visualización con ID {idVisualizacion}.");
                }

                // Validar nombre único (excepto la propia visualización)
                bool nombreDuplicado = await db.Visualizaciones
                    .AnyAsync(v => v.IdVisualizacion != idVisualizacion && v.Nombre == request.Nombre);
                if (nombreDuplicado)
                {
                    return BadRequest($"Ya existe otra visualización con el nombre '{request.Nombre}'.");
                }

                // Validar fechas
                if (request.FechaDesde > request.FechaHasta)
                {
                    return BadRequest("La fecha de inicio debe ser anterior o igual a la fecha de fin.");
                }
                if (request.FechaDesde == default || request.FechaHasta == default)
                {
                    return BadRequest("Las fechas de inicio y fin deben ser válidas.");
                }

                // Actualizar campos principales
                visualizacion.Nombre = request.Nombre;
                visualizacion.FechaDesde = request.FechaDesde;
                visualizacion.FechaHasta = request.FechaHasta;
                visualizacion.JsonDesign = request.JsonDiseñoGeneral;

                // --- Sincronizar GrupoDatasets ---
                var requestDatasetIds = request.Datasets.Select(ds => ds.DatasetId).ToHashSet();
                var toRemove = visualizacion.GrupoDatasets.Where(gd => !requestDatasetIds.Contains(gd.DatasetId)).ToList();
                foreach (GrupoDataset? gd in toRemove)
                {
                    db.GrupoDatasets.Remove(gd);
                }

                // Actualizar o agregar los que están en el request
                foreach (DatasetConfig ds in request.Datasets)
                {
                    // Validar que el dataset exista en la tabla DatasetIM
                    // se cambia por datasets en general
                    bool datasetExiste = db.Datasets.Any(d => d.Id == ds.DatasetId);
                    if (!datasetExiste)
                    {
                        return BadRequest($"El dataset con ID {ds.DatasetId} no existe.");
                    }

                    GrupoDataset? existing = visualizacion.GrupoDatasets.FirstOrDefault(gd => gd.DatasetId == ds.DatasetId);
                    if (existing != null)
                    {
                        existing.JsonDesign = ds.JsonDiseño;
                    }
                    else
                    {
                        visualizacion.GrupoDatasets.Add(new GrupoDataset
                        {
                            DatasetId = ds.DatasetId,
                            JsonDesign = ds.JsonDiseño,
                        });
                    }
                }

                await db.SaveChangesAsync();
                return Ok(visualizacion);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al editar la visualización: {ex.Message}");
            }
        }


        [HttpPost("visualization-data")]
        [ProducesResponseType(typeof(VisualizationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetVisualizationData([FromQuery] string token, [FromBody] VisualizationRequest request)
        {
            ArgumentNullException.ThrowIfNull(token);

            try
            {
                
                string username = await _sondaAuthService.GetUserByTokenOMAsync(token);

                var response = await _visualizacionService.GetVisualizationDataAsync(request, username);

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al generar los datos de visualización: {ex.Message}");
            }
        }


    }
}
