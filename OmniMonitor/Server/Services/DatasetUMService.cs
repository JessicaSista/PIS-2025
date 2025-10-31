using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Data;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de datasets Urban Monitor.
    /// </summary>
    public interface IDatasetUMService
    {
        /// <summary>
        /// Crea un nuevo dataset UM.
        /// </summary>
        /// <param name="createDatasetUmRequest">Datos para la creación del dataset.</param>
        /// <param name="datasetId">ID del dataset general asociado.</param>
        /// <returns>El dataset UM creado.</returns>
        Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest createDatasetUmRequest, int datasetId);

        /// <summary>
        /// Obtiene todos los datasets UM de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de datasets UM.</returns>
        Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username);

        /// <summary>
        /// Obtiene un dataset UM por su ID y usuario.
        /// </summary>
        /// <param name="datasetId">ID del dataset UM.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset UM encontrado o null.</returns>
        Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username);

        /// <summary>
        /// Obtiene un dataset UM para edición.
        /// </summary>
        /// <param name="datasetId">ID del dataset UM.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset UM encontrado o null.</returns>
        Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username);

        /// <summary>
        /// Actualiza un dataset UM existente.
        /// </summary>
        /// <param name="datasetId">ID del dataset UM.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <returns>El dataset UM actualizado.</returns>
        Task<DatasetUM> UpdateDatasetUMAsync(int datasetId, CreateDatasetUMRequest updateRequest);

        /// <summary>
        /// Elimina un dataset UM.
        /// </summary>
        /// <param name="datasetId">ID del dataset UM.</param>
        /// <param name="username">Nombre de usuario.</param>
        Task DeleteDatasetUMAsync(int datasetId, string username);

        /// <summary>
        /// Crea un nuevo dataset general.
        /// </summary>
        /// <param name="createDatasetRequest">Datos para la creación del dataset.</param>
        /// <returns>El dataset creado.</returns>
        Task<Datasets> CreateDatasetAsync(CreateDatasetRequest createDatasetRequest);

        /// <summary>
        /// Obtiene todos los datasets generales de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de datasets generales.</returns>
        Task<List<Datasets>> GetAllDatasetsAsync(string username);

        /// <summary>
        /// Obtiene un dataset general para edición.
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset encontrado o null.</returns>
        Task<Datasets?> GetDatasetByIdForEditAsync(int datasetId, string username);

        /// <summary>
        /// Actualiza un dataset general (UM).
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <param name="datasetUm">Entidad DatasetUM asociada.</param>
        /// <returns>El dataset actualizado.</returns>
        Task<Datasets> UpdateDatasetAsyncUM(int datasetId, CreateDatasetRequest updateRequest, DatasetUM datasetUm);

        /// <summary>
        /// Actualiza un dataset general (IM).
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <param name="datasetIm">Entidad DatasetIM asociada.</param>
        /// <returns>El dataset actualizado.</returns>
        Task<Datasets> UpdateDatasetAsyncIM(int datasetId, CreateDatasetRequest updateRequest, DatasetIM datasetIm);

        /// <summary>
        /// Actualiza un dataset general (AM).
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <param name="datasetAm">Entidad DatasetAM asociada.</param>
        /// <returns>El dataset actualizado.</returns>
        Task<Datasets> UpdateDatasetAsyncAM(int datasetId, CreateDatasetRequest updateRequest, DatasetAM datasetAm);

        /// <summary>
        /// Actualiza un dataset general (EM).
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <param name="datasetEm">Entidad DatasetEM asociada.</param>
        /// <returns>El dataset actualizado.</returns>
        Task<Datasets> UpdateDatasetAsyncEM(int datasetId, CreateDatasetRequest updateRequest, DatasetEM datasetEm);

        /// <summary>
        /// Elimina un dataset general.
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        Task DeleteDatasetAsync(int datasetId, string username);
    }

    public class DatasetUMService : IDatasetUMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaUMService _sondaUmService;
        private readonly ILogger<DatasetUMService> _logger;

        /// <summary>
        /// Constructor del servicio DatasetUMService.
        /// </summary>
        public DatasetUMService(ApplicationDbContext context, ISondaUMService sondaUmService, ILogger<DatasetUMService> logger)
        {
            _context = context;
            _sondaUmService = sondaUmService;
            _logger = logger;
        }

        #region Métodos DatasetUM

        /// <inheritdoc/>
        public async Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest createDatasetUmRequest, int datasetId)
        {
            if (string.IsNullOrEmpty(createDatasetUmRequest.Username) || string.IsNullOrEmpty(createDatasetUmRequest.Name))
            {
                _logger.LogWarning("El nombre de usuario o el nombre del dataset es nulo o vacío.");
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            var newDatasetUm = new DatasetUM
            {
                Username = createDatasetUmRequest.Username,
                Name = createDatasetUmRequest.Name,
                Description = createDatasetUmRequest.Description,
                Is_Dataset = createDatasetUmRequest.IsDataset,
                Id_Zone = createDatasetUmRequest.ZoneId,
                DatasetId = datasetId,
                ContentType = GetContentType(createDatasetUmRequest)
            };

            UpdateRelationsFromRequest(newDatasetUm, createDatasetUmRequest);

            _context.DatasetsUM.Add(newDatasetUm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetUM creado correctamente para el usuario {Username} con nombre {Name}.", createDatasetUmRequest.Username, createDatasetUmRequest.Name);

            return newDatasetUm;
        }

        /// <inheritdoc/>
        public async Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("El nombre de usuario es nulo o vacío al intentar obtener todos los datasets UM.");
                return new();
            }

            var result = await _context.DatasetsUM
                .AsNoTracking()
                .Where(d => string.Equals(d.Username, username))
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} datasets UM para el usuario {Username}.", result.Count, username);

            return result;
        }

        /// <inheritdoc/>
        public async Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username)
        {
            var datasetUm = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));

            if (datasetUm == null)
            {
                _logger.LogWarning("No se encontró el dataset UM con ID {DatasetId} para el usuario {Username}.", datasetId, username);
                return null;
            }

            if (string.Equals(datasetUm.Is_Dataset, "S") && !datasetUm.DatasetEvents.Any() && !datasetUm.DatasetNews.Any())
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => string.Equals(u.UserName, username));
                if (user == null)
                {
                    _logger.LogWarning("No se encontró el usuario {Username} en la base de datos.", username);
                    return null;
                }

                List<Event>? eventsFromZone = null;
                List<Event>? eventsFromNews = null;

                if (datasetUm.Id_Zone.HasValue)
                {
                    var allEvents = await _sondaUmService.GetAllEvents(user.UserName);
                    if (allEvents != null)
                    {
                        eventsFromZone = allEvents.Where(e => e.Location != null).ToList();
                    }
                }
                if (datasetUm.DatasetNews != null && datasetUm.DatasetNews.Any())
                {
                    var newsIds = datasetUm.DatasetNews.Select(n => n.Id_news).ToList();
                    var allEvents = await _sondaUmService.GetAllEvents(user.UserName);
                    if (allEvents != null)
                    {
                        eventsFromNews = allEvents.Where(e => newsIds.Contains(e.Id)).ToList();
                    }
                }

                List<Event> finalEventList = new();
                if (eventsFromZone != null && eventsFromNews != null)
                {
                    var eventIdsFromNews = new HashSet<int>(eventsFromNews.Select(e => e.Id));
                    finalEventList = eventsFromZone.Where(e => eventIdsFromNews.Contains(e.Id)).ToList();
                }
                else if (eventsFromZone != null)
                {
                    finalEventList = eventsFromZone;
                }
                else if (eventsFromNews != null)
                {
                    finalEventList = eventsFromNews;
                }
                else
                {
                    finalEventList = await _sondaUmService.GetAllEvents(user.UserName) ?? new();
                }

                List<News>? newsFromZone = null;
                List<News>? newsFromEvent = null;

                if (datasetUm.Id_Zone.HasValue)
                {
                    var newsResponse = await _sondaUmService.GetAllNews(user.UserName, 1, null, null, 1000);
                    if (newsResponse != null)
                    {
                        newsFromZone = newsResponse.Where(n => n.Zone?.Id == datasetUm.Id_Zone.Value).ToList();
                    }
                }
                if (datasetUm.DatasetNews != null && datasetUm.DatasetNews.Any())
                {
                    var newsIds = datasetUm.DatasetNews.Select(n => n.Id_news).ToList();
                    var newsResponse = await _sondaUmService.GetAllNews(user.UserName, 1, null, null, 1000);
                    if (newsResponse != null)
                    {
                        newsFromEvent = newsResponse.Where(n => newsIds.Contains(n.Id)).ToList();
                    }
                }

                List<News> finalNewsList = new();
                if (newsFromZone != null && newsFromEvent != null)
                {
                    var newsIdsFromEvent = new HashSet<int>(newsFromEvent.Select(n => n.Id));
                    finalNewsList = newsFromZone.Where(n => newsIdsFromEvent.Contains(n.Id)).ToList();
                }
                else if (newsFromZone != null)
                {
                    finalNewsList = newsFromZone;
                }
                else if (newsFromEvent != null)
                {
                    finalNewsList = newsFromEvent;
                }
                else
                {
                    finalNewsList = await _sondaUmService.GetAllNews(user.UserName, 1, null, null, 1000) ?? new();
                }

                if (finalEventList.Any())
                {
                    IEnumerable<Event> filteredEvents = finalEventList;
                    if (datasetUm.DatasetEvents != null && datasetUm.DatasetEvents.Any())
                    {
                        var eventIds = datasetUm.DatasetEvents.Select(ev => ev.Id_event).ToList();
                        filteredEvents = filteredEvents
                            .Where(e => eventIds.Contains(e.Id))
                            .ToList();
                    }
                    var foundEventIds = filteredEvents.Select(e => e.Id).ToList();
                    foreach (var eventId in foundEventIds)
                    {
                        datasetUm.DatasetEvents.Add(new() { Id_event = eventId });
                    }
                }

                if (finalNewsList.Any())
                {
                    var foundNewsIds = finalNewsList.Select(n => n.Id).ToList();
                    foreach (var newsId in foundNewsIds)
                    {
                        datasetUm.DatasetNews.Add(new() { Id_news = newsId });
                    }
                }
            }

            _logger.LogInformation("Se obtuvo el dataset UM con ID {DatasetId} para el usuario {Username}.", datasetId, username);

            return datasetUm;
        }

        /// <inheritdoc/>
        public async Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username)
        {
            return await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));
        }

        /// <inheritdoc/>
        public async Task<DatasetUM> UpdateDatasetUMAsync(int datasetId, CreateDatasetUMRequest updateRequest)
        {
            var existingDatasetUm = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, updateRequest.Username));

            if (existingDatasetUm == null)
            {
                _logger.LogWarning("No se encontró el dataset UM con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {updateRequest.Username}.");
            }

            existingDatasetUm.Name = updateRequest.Name;
            existingDatasetUm.Description = updateRequest.Description;
            existingDatasetUm.Is_Dataset = updateRequest.IsDataset;
            existingDatasetUm.ContentType = GetContentType(updateRequest);
            existingDatasetUm.Id_Zone = updateRequest.ZoneId;

            _context.DatasetEvents.RemoveRange(existingDatasetUm.DatasetEvents);
            _context.DatasetNews.RemoveRange(existingDatasetUm.DatasetNews);

            existingDatasetUm.DatasetEvents.Clear();
            existingDatasetUm.DatasetNews.Clear();

            UpdateRelationsFromRequest(existingDatasetUm, updateRequest);

            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetUM actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);

            return existingDatasetUm;
        }

        /// <inheritdoc/>
        public async Task DeleteDatasetUMAsync(int datasetId, string username)
        {
            var datasetUm = await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));

            if (datasetUm == null)
            {
                _logger.LogWarning("No se encontró el dataset UM con ID {DatasetId} para el usuario {Username}.", datasetId, username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            _context.DatasetsUM.Remove(datasetUm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetUM eliminado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, username);
        }

        #endregion

        #region Métodos Datasets (Generales)

        /// <inheritdoc/>
        public async Task<Datasets> CreateDatasetAsync(CreateDatasetRequest createDatasetRequest)
        {
            await ValidateDuplicateNameDataset(createDatasetRequest.Name, createDatasetRequest.Username);

            var newDataset = new Datasets
            {
                Username = createDatasetRequest.Username,
                NameDataset = createDatasetRequest.Name,
                TipoDataset = createDatasetRequest.TipoDataset
            };

            _context.Datasets.Add(newDataset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general creado correctamente para el usuario {Username} con nombre {Name}.", createDatasetRequest.Username, createDatasetRequest.Name);

            return newDataset;
        }

        /// <inheritdoc/>
        public async Task<List<Datasets>> GetAllDatasetsAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("El nombre de usuario es nulo o vacío al intentar obtener todos los datasets generales.");
                return new();
            }

            var result = await _context.Datasets
                .AsNoTracking()
                .Where(d => string.Equals(d.Username, username))
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} datasets generales para el usuario {Username}.", result.Count, username);

            return result;
        }

        /// <inheritdoc/>
        public async Task<Datasets?> GetDatasetByIdForEditAsync(int datasetId, string username)
        {
            var query = _context.Datasets.Where(d => d.Id == datasetId && string.Equals(d.Username, username));
            if (query == null)
            {
                return null;
            }
            var tipoModulo = await query.Select(d => d.TipoDataset).FirstOrDefaultAsync();

            if (tipoModulo == ModuleType.UrbanMonitor)
            {
                query = query.Include(d => d.DatasetUM);
            }
            else if (tipoModulo == ModuleType.EventManager)
            {
                query = query.Include(d => d.DatasetEM);
            }
            else if (tipoModulo == ModuleType.AssetManager)
            {
                query = query.Include(d => d.DatasetAM);
            }
            else if (tipoModulo == ModuleType.InsightMonitor)
            {
                query = query.Include(d => d.DatasetIM);
            }

            var dataset = await query.FirstOrDefaultAsync();
            return dataset;
        }

        /// <inheritdoc/>
        public async Task<Datasets> UpdateDatasetAsyncUM(int datasetId, CreateDatasetRequest updateRequest, DatasetUM datasetUm)
        {
            var existingDataset = await _context.Datasets
                .Include(d => d.DatasetUM)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
            {
                _logger.LogWarning("No se encontró el dataset general con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {updateRequest.Username}.");
            }

            await ValidateDuplicateNameDataset(updateRequest.Name, updateRequest.Username, datasetId);

            existingDataset.NameDataset = updateRequest.Name;
            existingDataset.Username = updateRequest.Username;

            existingDataset.DatasetUM.Clear();
            existingDataset.DatasetUM.Add(datasetUm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general (UM) actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);

            return existingDataset;
        }

        /// <inheritdoc/>
        public async Task<Datasets> UpdateDatasetAsyncIM(int datasetId, CreateDatasetRequest updateRequest, DatasetIM datasetIm)
        {
            var existingDataset = await _context.Datasets
                .Include(d => d.DatasetIM)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
            {
                _logger.LogWarning("No se encontró el dataset general con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {updateRequest.Username}.");
            }

            await ValidateDuplicateNameDataset(updateRequest.Name, updateRequest.Username, datasetId);

            existingDataset.NameDataset = updateRequest.Name;
            existingDataset.Username = updateRequest.Username;

            existingDataset.DatasetIM.Clear();
            existingDataset.DatasetIM.Add(datasetIm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general (IM) actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);

            return existingDataset;
        }

        /// <inheritdoc/>
        public async Task<Datasets> UpdateDatasetAsyncAM(int datasetId, CreateDatasetRequest updateRequest, DatasetAM datasetAm)
        {
            var existingDataset = await _context.Datasets
                .Include(d => d.DatasetAM)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
            {
                _logger.LogWarning("No se encontró el dataset general con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {updateRequest.Username}.");
            }

            await ValidateDuplicateNameDataset(updateRequest.Name, updateRequest.Username, datasetId);

            existingDataset.NameDataset = updateRequest.Name;
            existingDataset.Username = updateRequest.Username;

            existingDataset.DatasetAM.Clear();
            existingDataset.DatasetAM.Add(datasetAm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general (AM) actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);

            return existingDataset;
        }

        /// <inheritdoc/>
        public async Task<Datasets> UpdateDatasetAsyncEM(int datasetId, CreateDatasetRequest updateRequest, DatasetEM datasetEm)
        {
            var existingDataset = await _context.Datasets
                .Include(d => d.DatasetEM)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
            {
                _logger.LogWarning("No se encontró el dataset general con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {updateRequest.Username}.");
            }

            await ValidateDuplicateNameDataset(updateRequest.Name, updateRequest.Username, datasetId);

            existingDataset.NameDataset = updateRequest.Name;
            existingDataset.Username = updateRequest.Username;

            existingDataset.DatasetEM.Clear();
            existingDataset.DatasetEM.Add(datasetEm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general (EM) actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, updateRequest.Username);

            return existingDataset;
        }

        /// <inheritdoc/>
        public async Task DeleteDatasetAsync(int datasetId, string username)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));

            if (dataset == null)
            {
                _logger.LogWarning("No se encontró el dataset general con ID {DatasetId} para el usuario {Username}.", datasetId, username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset general eliminado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, username);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Actualiza las relaciones de eventos y noticias en el dataset UM a partir de la request.
        /// </summary>
        private static void UpdateRelationsFromRequest(DatasetUM datasetUm, CreateDatasetUMRequest request)
        {
            datasetUm.DatasetEvents = request.EventIds?.Select(id => new DatasetEvent { Id_event = id }).ToList() ?? new();
            datasetUm.DatasetNews = request.NewsIds?.Select(id => new DatasetNews { Id_news = id }).ToList() ?? new();
        }

        /// <summary>
        /// Determina el tipo de contenido del dataset UM.
        /// </summary>
        private static string? GetContentType(CreateDatasetUMRequest request)
        {
            if (string.Equals(request.IsDataset, "S"))
            {
                return "0";
            }
            if (request.EventIds?.Any() == true)
            {
                return "1";
            }
            if (request.NewsIds?.Any() == true)
            {
                return "2";
            }
            if (request.ZoneId.HasValue)
            {
                return "3";
            }
            return null;
        }

        /// <summary>
        /// Valida que no exista un dataset con el mismo nombre para el usuario.
        /// </summary>
        private async Task ValidateDuplicateNameDataset(string name, string username, int? excludeId = null)
        {
            var query = _context.Datasets
                .Where(d => string.Equals(d.NameDataset, name) && string.Equals(d.Username, username));

            if (excludeId.HasValue)
            {
                query = query.Where(d => d.Id != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                _logger.LogWarning("Intento de duplicar nombre de dataset '{Name}' para el usuario '{Username}'.", name, username);
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{name}' para el usuario '{username}'.");
            }
        }

        #endregion
    }
}