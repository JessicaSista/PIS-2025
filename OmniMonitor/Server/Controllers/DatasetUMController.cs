using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DatasetUMController : ControllerBase
    {
        private readonly IDatasetUMService _datasetUMService;
        private readonly ISondaAuthService _sondaAuthService;
        private readonly ApplicationDbContext _context;

        public DatasetUMController(IDatasetUMService datasetUMService, ISondaAuthService sondaAuthService, ApplicationDbContext context)
        {
            _context = context;
            _datasetUMService = datasetUMService;
            _sondaAuthService = sondaAuthService;
        }

        /// <summary>
        /// Crea un nuevo dataset.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [ProducesResponseType(typeof(DatasetUM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetUM>> CreateDataset([FromBody] CreateDatasetUMRequest request)
        {
            try
            {
                var username = User.Identity?.Name;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }


                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.UrbanMonitor);
                Datasets dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);
                DatasetUM newDataset = await _datasetUMService.CreateDatasetUMAsync(request, dataset.Id);
                await _datasetUMService.UpdateDatasetAsyncUM(dataset.Id, requestDataset, newDataset);
                return CreatedAtAction(nameof(GetDatasetById), new { datasetId = newDataset.Id, username = newDataset.Username }, newDataset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al crear el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all datasets for a specific user.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDatasets")]
        [ProducesResponseType(typeof(List<DatasetUM>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetUM>>> GetAllDatasets()
        {
            try
            {
                var username = User.Identity?.Name;
                List<DatasetUM> datasets = await _datasetUMService.GetAllDatasetsUMAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a specific dataset by its ID and username.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetDatasetById")]
        [ProducesResponseType(typeof(DatasetUM), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetUM>> GetDatasetById(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;
                DatasetUM? dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("{datasetId}")]
        [ProducesResponseType(typeof(DatasetUM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetUM>> UpdateDataset(int datasetId, [FromBody] CreateDatasetUMRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                DatasetUM? existingDataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
                }
                await _datasetUMService.ValidateDatasetNameAsync(request.Name, request.Username, ModuleType.UrbanMonitor, existingDataset.DatasetId);

                var requestDataset = new CreateDatasetRequest(request.Name, request.Username, ModuleType.UrbanMonitor);
                DatasetUM updatedDataset = await _datasetUMService.UpdateDatasetUMAsync(datasetId, request);
                int id = updatedDataset.DatasetId;
                Datasets dataset = await _datasetUMService.UpdateDatasetAsyncUM(id, requestDataset, updatedDataset);
                return Ok(updatedDataset);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al actualizar el dataset: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un dataset.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpDelete("{datasetId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteDataset(int datasetId)
        {
            try
            {
                var username = User.Identity?.Name;

                DatasetUM? dataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
                if (dataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }

                DatasetUM? id = await _context.DatasetsUM
                    .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
                await _datasetUMService.DeleteDatasetUMAsync(datasetId, username);
                await _datasetUMService.DeleteDatasetAsync(id!.DatasetId, username);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al eliminar el dataset: {ex.Message}");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDataset")]
        [ProducesResponseType(typeof(List<Datasets>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetUM>>> GetAllDataset()
        {
            try
            {
                var username = User.Identity?.Name;

                List<Datasets> datasets = await _datasetUMService.GetAllDatasetsAsync(username);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets all datasets from all modules in unified format from the general table.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDatasetsDto")]
        [ProducesResponseType(typeof(List<DatasetDto>), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DatasetDto>>> GetAllDatasetsDto([FromQuery] string? search = null)
        {
            try
            {
                var username = User.Identity?.Name;

                var datasetDtos = new List<DatasetDto>();
                var datasetsIM = await _context.Datasets
                    .Include(d => d.DatasetIM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.InsightMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsIM)
                {
                    if (dataset.DatasetIM.Any())
                    {
                        var imDataset = dataset.DatasetIM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = imDataset.Id,
                            Nombre = imDataset.Name,
                            Descripcion = imDataset.Description ?? string.Empty,
                            Module = "Insight Monitor"
                        });
                    }
                }
                var datasetsUM = await _context.Datasets
                    .Include(d => d.DatasetUM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.UrbanMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsUM)
                {
                    if (dataset.DatasetUM.Any())
                    {
                        var umDataset = dataset.DatasetUM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = umDataset.Id,
                            Nombre = umDataset.Name,
                            Descripcion = umDataset.Description ?? string.Empty,
                            Module = "Urban Monitor"
                        });
                    }
                }
                var datasetsAM = await _context.Datasets
                    .Include(d => d.DatasetAM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.AssetManager)
                    .ToListAsync();

                foreach (var dataset in datasetsAM)
                {
                    if (dataset.DatasetAM.Any())
                    {
                        var amDataset = dataset.DatasetAM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = amDataset.Id_Dataset,
                            Nombre = amDataset.Nombre,
                            Descripcion = amDataset.Descripcion ?? string.Empty,
                            Module = "Asset Manager"
                        });
                    }
                }
                var datasetsEM = await _context.Datasets
                    .Include(d => d.DatasetEM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.EventManager)
                    .ToListAsync();

                foreach (var dataset in datasetsEM)
                {
                    if (dataset.DatasetEM.Any())
                    {
                        var emDataset = dataset.DatasetEM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = emDataset.Id,
                            Nombre = emDataset.Name,
                            Descripcion = emDataset.Description ?? string.Empty,
                            Module = "Event Manager"
                        });
                    }
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string normalizedSearch = NormalizeText(search);
                    datasetDtos = datasetDtos.Where(d => NormalizeText(d.Nombre).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Ok(datasetDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Devuelve todos los datasets en formato DatasetDtoGenerico.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllGenericDatasetDtos")]
        [ProducesResponseType(typeof(List<DatasetDtoGenerico>), 200)]
        [ProducesResponseType(500)]
    public async Task<ActionResult<List<DatasetDtoGenerico>>> GetAllGenericDatasetDtos([FromQuery] string? search = null)
        {
            try
            {
                var username = User.Identity?.Name;

                var datasetDtos = new List<DatasetDtoGenerico>();
                var datasetsIM = await _context.Datasets
                    .Include(d => d.DatasetIM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.InsightMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsIM)
                {
                    if (dataset.DatasetIM.Any())
                    {
                        var imDataset = dataset.DatasetIM.First();
                        datasetDtos.Add(new DatasetDtoGenerico
                        {
                            Id = imDataset.Id,
                            IdGenerico = imDataset.DatasetId,
                            Nombre = imDataset.Name,
                            Descripcion = imDataset.Description ?? string.Empty,
                            Module = "Insight Monitor"
                        });
                    }
                }
                var datasetsUM = await _context.Datasets
                    .Include(d => d.DatasetUM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.UrbanMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsUM)
                {
                    if (dataset.DatasetUM.Any())
                    {
                        var umDataset = dataset.DatasetUM.First();
                        datasetDtos.Add(new DatasetDtoGenerico
                        {
                            Id = umDataset.Id,
                            IdGenerico = umDataset.DatasetId,
                            Nombre = umDataset.Name,
                            Descripcion = umDataset.Description ?? string.Empty,
                            Module = "Urban Monitor"
                        });
                    }
                }
                var datasetsAM = await _context.Datasets
                    .Include(d => d.DatasetAM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.AssetManager)
                    .ToListAsync();

                foreach (var dataset in datasetsAM)
                {
                    if (dataset.DatasetAM.Any())
                    {
                        var amDataset = dataset.DatasetAM.First();
                        datasetDtos.Add(new DatasetDtoGenerico
                        {
                            Id = amDataset.Id_Dataset,
                            IdGenerico = amDataset.DatasetId,
                            Nombre = amDataset.Nombre,
                            Descripcion = amDataset.Descripcion ?? string.Empty,
                            Module = "Asset Manager"
                        });
                    }
                }
                var datasetsEM = await _context.Datasets
                    .Include(d => d.DatasetEM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.EventManager)
                    .ToListAsync();

                foreach (var dataset in datasetsEM)
                {
                    if (dataset.DatasetEM.Any())
                    {
                        var emDataset = dataset.DatasetEM.First();
                        datasetDtos.Add(new DatasetDtoGenerico
                        {
                            Id = emDataset.Id,
                            IdGenerico = emDataset.DatasetId,
                            Nombre = emDataset.Name,
                            Descripcion = emDataset.Description ?? string.Empty,
                            Module = "Event Manager"
                        });
                    }
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string normalizedSearch = NormalizeText(search);
                    datasetDtos = datasetDtos.Where(d => NormalizeText(d.Nombre).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Ok(datasetDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }



        /// <summary>
        /// Gets all datasets from all modules with pagination.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("GetAllDatasetsDtoPaginated")]
        [ProducesResponseType(typeof(PaginatedDatasetDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PaginatedDatasetDto>> GetAllDatasetsDtoPaginated(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            try
            {
                var username = User.Identity?.Name;

                var datasetDtos = new List<DatasetDto>();
                var datasetsIM = await _context.Datasets
                    .Include(d => d.DatasetIM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.InsightMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsIM)
                {
                    if (dataset.DatasetIM.Any())
                    {
                        var imDataset = dataset.DatasetIM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = imDataset.Id,
                            Nombre = imDataset.Name,
                            Descripcion = imDataset.Description ?? string.Empty,
                            Module = "Insight Monitor"
                        });
                    }
                }
                var datasetsUM = await _context.Datasets
                    .Include(d => d.DatasetUM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.UrbanMonitor)
                    .ToListAsync();

                foreach (var dataset in datasetsUM)
                {
                    if (dataset.DatasetUM.Any())
                    {
                        var umDataset = dataset.DatasetUM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = umDataset.Id,
                            Nombre = umDataset.Name,
                            Descripcion = umDataset.Description ?? string.Empty,
                            Module = "Urban Monitor"
                        });
                    }
                }
                var datasetsAM = await _context.Datasets
                    .Include(d => d.DatasetAM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.AssetManager)
                    .ToListAsync();

                foreach (var dataset in datasetsAM)
                {
                    if (dataset.DatasetAM.Any())
                    {
                        var amDataset = dataset.DatasetAM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = amDataset.Id_Dataset,
                            Nombre = amDataset.Nombre,
                            Descripcion = amDataset.Descripcion ?? string.Empty,
                            Module = "Asset Manager"
                        });
                    }
                }
                var datasetsEM = await _context.Datasets
                    .Include(d => d.DatasetEM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.EventManager)
                    .ToListAsync();

                foreach (var dataset in datasetsEM)
                {
                    if (dataset.DatasetEM.Any())
                    {
                        var emDataset = dataset.DatasetEM.First();
                        datasetDtos.Add(new DatasetDto
                        {
                            Id = emDataset.Id,
                            Nombre = emDataset.Name,
                            Descripcion = emDataset.Description ?? string.Empty,
                            Module = "Event Manager"
                        });
                    }
                }
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string normalizedSearch = NormalizeText(search);
                    datasetDtos = datasetDtos.Where(d => NormalizeText(d.Nombre).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                int totalCount = datasetDtos.Count;
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;
                var paginatedItems = datasetDtos
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                var result = new PaginatedDatasetDto
                {
                    Items = paginatedItems,
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
                return StatusCode(500, $"Error interno al obtener los datasets: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalizes text for accent-insensitive and case-insensitive searches.
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // 1) Normalizar a FormD y remover diacríticos (acentos)
            string formD = text.Trim().ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD);
            string withoutDiacritics = new string(formD.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

            // 2) Reemplazos adicionales: espacios fuera, ñ->n, subíndices -> dígitos normales
            withoutDiacritics = withoutDiacritics
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("ñ", "n", StringComparison.Ordinal)
                .Replace("₀", "0", StringComparison.Ordinal).Replace("₁", "1", StringComparison.Ordinal).Replace("₂", "2", StringComparison.Ordinal)
                .Replace("₃", "3", StringComparison.Ordinal).Replace("₄", "4", StringComparison.Ordinal).Replace("₅", "5", StringComparison.Ordinal)
                .Replace("₆", "6", StringComparison.Ordinal).Replace("₇", "7", StringComparison.Ordinal).Replace("₈", "8", StringComparison.Ordinal)
                .Replace("₉", "9", StringComparison.Ordinal);

            // 3) Normalizar de vuelta a FormC
            return withoutDiacritics.Normalize(System.Text.NormalizationForm.FormC);
        }

        
    }
}
