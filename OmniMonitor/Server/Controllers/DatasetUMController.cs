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
        private readonly ISondaUMService _sondaUMService;

        public DatasetUMController(IDatasetUMService datasetUMService, ISondaAuthService sondaAuthService, ApplicationDbContext context, ISondaUMService sondaUMService)
        {
            _context = context;
            _datasetUMService = datasetUMService;
            _sondaAuthService = sondaAuthService;
            _sondaUMService = sondaUMService;
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

        public class CreateDatasetUMFilteredRequest
        {
            public CreateDatasetUMRequest DatasetRequest { get; set; } = new CreateDatasetUMRequest();
            public string Token { get; set; } = string.Empty;
            public List<FilterCondition> Filters { get; set; } = new List<FilterCondition>();
        }

        [HttpPost("filtered")]
        [ProducesResponseType(typeof(DatasetUM), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetUM>> CreateDatasetFiltered([FromBody] CreateDatasetUMFilteredRequest request)
        {
            try
            {
                string username = await _sondaAuthService.GetUserByTokenOMAsync(request.Token);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var req = request.DatasetRequest;
                var requestDataset = new CreateDatasetRequest(req.Name, req.Username, ModuleType.UrbanMonitor);
                Datasets dataset = await _datasetUMService.CreateDatasetAsync(requestDataset);

                List<int> filteredIds = new List<int>();
                if (req.ContentType == "2") // News
                {
                    var allNews = await _sondaUMService.GetAllNews(username, 1, null, null, 1000);
                    Console.WriteLine($"[DEBUG] Total news antes de filtrar: {allNews.Count}");
                    var filtrados = ApiDataService.StaticFilterObjects(allNews, request.Filters);
                    Console.WriteLine($"[DEBUG] Total news filtrados: {filtrados.Count}");
                    foreach (var news in filtrados)
                    {
                        Console.WriteLine($"[DEBUG] News filtrado: Id={news.Id}, Title={news.Title}");
                    }
                    filteredIds = filtrados.Select(n => (int)n.Id).ToList();
                    req.NewsIds = filteredIds;
                    Console.WriteLine($"[DEBUG] IDs agregados a request.NewsIds: {string.Join(",", req.NewsIds)}");
                }
                else if (req.ContentType == "1") // Eventos
                {
                    IEnumerable<object> eventos = (await _sondaUMService.GetAllEvents(username)).Cast<object>();
                    Console.WriteLine($"[DEBUG] Total eventos antes de filtrar: {eventos.Count()}");
                    var filtrados = ApiDataService.StaticFilterObjects(eventos, request.Filters);
                    Console.WriteLine($"[DEBUG] Total eventos filtrados: {filtrados.Count}");
                    foreach (var ev in filtrados)
                    {
                        Console.WriteLine($"[DEBUG] Evento filtrado: Id={ev.Id}, Name={ev.Name}");
                    }
                    filteredIds = filtrados.Select(e => (int)e.Id).ToList();
                    req.EventIds = filteredIds;
                    Console.WriteLine($"[DEBUG] IDs agregados a request.EventIds: {string.Join(",", req.EventIds)}");
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado");
                }

                DatasetUM newDataset = await _datasetUMService.CreateDatasetUMWithFiltersAsync(req, dataset.Id, request.Filters);
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
        /// Obtiene todos los datasets para un usuario específico.
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
        /// Obtiene un dataset específico por su ID y nombre de usuario.
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

        [HttpPut("with-filters/{datasetId}")]
        [ProducesResponseType(typeof(DatasetUM), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DatasetUM>> UpdateDatasetWithFilters(int datasetId, [FromBody] CreateDatasetUMFilteredRequest request)
        {
            try
            {
                Console.WriteLine($"[DEBUG] Iniciando UpdateDatasetWithFilters para datasetId: {datasetId}");
                
                if (!ModelState.IsValid)
                {
                    Console.WriteLine($"[DEBUG] ModelState inválido");
                    return BadRequest(ModelState);
                }

                var req = request.DatasetRequest;
                //string username = await _sondaAuthService.GetUserByTokenOMAsync(request.Token);
                string username = "admin";
                Console.WriteLine($"[DEBUG] Username usado: {username}, req.Username: {req.Username}");
                
                // Usar el username consistente
                req.Username = username;
                
                // Obtener el dataset existente
                DatasetUM? existingDataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, username);
                if (existingDataset == null)
                {
                    Console.WriteLine($"[DEBUG] Dataset no encontrado para ID {datasetId} y usuario {username}");
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
                }

                Console.WriteLine($"[DEBUG] Dataset encontrado: {existingDataset.Name}");
                Console.WriteLine($"[DEBUG] Número de filtros recibidos: {request.Filters.Count}");

                // Validar nombre único
                await _datasetUMService.ValidateDatasetNameAsync(req.Name, username, ModuleType.UrbanMonitor, existingDataset.DatasetId);

                var requestDataset = new CreateDatasetRequest(req.Name, username, ModuleType.UrbanMonitor);

                // Aplicar filtros - SIEMPRE procesar datos (sin filtros = todos los datos)
                List<int> filteredIds = new List<int>();
                Console.WriteLine($"[DEBUG] ContentType: {req.ContentType}");
                Console.WriteLine($"[DEBUG] Filtros disponibles: {request.Filters.Any()}");
                
                // SIEMPRE procesar según ContentType
                Console.WriteLine($"[DEBUG] Procesando datos - ContentType: {req.ContentType}");
                
                if (req.ContentType == "2") // News
                {
                    var allNews = await _sondaUMService.GetAllNews(username, 1, null, null, 1000);
                    Console.WriteLine($"[DEBUG] Total news antes de filtrar: {allNews.Count}");
                    
                    if (request.Filters.Any())
                    {
                        var filtrados = ApiDataService.StaticFilterObjects(allNews, request.Filters);
                        Console.WriteLine($"[DEBUG] Total news filtrados: {filtrados.Count}");
                        filteredIds = filtrados.Select(n => (int)n.Id).ToList();
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Sin filtros - tomando todas las news");
                        filteredIds = allNews.Select(n => (int)n.Id).ToList();
                    }
                    
                    req.NewsIds = filteredIds;
                    Console.WriteLine($"[DEBUG] IDs agregados a request.NewsIds: {string.Join(",", req.NewsIds)}");
                }
                else if (req.ContentType == "1") // Eventos
                {
                    var eventos = await _sondaUMService.GetAllEvents(username);
                    Console.WriteLine($"[DEBUG] Total eventos antes de filtrar: {eventos.Count}");
                    
                    if (request.Filters.Any())
                    {
                        IEnumerable<object> eventosObj = eventos.Cast<object>();
                        var filtrados = ApiDataService.StaticFilterObjects(eventosObj, request.Filters);
                        Console.WriteLine($"[DEBUG] Total eventos filtrados: {filtrados.Count}");
                        filteredIds = filtrados.Select(e => (int)e.Id).ToList();
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Sin filtros - tomando todos los eventos");
                        filteredIds = eventos.Select(e => e.Id).ToList();
                    }
                    
                    req.EventIds = filteredIds;
                    Console.WriteLine($"[DEBUG] IDs agregados a request.EventIds: {string.Join(",", req.EventIds)}");
                }
                else
                {
                    return BadRequest("ContentType inválido o no soportado para filtros");
                }

                // Actualizar dataset con filtros
                Console.WriteLine($"[DEBUG] Actualizando dataset con filtros...");
                DatasetUM updatedDataset = await _datasetUMService.UpdateDatasetUMWithFiltersAsync(datasetId, req, request.Filters);
                await _datasetUMService.UpdateDatasetAsyncUM(updatedDataset.DatasetId, requestDataset, updatedDataset);
                
                Console.WriteLine($"[DEBUG] Dataset actualizado exitosamente: {updatedDataset.Name}");
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
                return StatusCode(500, $"Error interno al actualizar el dataset con filtros: {ex.Message}");
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

                // Obtener el dataset existente para obtener el DatasetId
                DatasetUM? existingDataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, request.Username);
                if (existingDataset == null)
                {
                    return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");
                }

                // Primero validar el nombre en la tabla general antes de actualizar cualquier tabla
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
            /// Edita un dataset existente aplicando filtrado, similar a la creación filtrada.
            /// </summary>
            [HttpPut("EditarUMFiltrado/{datasetId}")]
            [ProducesResponseType(typeof(DatasetUM), 200)]
            [ProducesResponseType(400)]
            [ProducesResponseType(403)]
            [ProducesResponseType(404)]
            [ProducesResponseType(500)]
            public async Task<ActionResult<DatasetUM>> EditarDatasetUMFiltrado(int datasetId, [FromBody] CreateDatasetUMFilteredRequest request)
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        return BadRequest(ModelState);
                    }

                    var req = request.DatasetRequest;
                    string username = await _sondaAuthService.GetUserByTokenOMAsync(request.Token);
                    DatasetUM? existingDataset = await _datasetUMService.GetDatasetUMByIdForEditAsync(datasetId, req.Username);
                    if (existingDataset == null)
                    {
                        return NotFound($"No se encontró el dataset con ID {datasetId} para el usuario {req.Username}.");
                    }

                    await _datasetUMService.ValidateDatasetNameAsync(req.Name, req.Username, ModuleType.UrbanMonitor, existingDataset.DatasetId);

                    var requestDataset = new CreateDatasetRequest(req.Name, req.Username, ModuleType.UrbanMonitor);

                    List<int> filteredIds = new List<int>();
                    if (req.ContentType == "2") // News
                    {
                        var allNews = await _sondaUMService.GetAllNews(username, 1, null, null, 1000);
                        Console.WriteLine($"[DEBUG] Total news antes de filtrar: {allNews.Count}");
                        var filtrados = ApiDataService.StaticFilterObjects(allNews, request.Filters);
                        Console.WriteLine($"[DEBUG] Total news filtrados: {filtrados.Count}");
                        foreach (var news in filtrados)
                        {
                            Console.WriteLine($"[DEBUG] News filtrado: Id={news.Id}, Title={news.Title}");
                        }
                        filteredIds = filtrados.Select(n => (int)n.Id).ToList();
                        req.NewsIds = filteredIds;
                        Console.WriteLine($"[DEBUG] IDs agregados a request.NewsIds: {string.Join(",", req.NewsIds)}");
                    }
                    else if (req.ContentType == "1") // Eventos
                    {
                        IEnumerable<object> eventos = (await _sondaUMService.GetAllEvents(username)).Cast<object>();
                        Console.WriteLine($"[DEBUG] Total eventos antes de filtrar: {eventos.Count()}");
                        var filtrados = ApiDataService.StaticFilterObjects(eventos, request.Filters);
                        Console.WriteLine($"[DEBUG] Total eventos filtrados: {filtrados.Count}");
                        foreach (var ev in filtrados)
                        {
                            Console.WriteLine($"[DEBUG] Evento filtrado: Id={ev.Id}, Name={ev.Name}");
                        }
                        filteredIds = filtrados.Select(e => (int)e.Id).ToList();
                        req.EventIds = filteredIds;
                        Console.WriteLine($"[DEBUG] IDs agregados a request.EventIds: {string.Join(",", req.EventIds)}");
                    }
                    else
                    {
                        return BadRequest("ContentType inválido o no soportado");
                    }

                    DatasetUM updatedDataset = await _datasetUMService.UpdateDatasetUMAsync(datasetId, req);
                    await _datasetUMService.UpdateDatasetAsyncUM(updatedDataset.DatasetId, requestDataset, updatedDataset);
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
                    return StatusCode(500, $"Error interno al editar el dataset: {ex.Message}");
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
        /// Obtiene todos los datasets de todos los módulos en formato unificado desde la tabla general.
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

                // Obtener datasets IM
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

                // Obtener datasets UM
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

                // Obtener datasets AM
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

                // Obtener datasets EM
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

                // Aplicar filtro de búsqueda si existe
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

                // Obtener datasets IM
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

                // Obtener datasets UM
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

                // Obtener datasets AM
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

                // Obtener datasets EM
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

                // Aplicar filtro de búsqueda si existe
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
        /// Obtiene todos los datasets de todos los módulos con paginación.
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

                // Validar parámetros de entrada
                if (page < 1)
                {
                    return BadRequest("El número de página debe ser mayor a 0.");
                }

                if (pageSize < 1 || pageSize > 100) // Límite máximo para prevenir sobrecarga
                {
                    return BadRequest("El tamaño de página debe estar entre 1 y 100.");
                }

                // Construir una consulta unificada más eficiente
                var normalizedSearch = !string.IsNullOrWhiteSpace(search) ? NormalizeText(search) : null;

                // Query para datasets IM
                var imQuery = _context.Datasets
                    .Include(d => d.DatasetIM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.InsightMonitor && d.DatasetIM.Any())
                    .Select(d => new DatasetDto
                    {
                        Id = d.DatasetIM.First().Id,
                        Nombre = d.DatasetIM.First().Name,
                        Descripcion = d.DatasetIM.First().Description ?? string.Empty,
                        Module = "Insight Monitor"
                    });

                // Query para datasets UM
                var umQuery = _context.Datasets
                    .Include(d => d.DatasetUM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.UrbanMonitor && d.DatasetUM.Any())
                    .Select(d => new DatasetDto
                    {
                        Id = d.DatasetUM.First().Id,
                        Nombre = d.DatasetUM.First().Name,
                        Descripcion = d.DatasetUM.First().Description ?? string.Empty,
                        Module = "Urban Monitor"
                    });

                // Query para datasets AM
                var amQuery = _context.Datasets
                    .Include(d => d.DatasetAM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.AssetManager && d.DatasetAM.Any())
                    .Select(d => new DatasetDto
                    {
                        Id = d.DatasetAM.First().Id_Dataset,
                        Nombre = d.DatasetAM.First().Nombre,
                        Descripcion = d.DatasetAM.First().Descripcion ?? string.Empty,
                        Module = "Asset Manager"
                    });

                // Query para datasets EM
                var emQuery = _context.Datasets
                    .Include(d => d.DatasetEM)
                    .Where(d => d.Username == username && d.TipoDataset == ModuleType.EventManager && d.DatasetEM.Any())
                    .Select(d => new DatasetDto
                    {
                        Id = d.DatasetEM.First().Id,
                        Nombre = d.DatasetEM.First().Name,
                        Descripcion = d.DatasetEM.First().Description ?? string.Empty,
                        Module = "Event Manager"
                    });

                // Combinar todas las consultas
                var combinedQuery = imQuery
                    .Concat(umQuery)
                    .Concat(amQuery)
                    .Concat(emQuery);

                // Aplicar filtro de búsqueda si existe a nivel de SQL
                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    combinedQuery = combinedQuery.Where(d => EF.Functions.Like(d.Nombre.ToLower(), $"%{normalizedSearch.ToLower()}%"));
                }

                // Obtener el total de registros antes de aplicar paginación
                int totalCount = await combinedQuery.CountAsync();

                // Calcular páginas totales
                int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                // Validar que la página solicitada no exceda las páginas disponibles
                // Si hay datos y la página es mayor al total, usar la última página
                if (page > totalPages && totalPages > 0) 
                {
                    page = totalPages;
                }

                // Aplicar paginación directamente en SQL y ejecutar la consulta
                var paginatedItems = await combinedQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Crear respuesta paginada
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
        /// Normaliza el texto para búsquedas insensibles a acentos y mayúsculas.
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