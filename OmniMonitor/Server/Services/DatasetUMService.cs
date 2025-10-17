using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetUMService
    {
        Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest request);
        Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username);
        Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username);
        Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username);
        Task<DatasetUM> UpdateDatasetUMAsync(int datasetId, CreateDatasetUMRequest request);
        Task DeleteDatasetUMAsync(int datasetId, string username);
    }

    public class DatasetUMService : IDatasetUMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaUMService _sondaUMService;

        public DatasetUMService(ApplicationDbContext context, ISondaUMService sondaUMService)
        {
            _context = context;
            _sondaUMService = sondaUMService;
        }

        public async Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest request)
        {
            await ValidateDuplicateName(request.Name, request.Username);

            var newDataset = new DatasetUM
            {
                Username = request.Username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Zone = request.ZoneId,
                Id_News = request.NewsId,
                EventName = request.EventName,
                ContentType = GetContentType(request)
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
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return null;

                // 1. Buscar Events dinámicamente
                List<Event>? eventsFromZone = null;
                List<Event>? eventsFromNews = null;

                if (dataset.Id_Zone.HasValue)
                {
                    var allEvents = await _sondaUMService.GetAllEvents(user.Username, user.Password);
                    eventsFromZone = allEvents.Where(e => e.Location != null).ToList();
                }
                if (dataset.Id_News.HasValue)
                {
                    var allEvents = await _sondaUMService.GetAllEvents(user.Username, user.Password);
                    eventsFromNews = allEvents.Where(e => e.Id == dataset.Id_News.Value).ToList();
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
                    finalEventList = await _sondaUMService.GetAllEvents(user.Username, user.Password) ?? new List<Event>();
                }

                // 3. Buscar News dinámicamente
                List<News>? newsFromZone = null;
                List<News>? newsFromEvent = null;

                if (dataset.Id_Zone.HasValue)
                {
                    var newsResponse = await _sondaUMService.GetAllNews(user.Username, user.Password, 1, null, null, 1000);
                    newsFromZone = newsResponse.Where(n => n.Zone?.Id == dataset.Id_Zone.Value).ToList();
                }
                if (dataset.Id_News.HasValue)
                {
                    var newsResponse = await _sondaUMService.GetAllNews(user.Username, user.Password, 1, null, null, 1000);
                    newsFromEvent = newsResponse.Where(n => n.Id == dataset.Id_News.Value).ToList();
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
                    finalNewsList = await _sondaUMService.GetAllNews(user.Username, user.Password, 1, null, null, 1000) ?? new List<News>();
                }

                // 5. Agregar events encontrados al dataset
                if (finalEventList.Any())
                {
                    IEnumerable<Event> filteredEvents = finalEventList;
                    if (!string.IsNullOrEmpty(dataset.EventName))
                    {
                        string eventNameToFind = dataset.EventName;
                        filteredEvents = filteredEvents.Where(e => e.Name.Contains(eventNameToFind));
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
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");

            await ValidateDuplicateName(request.Name, request.Username, datasetId);

            // Actualizar campos básicos
            existingDataset.Name = request.Name;
            existingDataset.Description = request.Description;
            existingDataset.Is_Dataset = request.IsDataset;
            existingDataset.ContentType = GetContentType(request);
            existingDataset.Id_Zone = request.ZoneId;
            existingDataset.Id_News = request.NewsId;
            existingDataset.EventName = request.EventName;

            // Eliminar relaciones existentes (si no tienes Cascade Delete, si lo tienes puedes solo limpiar)
            _context.DatasetEvents.RemoveRange(existingDataset.DatasetEvents);
            _context.DatasetNews.RemoveRange(existingDataset.DatasetNews);

            // Limpiar colecciones
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
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");

            _context.DatasetsUM.Remove(dataset);
            await _context.SaveChangesAsync();
        }

        // --- Helpers ---

        private static void UpdateRelationsFromRequest(DatasetUM dataset, CreateDatasetUMRequest request)
        {
            dataset.DatasetEvents = request.EventIds?.Select(id => new DatasetEvent { Id_event = id }).ToList() ?? new();
            dataset.DatasetNews = request.NewsIds?.Select(id => new DatasetNews { Id_news = id }).ToList() ?? new();
        }

        private static string GetContentType(CreateDatasetUMRequest r)
        {
            if (r.IsDataset == "S") return "0";
            if (r.EventIds?.Any() == true) return "1";
            if (r.NewsIds?.Any() == true) return "2";
            if (r.ZoneId.HasValue) return "3";
            return null;
        }

        private async Task ValidateDuplicateName(string name, string username, int? excludeId = null)
        {
            var query = _context.DatasetsUM
                .Where(d => d.Name == name && d.Username == username);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            if (await query.AnyAsync())
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{name}' para el usuario '{username}'.");
        }
    }
}