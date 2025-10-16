using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetEMService
    {
        Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request);
        Task<List<DatasetEM>> GetAllDatasetsEMAsync(string username);
        Task<DatasetEM?> GetDatasetEMByIdAsync(int datasetId, string username);
        Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username);
        Task<DatasetEM> UpdateDatasetEMAsync(DatasetEM dataset);
        Task DeleteDatasetEMAsync(int datasetId, string username);
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
        public async Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request)
        {
            // Validar que el nombre sea único para el usuario
            var existingDataset = await _context.DatasetsEM
                .FirstOrDefaultAsync(d => d.Name == request.Name && d.Username == request.Username);

            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Name}' para el usuario '{request.Username}'.");
            }

            var newDataset = new DatasetEM
            {
                Name = request.Name,
                Description = request.Description,
                Username = request.Username,
                Is_Dataset = request.IsDataset,
                Id_Alert = request.AlertId,
                Id_Event = request.EventId,
                Id_Extension = request.ExtensionId,
                Id_Resource = request.ResourceId,
                AlertState = request.AlertState,
                EventState = request.EventState,
                ExtensionState = request.ExtensionState,
                ResourceState = request.ResourceState
            };

            if (request.IsDataset == "S")
            {
               newDataset.ContentType = "0"; // 0 para indicar un dataset formal
            }
            else // Si IsDataset es 'N'
            {
                if (request.AlertIds != null && request.AlertIds.Any())
                {
                    newDataset.ContentType = "1"; // 1 para indicar alerts
                }
                else if (request.EventIds != null && request.EventIds.Any())
                {
                    newDataset.ContentType = "2"; // 2 para indicar events
                }
                else if (request.ExtensionIds != null && request.ExtensionIds.Any())
                {
                    newDataset.ContentType = "3"; // 3 para indicar extensions
                }
                else if (request.ResourceIds != null && request.ResourceIds.Any())
                {
                    newDataset.ContentType = "4"; // 4 para indicar resources
                }
            }

            // Si el usuario seleccionó alerts específicos, los agregamos.
            if (request.AlertIds != null && request.AlertIds.Any())
            {
                foreach (var alertId in request.AlertIds)
                {
                    newDataset.DatasetAlerts.Add(new DatasetAlert { Id_alert = alertId });
                }
            }

            // Si el usuario seleccionó events específicos, los agregamos.
            if (request.EventIds != null && request.EventIds.Any())
            {
                foreach (var eventId in request.EventIds)
                {
                    newDataset.DatasetEvents.Add(new DatasetEventEM { Id_event = eventId });
                }
            }

            // Si el usuario seleccionó extensions específicas, las agregamos.
            if (request.ExtensionIds != null && request.ExtensionIds.Any())
            {
                foreach (var extensionId in request.ExtensionIds)
                {
                    newDataset.DatasetExtensions.Add(new DatasetExtension { Id_extension = extensionId });
                }
            }

            // Si el usuario seleccionó resources específicos, los agregamos.
            if (request.ResourceIds != null && request.ResourceIds.Any())
            {
                foreach (var resourceId in request.ResourceIds)
                {
                    newDataset.DatasetResources.Add(new DatasetResource { Id_resource = resourceId });
                }
            }

            _context.DatasetsEM.Add(newDataset);
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
                .Include(d => d.DatasetResources)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                return null;
            }

            // Si es un dataset formal ('S') y no se seleccionaron entidades, las buscamos dinámicamente.
            if (dataset.Is_Dataset == "S" && !dataset.DatasetAlerts.Any() && !dataset.DatasetEvents.Any() && 
                !dataset.DatasetExtensions.Any() && !dataset.DatasetResources.Any())
            {
                // Para llamar a la API externa, necesitamos las credenciales del usuario.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    // No se puede proceder si el usuario no existe en la base de datos local.
                    return null;
                }

                // --- LÓGICA MODIFICADA: Búsqueda dinámica optimizada para EM ---
                
                // 1. Buscar Alerts dinámicamente
                if (dataset.Id_Alert.HasValue || !string.IsNullOrEmpty(dataset.AlertState))
                {
                    var alerts = await _sondaEMService.GetAlerts(1, 1000, null, dataset.AlertState, null, null, null, null, null, user.Username, user.Password);
                    
                    if (dataset.Id_Alert.HasValue)
                    {
                        alerts = alerts.Where(a => a.AlertId == dataset.Id_Alert.Value).ToList();
                    }

                    foreach (var alert in alerts)
                    {
                        dataset.DatasetAlerts.Add(new DatasetAlert { Id_alert = alert.AlertId });
                    }
                }

                // 2. Buscar Events dinámicamente
                if (dataset.Id_Event.HasValue || !string.IsNullOrEmpty(dataset.EventState))
                {
                    var events = await _sondaEMService.GetEvents(1, 1000, null, null, user.Username, user.Password);
                    
                    if (dataset.Id_Event.HasValue)
                    {
                        events = events.Where(e => e.Id == dataset.Id_Event.Value).ToList();
                    }

                    if (!string.IsNullOrEmpty(dataset.EventState))
                    {
                        events = events.Where(e => e.State == dataset.EventState).ToList();
                    }

                    foreach (var eventItem in events)
                    {
                        dataset.DatasetEvents.Add(new DatasetEventEM { Id_event = eventItem.Id });
                    }
                }

                // 3. Buscar Extensions dinámicamente
                if (dataset.Id_Extension.HasValue || !string.IsNullOrEmpty(dataset.ExtensionState))
                {
                    var extensions = await _sondaEMService.GetExtensions(1, 1000, null, null, dataset.ExtensionState, null, null, null, null, user.Username, user.Password);
                    
                    if (dataset.Id_Extension.HasValue)
                    {
                        extensions = extensions.Where(e => e.ExtensionId == dataset.Id_Extension.Value).ToList();
                    }

                    foreach (var extension in extensions)
                    {
                        dataset.DatasetExtensions.Add(new DatasetExtension { Id_extension = extension.ExtensionId });
                    }
                }

                // 4. Buscar Resources dinámicamente
                if (dataset.Id_Resource.HasValue || !string.IsNullOrEmpty(dataset.ResourceState))
                {
                    // Nota: No hay método GetAllResources en ISondaEMService, solo GetResourceById
                    // Por ahora, solo buscamos por ID específico
                    if (dataset.Id_Resource.HasValue)
                    {
                        var resource = await _sondaEMService.GetResourceById(dataset.Id_Resource.Value, user.Username, user.Password);
                        if (resource != null)
                        {
                            dataset.DatasetResources.Add(new DatasetResource { Id_resource = resource.Id });
                        }
                    }
                }
            }

            return dataset;
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario para edición (sin carga dinámica).
        /// </summary>
        public async Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsEM
                .Include(d => d.DatasetAlerts)
                .Include(d => d.DatasetEvents)
                .Include(d => d.DatasetExtensions)
                .Include(d => d.DatasetResources)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            return dataset;
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        public async Task<DatasetEM> UpdateDatasetEMAsync(DatasetEM dataset)
        {
            // Validar que el nombre sea único para el usuario (excluyendo el dataset actual)
            var existingDataset = await _context.DatasetsEM
                .FirstOrDefaultAsync(d => d.Name == dataset.Name && d.Username == dataset.Username && d.Id != dataset.Id);

            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{dataset.Name}' para el usuario '{dataset.Username}'.");
            }

            _context.DatasetsEM.Update(dataset);
            await _context.SaveChangesAsync();

            return dataset;
        }

        /// <summary>
        /// Elimina un dataset.
        /// </summary>
        public async Task DeleteDatasetEMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsEM
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            _context.DatasetsEM.Remove(dataset);
            await _context.SaveChangesAsync();
        }
    }
}
