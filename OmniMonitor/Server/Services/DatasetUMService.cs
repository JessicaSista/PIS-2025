using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
// Se asume que existe un servicio y DTOs para interactuar con la API de Sonda
using OmniMonitor.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    // --- Interfaz para el servicio ---
    public interface IDatasetUMService
    {
        Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest request);
        Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username);
        Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username);
        Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username);
        Task<DatasetUM> UpdateDatasetUMAsync(DatasetUM dataset);
        Task DeleteDatasetUMAsync(int datasetId, string username);
    }

    // --- Implementación del servicio ---
    public class DatasetUMService : IDatasetUMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaUMService _sondaUMService;

        public DatasetUMService(ApplicationDbContext context, ISondaUMService sondaUMService)
        {
            _context = context;
            _sondaUMService = sondaUMService;
        }

        /// <summary>
        /// Crea un nuevo dataset, ya sea uno formal ('S') o uno interno para un solo elemento ('N').
        /// </summary>
        public async Task<DatasetUM> CreateDatasetUMAsync(CreateDatasetUMRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            // Validar que no exista otro dataset con el mismo nombre para el mismo usuario
            var existingDataset = await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.Username == request.Username && d.Name == request.Name);
            
            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Name}' para el usuario '{request.Username}'.");
            }

            var newDataset = new DatasetUM
            {
                Username = request.Username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Zone = request.ZoneId,
                Id_News = request.NewsId,
                EventName = request.EventName
            };

            if (request.IsDataset == "S")
            {
               newDataset.ContentType = "0"; // 0 para indicar un dataset formal
            }
            else // Si IsDataset es 'N'
            {
                if (request.EventIds != null && request.EventIds.Any())
                {
                    newDataset.ContentType = "1"; // 1 para indicar un event
                }
                else if (request.NewsIds != null && request.NewsIds.Any())
                {
                    newDataset.ContentType = "1"; // 1 para indicar un news
                }
                else if (request.NewsId.HasValue)
                {
                    newDataset.ContentType = "2"; // 2 para indicar un news por filtro
                }
                else if (!string.IsNullOrEmpty(request.EventName))
                {
                    newDataset.ContentType = "3"; // 3 para indicar un zone
                }
            }

            // Si el usuario seleccionó events específicos, los agregamos.
            if (request.EventIds != null && request.EventIds.Any())
            {
                foreach (var eventId in request.EventIds)
                {
                    newDataset.DatasetEvents.Add(new DatasetEvent { Id_event = eventId });
                }
            }

            // Si el usuario seleccionó news específicos, los agregamos.
            if (request.NewsIds != null && request.NewsIds.Any())
            {
                foreach (var newsId in request.NewsIds)
                {
                    newDataset.DatasetNews.Add(new DatasetNews { Id_news = newsId });
                }
            }

            _context.DatasetsUM.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        /// <summary>
        /// Obtiene todos los datasets de un usuario específico.
        /// </summary>
        public async Task<List<DatasetUM>> GetAllDatasetsUMAsync(string username)
        {
            return await _context.DatasetsUM
                .Where(d => d.Username == username)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario, aplicando la lógica de carga de news.
        /// </summary>
        public async Task<DatasetUM?> GetDatasetUMByIdAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                return null;
            }

            // Si es un dataset formal ('S') y no se seleccionaron entidades, las buscamos dinámicamente.
            if (dataset.Is_Dataset == "S" && !dataset.DatasetEvents.Any() && !dataset.DatasetNews.Any())
            {
                // Para llamar a la API externa, necesitamos las credenciales del usuario.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    // No se puede proceder si el usuario no existe en la base de datos local.
                    return null;
                }

                // --- LÓGICA MODIFICADA: Búsqueda dinámica optimizada para Events y News ---
                
                // 1. Buscar Events dinámicamente
                List<Event>? eventsFromZone = null;
                List<Event>? eventsFromNews = null;

                if (dataset.Id_Zone.HasValue)
                {
                    var allEvents = await _sondaUMService.GetAllEvents(user.Username, user.Password);
                    eventsFromZone = allEvents.Where(e => e.Location != null).ToList(); // Filtrar por zona si es necesario
                }
                if (dataset.Id_News.HasValue)
                {
                    var allEvents = await _sondaUMService.GetAllEvents(user.Username, user.Password);
                    eventsFromNews = allEvents.Where(e => e.Id == dataset.Id_News.Value).ToList();
                }

                // 2. Determinar la lista final de events
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

                // 4. Determinar la lista final de news
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

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario para edición (sin carga dinámica).
        /// </summary>
        public async Task<DatasetUM?> GetDatasetUMByIdForEditAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsUM
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetNews)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            return dataset;
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        public async Task<DatasetUM> UpdateDatasetUMAsync(DatasetUM dataset)
        {
            // Validar que no exista otro dataset con el mismo nombre para el mismo usuario
            var existingDataset = await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.Username == dataset.Username && d.Name == dataset.Name && d.Id != dataset.Id);
            
            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{dataset.Name}' para el usuario '{dataset.Username}'.");
            }

            // Actualizar el dataset
            _context.DatasetsUM.Update(dataset);
            await _context.SaveChangesAsync();

            return dataset;
        }

        /// <summary>
        /// Elimina un dataset.
        /// </summary>
        public async Task DeleteDatasetUMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsUM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            _context.DatasetsUM.Remove(dataset);
            await _context.SaveChangesAsync();
        }
    }
}