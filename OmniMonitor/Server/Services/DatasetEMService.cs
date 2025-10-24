using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetEMService
    {
        Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request,int dataset);
        Task<List<DatasetEM>> GetAllDatasetsEMAsync(string username);
        Task<DatasetEM?> GetDatasetEMByIdAsync(int datasetId, string username);
        Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username);
        Task DeleteDatasetEMAsync(int datasetId, string username);
        Task<DatasetEM> UpdateDatasetEMAsync(int datasetId, CreateDatasetEMRequest request);
    }

    public class DatasetEMService : IDatasetEMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaEMService _sondaEMService;

        public DatasetEMService(ApplicationDbContext context, ISondaEMService sondaEMService)
        {
            _context = context;
            _sondaEMService = sondaEMService;
        }

        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        public async Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request, int dataset)
        {
            await ValidateDuplicateName(request.Name, request.Username);

            var newDataset = new DatasetEM
            {
                Name = request.Name,
                Description = request.Description,
                Username = request.Username,
                Is_Dataset = request.IsDataset,
                DatasetId = dataset,
                ContentType = GetContentType(request).ToString()
            };

            // Save dataset first to generate the ID
            _context.DatasetsEM.Add(newDataset);
            await _context.SaveChangesAsync();

            // Now update relations with the generated dataset.Id
            UpdateRelationsFromRequest(newDataset, request);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        /// <summary>
        /// Obtiene todos los datasets EM de un usuario.
        /// </summary>
        public async Task<List<DatasetEM>> GetAllDatasetsEMAsync(string username)
        {
            return await _context.DatasetsEM
                .Where(d => d.Username == username)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario, aplicando la lógica de carga dinámica.
        /// </summary>
        public async Task<DatasetEM?> GetDatasetEMByIdAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetExtensions)
                 .Include(d => d.DatasetCategory)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
                return null;

            // Lógica de carga dinámica (sin cambios aquí)
            if (dataset.Is_Dataset == "S" &&
                !dataset.DatasetAlerts.Any() &&
                !dataset.DatasetEvents.Any() &&
                !dataset.DatasetExtensions.Any() &&
                 !dataset.DatasetCategory.Any())
            {
                // Obtener usuario y credenciales
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return null;

               /* // 1. Alerts dinámicos
                if (dataset.Id_Alert.HasValue || !string.IsNullOrEmpty(dataset.AlertState))
                {
                    var alerts = await _sondaEMService.GetAlerts(1, 1000, null, dataset.AlertState, null, null, null, null, null, user.Username, user.Password);
                    if (dataset.Id_Alert.HasValue)
                        alerts = alerts.Where(a => a.AlertId == dataset.Id_Alert.Value).ToList();

                    foreach (var alert in alerts)
                        dataset.DatasetAlerts.Add(new DatasetAlert { Id_alert = alert.AlertId });
                }

                // 2. Events dinámicos
                if (dataset.Id_Event.HasValue || !string.IsNullOrEmpty(dataset.EventState))
                {
                    var events = await _sondaEMService.GetEvents(1, 1000, null, null, user.Username, user.Password);
                    if (dataset.Id_Event.HasValue)
                        events = events.Where(e => e.Id == dataset.Id_Event.Value).ToList();
                    if (!string.IsNullOrEmpty(dataset.EventState))
                        events = events.Where(e => e.State == dataset.EventState).ToList();

                    foreach (var eventItem in events)
                        dataset.DatasetEvents.Add(new DatasetEventEM { Id_event = eventItem.Id });
                }

                // 3. Extensions dinámicas
                if (dataset.Id_Extension.HasValue || !string.IsNullOrEmpty(dataset.ExtensionState))
                {
                    var extensions = await _sondaEMService.GetExtensions(1, 1000, null, null, dataset.ExtensionState, null, null, null, null, user.Username, user.Password);
                    if (dataset.Id_Extension.HasValue)
                        extensions = extensions.Where(e => e.ExtensionId == dataset.Id_Extension.Value).ToList();

                    foreach (var extension in extensions)
                        dataset.DatasetExtensions.Add(new DatasetExtension { Id_extension = extension.ExtensionId });
                }
                // 4. Category dinámicas
                if (dataset.Id_Category.HasValue || !string.IsNullOrEmpty(dataset.CategoryState))
                {
                    var categories = await _sondaEMService.GetCategory(1, 1000, null,null, user.Username, user.Password);
                    if (dataset.Id_Category.HasValue)
                        categories = categories.Where(c => c.CategoryId == dataset.Id_Category.Value).ToList();
                    foreach (var category in categories)
                        dataset.DatasetCategory.Add(new DatasetCategory { Id_Category = category.CategoryId });
                }*/
            }

            return dataset;
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario para edición (sin carga dinámica).
        /// </summary>
        public async Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username)
        {
            return await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetExtensions)
                .Include(d => d.DatasetCategory)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        public async Task<DatasetEM> UpdateDatasetEMAsync(int datasetId, CreateDatasetEMRequest request)
        {
            var existingDataset = await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetExtensions)
                .Include(d => d.DatasetCategory)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == request.Username);

            if (existingDataset == null)
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {request.Username}.");

            await ValidateDuplicateName(request.Name, request.Username, datasetId);

            // Actualizar campos básicos
            existingDataset.Name = request.Name;
            existingDataset.Description = request.Description;
            existingDataset.Is_Dataset = request.IsDataset;
            existingDataset.ContentType = GetContentType(request).ToString();
            /*existingDataset.Id_Alert = request.AlertId;
            existingDataset.Id_Event = request.EventId;
            existingDataset.Id_Extension = request.ExtensionId;
            existingDataset.Id_Category = request.CategoryId;
            existingDataset.AlertState = request.AlertState;
            existingDataset.EventState = request.EventState;
            existingDataset.ExtensionState = request.ExtensionState;
            existingDataset.CategoryState = request.CategoryState;
            
            // Marcar campos nullable como modificados si es necesario
            _context.Entry(existingDataset).Property(d => d.Id_Alert).IsModified = true;
            _context.Entry(existingDataset).Property(d => d.Id_Event).IsModified = true;
            _context.Entry(existingDataset).Property(d => d.Id_Extension).IsModified = true;
            _context.Entry(existingDataset).Property(d => d.Id_Category).IsModified = true;
            */// Eliminar relaciones existentes de la base de datos
            _context.DatasetAlerts.RemoveRange(existingDataset.DatasetAlerts);
            _context.DatasetEventsEM.RemoveRange(existingDataset.DatasetEvents);
            _context.DatasetExtensions.RemoveRange(existingDataset.DatasetExtensions);
            _context.DatasetCategory.RemoveRange(existingDataset.DatasetCategory);

            // Limpiar colecciones
            existingDataset.DatasetAlerts.Clear();
            existingDataset.DatasetEvents.Clear();
            existingDataset.DatasetExtensions.Clear();
            existingDataset.DatasetCategory.Clear();
            // Agregar nuevas relaciones
            UpdateRelationsFromRequest(existingDataset, request);

            await _context.SaveChangesAsync();
            return existingDataset;
        }

        /// <summary>
        /// Elimina un dataset.
        /// </summary>
        public async Task DeleteDatasetEMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsEM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");

            _context.DatasetsEM.Remove(dataset);
            await _context.SaveChangesAsync();
        }

        // --- Helpers ---

        private static void UpdateRelationsFromRequest(DatasetEM dataset, CreateDatasetEMRequest request)
        {
            if (request.AlertIds?.Any() == true)
            {
                dataset.DatasetAlerts = request.AlertIds.Select(id => new DatasetAlert 
                { 
                    DatasetId = dataset.Id, 
                    Id_alert = id 
                }).ToList();
            }
            
            if (request.EventIds?.Any() == true)
            {
                dataset.DatasetEvents = request.EventIds.Select(id => new DatasetEventEM 
                { 
                    DatasetId = dataset.Id, 
                    Id_event = id 
                }).ToList();
            }
            
            if (request.ExtensionIds?.Any() == true)
            {
                dataset.DatasetExtensions = request.ExtensionIds.Select(id => new DatasetExtension 
                { 
                    DatasetId = dataset.Id, 
                    Id_extension = id 
                }).ToList();
            }
            
            if (request.CategoryIds?.Any() == true)
            {
                dataset.DatasetCategory = request.CategoryIds.Select(id => new DatasetCategory 
                { 
                    DatasetId = dataset.Id, 
                    Id_Category = id 
                }).ToList();
            }
        }

        private static DatasetContentType GetContentType(CreateDatasetEMRequest r)
        {
            DatasetContentType type = DatasetContentType.None;
            if (r.AlertIds?.Any() == true) type |= DatasetContentType.Alerts;
            if (r.EventIds?.Any() == true) type |= DatasetContentType.Events;
            if (r.ExtensionIds?.Any() == true) type |= DatasetContentType.Extensions;
            if (r.CategoryIds?.Any() == true) type |= DatasetContentType.Category;
            return type;
        }

        private async Task ValidateDuplicateName(string name, string username, int? excludeId = null)
        {
            var query = _context.DatasetsEM
                .Where(d => d.Name == name && d.Username == username);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            if (await query.AnyAsync())
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{name}' para el usuario '{username}'.");
        }
    }

    [Flags]
    public enum DatasetContentType
    {
        None = 0,
        Alerts = 1,
        Events = 2,
        Extensions = 4,
        Category = 8
    }
}
