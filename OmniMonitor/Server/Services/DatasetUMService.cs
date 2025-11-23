using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Data;
using OmniMonitor.Server.Resources;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetUMService
    {
        Task<DatasetUM> CreateDatasetUMWithFiltersAsync(CreateDatasetUMRequest request, int dataset, List<FilterCondition> filters);
        Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username);
        Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username);
        Task<DatasetUM?> GetDatasetUMByIdAsyncSinToken(int datasetId);
        Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username);
        Task<DatasetUM> UpdateDatasetUMAsync(int datasetId, CreateDatasetUMRequest request);
        Task<DatasetUM> UpdateDatasetUMWithFiltersAsync(int datasetId, CreateDatasetUMRequest request, List<FilterCondition> filters);
        Task DeleteDatasetUMAsync(int datasetId, string username);
        Task<Datasets> CreateDatasetAsync(CreateDatasetRequest request);
        Task<List<Datasets>> GetAllDatasetsAsync(string username);
        Task<Datasets?> GetDatasetByIdForEditAsync(int datasetId, string username);
        Task<Datasets> UpdateDatasetAsyncUM(int datasetId, CreateDatasetRequest request, DatasetUM datasetUM);
        Task<Datasets> UpdateDatasetAsyncIM(int datasetId, CreateDatasetRequest request, DatasetIM datasetIM);
        Task<Datasets> UpdateDatasetAsyncAM(int datasetId, CreateDatasetRequest request, DatasetAM datasetAM);
        Task<Datasets> UpdateDatasetAsyncEM(int datasetId, CreateDatasetRequest request, DatasetEM datasetEM);
        Task DeleteDatasetAsync(int datasetId, string username);
        Task ValidateDatasetNameAsync(string name, string username, ModuleType tipoDataset, int? excludeId = null);


    }

    public class DatasetUMService : IDatasetUMService
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ISondaUMService _sondaUMService;

        #endregion

        #region Constructors

        public DatasetUMService(ApplicationDbContext context, ISondaUMService sondaUMService)
        {
            _context = context;
            _sondaUMService = sondaUMService;
        }

        #endregion

        #region Methods

        public async Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest request, int dataset)
        {
            var newDataset = new DatasetUM
            {
                Username = request.Username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Zone = request.ZoneId,
                DatasetId = dataset,
                ContentType = GetContentType(request)
            };

            UpdateRelationsFromRequest(newDataset, request);

            _context.DatasetsUM.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        public async Task<DatasetUM> CreateDatasetUMWithFiltersAsync(CreateDatasetUMRequest request, int dataset, List<FilterCondition> filters)
        {
            string filtersJson = System.Text.Json.JsonSerializer.Serialize(filters);

            var newDataset = new DatasetUM
            {
                Username = request.Username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Zone = request.ZoneId,
                DatasetId = dataset,
                ContentType = GetContentType(request),
                Filters = filtersJson // Almacenar los filtros como JSON
            };

            UpdateRelationsFromRequest(newDataset, request);

            _context.DatasetsUM.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        public async Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username)
        {
            return await _context.DatasetsUM
                .Where(d => d.Username == username)
                .ToListAsync();
        }

        public async Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
                return null;

            // Lógica de carga dinámica (igual que antes)
            if (dataset.Is_Dataset == "S" && !dataset.DatasetEvents.Any() && !dataset.DatasetNews.Any())
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                    return null;
                List<Event>? eventsFromZone = null;
                List<Event>? eventsFromNews = null;

                if (dataset.Id_Zone.HasValue)
                {
                    var allEvents = await _sondaUMService.GetAllEvents(user.UserName);
                    eventsFromZone = allEvents.Where(e => e.Location != null).ToList();
                }
                if (dataset.DatasetNews != null && dataset.DatasetNews.Any())
                {
                    var newsIds = dataset.DatasetNews.Select(n => n.Id_news).ToList();
                    var allEvents = await _sondaUMService.GetAllEvents(user.UserName);
                    eventsFromNews = allEvents.Where(e => newsIds.Contains(e.Id)).ToList();
                }

                List<Event> finalEventList = new List<Event>();
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
                    finalEventList = await _sondaUMService.GetAllEvents(user.UserName) ?? new List<Event>();
                }
                List<News>? newsFromZone = null;
                List<News>? newsFromEvent = null;

                if (dataset.Id_Zone.HasValue)
                {
                    var newsResponse = await _sondaUMService.GetAllNews(user.UserName, 1, null, null, 1000);
                    newsFromZone = newsResponse.Where(n => n.Zone?.Id == dataset.Id_Zone.Value).ToList();
                }
                if (dataset.DatasetNews != null && dataset.DatasetNews.Any())
                {
                    var newsIds = dataset.DatasetNews.Select(n => n.Id_news).ToList();
                    var newsResponse = await _sondaUMService.GetAllNews(user.UserName, 1, null, null, 1000);
                    newsFromEvent = newsResponse.Where(n => newsIds.Contains(n.Id)).ToList();
                }

                List<News> finalNewsList = new List<News>();
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
                    finalNewsList = await _sondaUMService.GetAllNews(user.UserName, 1, null, null, 1000) ?? new List<News>();
                }

                // 5. Agregar events encontrados al dataset
                if (finalEventList.Any())
                {
                    IEnumerable<Event> filteredEvents = finalEventList;
                    if (dataset.DatasetEvents != null && dataset.DatasetEvents.Any())
                    {
                        var eventIds = dataset.DatasetEvents.Select(ev => ev.Id_event).ToList();
                        filteredEvents = filteredEvents
                            .Where(e => eventIds.Contains(e.Id))
                            .ToList();
                    }
                    var foundEventIds = filteredEvents.Select(e => e.Id).ToList();
                    foreach (var eventId in foundEventIds)
                    {
                        dataset.DatasetEvents.Add(new DatasetEvent { Id_event = eventId });
                    }
                }

                // 6. Agregar news encontrados al dataset
                if (finalNewsList.Any())
                {
                    var foundNewsIds = finalNewsList.Select(n => n.Id).ToList();
                    foreach (var newsId in foundNewsIds)
                    {
                        dataset.DatasetNews.Add(new DatasetNews { Id_news = newsId });
                    }
                }
            }

            return dataset;
        }

        public async Task<DatasetUM?> GetDatasetUMByIdAsyncSinToken(int datasetId)
        {
            var ownerUsername = await _context.DatasetsUM
                .Where(d => d.Id == datasetId)
                .Select(d => d.Username)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(ownerUsername))
                return null;

            return await GetDatasetUMByIdAsync(datasetId, ownerUsername);
        }

        public async Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username)
        {
            return await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
        }

        public async Task<DatasetUM> UpdateDatasetUMAsync(int datasetId, CreateDatasetUMRequest request)
        {
            var existingDataset = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == request.Username);

            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            // La validación de nombres duplicados se hace en la tabla general (UpdateDatasetAsyncUM)

            // Actualizar campos básicos
            existingDataset.Name = request.Name;
            existingDataset.Description = request.Description;
            existingDataset.Is_Dataset = request.IsDataset;
            existingDataset.ContentType = GetContentType(request);
            existingDataset.Id_Zone = request.ZoneId;

            _context.DatasetEvents.RemoveRange(existingDataset.DatasetEvents);
            _context.DatasetNews.RemoveRange(existingDataset.DatasetNews);

            existingDataset.DatasetEvents.Clear();
            existingDataset.DatasetNews.Clear();

            // Agregar nuevas relaciones
            UpdateRelationsFromRequest(existingDataset, request);

            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task<DatasetUM> UpdateDatasetUMWithFiltersAsync(int datasetId, CreateDatasetUMRequest request, List<FilterCondition> filters)
        {
            var existingDataset = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == request.Username);

            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            string filtersJson = System.Text.Json.JsonSerializer.Serialize(filters);

            // Actualizar campos básicos
            existingDataset.Name = request.Name;
            existingDataset.Description = request.Description;
            existingDataset.Is_Dataset = request.IsDataset;
            existingDataset.ContentType = GetContentType(request);
            existingDataset.Id_Zone = request.ZoneId;
            existingDataset.Filters = filtersJson; // Actualizar los filtros

            _context.DatasetEvents.RemoveRange(existingDataset.DatasetEvents);
            _context.DatasetNews.RemoveRange(existingDataset.DatasetNews);

            existingDataset.DatasetEvents.Clear();
            existingDataset.DatasetNews.Clear();

            // Agregar nuevas relaciones
            UpdateRelationsFromRequest(existingDataset, request);

            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task DeleteDatasetUMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, username));

            _context.DatasetsUM.Remove(dataset);
            await _context.SaveChangesAsync();
        }
        private static void UpdateRelationsFromRequest(DatasetUM dataset, CreateDatasetUMRequest request)
        {

            if (request.EventIds?.Any() == true)
            {
            }

            if (request.NewsIds?.Any() == true)
            {
            }

            dataset.DatasetEvents = request.EventIds?.Select(id => new DatasetEvent { Id_event = id }).ToList() ?? new();
            dataset.DatasetNews = request.NewsIds?.Select(id => new DatasetNews { Id_news = id }).ToList() ?? new();

        }

        private static string GetContentType(CreateDatasetUMRequest r)
        {
            // Si ContentType ya está establecido (sistema de filtros), usarlo directamente
            if (!string.IsNullOrEmpty(r.ContentType) && r.ContentType != "0")
            {
                return r.ContentType;
            }

            // Para datasets formales, determinar el tipo según los datos que contiene
            if (r.IsDataset == "S")
            {
                // Si tiene eventos, es tipo Evento
                if (r.EventIds?.Any() == true)
                {
                    return "Evento";
                }
                // Si tiene noticias, es tipo Noticia
                if (r.NewsIds?.Any() == true)
                {
                    return "Noticia";
                }
                // Si solo tiene zona, por defecto es Evento (ya que las zonas generalmente tienen eventos)
                if (r.ZoneId.HasValue)
                {
                    return "Evento";
                }
                // Fallback
                return "0";
            }

            // Para datasets no formales
            if (r.EventIds?.Any() == true) return "Evento";
            if (r.NewsIds?.Any() == true) return "Noticia";
            if (r.ZoneId.HasValue) return "Evento"; // Zona generalmente implica eventos
            return null;
        }





        public async Task<Datasets> CreateDatasetAsync(CreateDatasetRequest request)
        {
            await ValidateDuplicateNameDataset(request.Name, request.Username, request.TipoDataset);

            var newDataset = new Datasets
            {
                Username = request.Username,
                NameDataset = request.Name,
                TipoDataset = request.TipoDataset
            };

            _context.Datasets.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        public async Task<List<Datasets>> GetAllDatasetsAsync(string username)
        {
            return await _context.Datasets
                .Where(d => d.Username == username)
                .ToListAsync();
        }


        public async Task<Datasets?> GetDatasetByIdForEditAsync(int datasetId, string username)
        {
            var query = _context.Datasets.Where(d => d.Id == datasetId && d.Username == username);
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

        public async Task<Datasets> UpdateDatasetAsyncUM(int datasetId, CreateDatasetRequest request, DatasetUM datasetUM)
        {
            var existingDataset = await _context.Datasets
            .Include(d => d.DatasetUM)
            .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            // La validación ya se hizo en el controlador antes de llamar a este método

            // Actualizar campos básicos
            existingDataset.NameDataset = request.Name;
            existingDataset.Username = request.Username;

            existingDataset.DatasetUM.Clear();

            // Agregar nuevas relaciones
            existingDataset.DatasetUM.Add(datasetUM);
            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task<Datasets> UpdateDatasetAsyncIM(int datasetId, CreateDatasetRequest request, DatasetIM datasetIM)
        {
            var existingDataset = await _context.Datasets
            .Include(d => d.DatasetIM)
            .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            // La validación ya se hizo en el controlador antes de llamar a este método

            // Actualizar campos básicos
            existingDataset.NameDataset = request.Name;
            existingDataset.Username = request.Username;

            existingDataset.DatasetIM.Clear();

            // Agregar nuevas relaciones
            existingDataset.DatasetIM.Add(datasetIM);
            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task<Datasets> UpdateDatasetAsyncAM(int datasetId, CreateDatasetRequest request, DatasetAM datasetAM)
        {
            var existingDataset = await _context.Datasets
            .Include(d => d.DatasetAM)
            .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            // La validación ya se hizo en el controlador antes de llamar a este método

            // Actualizar campos básicos
            existingDataset.NameDataset = request.Name;
            existingDataset.Username = request.Username;

            existingDataset.DatasetAM.Clear();

            // Agregar nuevas relaciones
            existingDataset.DatasetAM.Add(datasetAM);
            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task<Datasets> UpdateDatasetAsyncEM(int datasetId, CreateDatasetRequest request, DatasetEM datasetEM)
        {
            var existingDataset = await _context.Datasets
            .Include(d => d.DatasetEM)
            .FirstOrDefaultAsync(d => d.Id == datasetId);
            if (existingDataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, request.Username));

            // La validación ya se hizo en el controlador antes de llamar a este método

            // Actualizar campos básicos
            existingDataset.NameDataset = request.Name;
            existingDataset.Username = request.Username;

            existingDataset.DatasetEM.Clear();

            // Agregar nuevas relaciones
            existingDataset.DatasetEM.Add(datasetEM);
            await _context.SaveChangesAsync();
            return existingDataset;
        }

        public async Task DeleteDatasetAsync(int datasetId, string username)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
                throw new InvalidOperationException(string.Format(Language.DatasetNotFound, datasetId, username));

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync();
        }

        public async Task ValidateDatasetNameAsync(string name, string username, ModuleType tipoDataset, int? excludeId = null)
        {
            // Validar que no exista otro dataset con el mismo nombre en CUALQUIER módulo para el mismo usuario
            var query = _context.Datasets
                .Where(d => d.NameDataset == name && d.Username == username);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            if (await query.AnyAsync())
                throw new InvalidOperationException(string.Format(Language.DatasetNameExists, name, username));
        }
        private async Task ValidateDuplicateNameDataset(string name, string username, ModuleType tipoDataset, int? excludeId = null)
        {
            // Validar que no exista otro dataset con el mismo nombre en CUALQUIER módulo para el mismo usuario
            var query = _context.Datasets
                .Where(d => d.NameDataset == name && d.Username == username);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            if (await query.AnyAsync())
                throw new InvalidOperationException(string.Format(Language.DatasetNameExists, name, username));
        }
        #endregion
    }
}
