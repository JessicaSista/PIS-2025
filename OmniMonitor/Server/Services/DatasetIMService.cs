using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetService
    {
        Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest request, int dataset, string username);
        Task<DatasetIM> CreateDatasetIMFilteredAsync(CreateDatasetIMRequest request, int dataset, string username);
        Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username);
        Task<DatasetIM?> GetDatasetIMByIdAsync(int datasetId, string username);
        Task<DatasetIM?> GetDatasetIMByIdForEditAsync(int datasetId, string username);
        Task<DatasetIM?> GetDatasetIMByIdForEditAsyncSinToken(int datasetId);
        Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM dataset, CreateDatasetIMRequest request, string username);
        Task DeleteDatasetIMAsync(int datasetId, string username);
        Task<string?> IdentifyDatasetModuleAsync(int datasetId, string username);
    }

    public class DatasetIMService : IDatasetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaIMService _sondaIMService;

        public DatasetIMService(ApplicationDbContext context, ISondaIMService sondaIMService)
        {
            _context = context;
            _sondaIMService = sondaIMService;
        }

        /// <summary>
        /// Crea un nuevo dataset, ya sea uno formal ('S') o uno interno para un solo elemento ('N').
        /// </summary>
        public async Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest request, int dataset, string username)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            // Validar que no exista otro dataset con el mismo nombre para el mismo usuario
            var existingDataset = await _context.DatasetsIM
                .FirstOrDefaultAsync(d => d.Username == username && d.Name == request.Name);
            
            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Name}' para el usuario '{username}'.");
            }

            var newDataset = new DatasetIM
            {
                Username = username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Source = request.SourceId,
                Id_Group = request.GroupId,
                SensorName = request.SensorName,
                DatasetId= dataset
            };

            if (request.IsDataset == "S")
            {
               newDataset.ContentType = "0"; // 0 para indicar un dataset formal
            }
            else // Si IsDataset es 'N'
            {
                if (request.DeviceIds != null && request.DeviceIds.Any())
                {
                    newDataset.ContentType = "1"; // 1 para indicar un device
                }
                else if (request.SourceId.HasValue)
                {
                    newDataset.ContentType = "2"; // 2 para indicar una source
                }
                else if (!string.IsNullOrEmpty(request.SensorName))
                {
                    newDataset.ContentType = "3"; // 3 para indicar un sensor
                }
            }

            // Si el usuario seleccionó devices específicos, los agregamos.
            if (request.DeviceIds != null && request.DeviceIds.Any())
            {
                foreach (var deviceId in request.DeviceIds)
                {
                    newDataset.DatasetDevices.Add(new DatasetDevice { Id_device = deviceId });
                }
            }

            _context.DatasetsIM.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        /// <summary>
        /// Crea un nuevo dataset no formal aplicando filtros JSON y persistiendo los elementos filtrados.
        /// </summary>
        public async Task<DatasetIM> CreateDatasetIMFilteredAsync(CreateDatasetIMRequest request, int dataset, string username)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            if (request.IsDataset == "S")
            {
                throw new ArgumentException("Este método es solo para datasets no formales (IsDataset = 'N').");
            }

            if (request.Filters == null || !request.Filters.Any())
            {
                throw new ArgumentException("Los filtros son obligatorios para datasets filtrados.");
            }

            // Validar que no exista otro dataset con el mismo nombre para el mismo usuario
            var existingDataset = await _context.DatasetsIM
                .FirstOrDefaultAsync(d => d.Username == username && d.Name == request.Name);
            
            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Name}' para el usuario '{username}'.");
            }

            var newDataset = new DatasetIM
            {
                Username = username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = "N", // Siempre no formal
                JsonFilters = request.JsonFilters, // Ya viene serializado desde el controller
                ContentType = request.ContentType,
                DatasetId = dataset
            };

            // Usar directamente los filtros de la request
            var filters = request.Filters;

            // Traer todos los elementos, filtrar y persistir según el ContentType
            switch (request.ContentType)
            {
                case "1": // Device
                    await ProcessAndPersistDevices(newDataset, filters, username);
                    break;
                case "2": // Source
                    await ProcessAndPersistSources(newDataset, filters, username);
                    break;
                case "3": // Sensor
                    await ProcessAndPersistSensors(newDataset, filters, username);
                    break;
                default:
                    throw new ArgumentException("ContentType no válido para datasets filtrados.");
            }

            _context.DatasetsIM.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        private async Task ProcessAndPersistDevices(DatasetIM dataset, List<FilterCondition> filters, string username)
        {
            // 1. Traer todos los devices
            var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
            
            // 2. Aplicar filtros
            var filteredDevices = ApiDataService.StaticFilterObjects(allDevices, filters);
            
            // 3. Persistir devices filtrados
            foreach (var deviceObj in filteredDevices)
            {
                if (deviceObj is Device device)
                {
                    dataset.DatasetDevices.Add(new DatasetDevice { Id_device = device.Id });
                }
            }
        }

        private async Task ProcessAndPersistSources(DatasetIM dataset, List<FilterCondition> filters, string username)
        {
            // 1. Traer todas las sources
            var allSources = await _sondaIMService.GetAllSources(username) ?? new List<Source>();
            
            // 2. Aplicar filtros
            var filteredSources = ApiDataService.StaticFilterObjects(allSources, filters);
            
            // 3. Persistir sources filtradas
            foreach (var sourceObj in filteredSources)
            {
                if (sourceObj is Source source)
                {
                    dataset.DatasetSources.Add(new DatasetSource { Id_source = source.Id });
                }
            }
        }

        private async Task ProcessAndPersistSensors(DatasetIM dataset, List<FilterCondition> filters, string username)
        {
            // 1. Traer todos los devices y extraer sensores únicos
            var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
            var allSensors = allDevices
                .Where(d => d.Sensors != null)
                .SelectMany(d => d.Sensors!)
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .ToList();
            
            // 2. Aplicar filtros
            var filteredSensors = ApiDataService.StaticFilterObjects(allSensors, filters);
            
            // 3. Persistir sensors filtrados
            foreach (var sensorObj in filteredSensors)
            {
                if (sensorObj is Sensor sensor && !string.IsNullOrEmpty(sensor.Name))
                {
                    dataset.DatasetSensors.Add(new DatasetSensor 
                    { 
                        SensorName = sensor.Name,
                    });
                }
            }
        }

        /// <summary>
        /// Obtiene todos los datasets de un usuario específico.
        /// </summary>
        public async Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username)
        {
            return await _context.DatasetsIM
                .Where(d => d.Username == username)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario, aplicando la lógica de carga de devices.
        /// </summary>
        public async Task<DatasetIM?> GetDatasetIMByIdAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .Include(d => d.DatasetSources)
                .Include(d => d.DatasetSensors)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                return null;
            }

            // Si es un dataset formal ('S') y no se seleccionaron devices, los buscamos dinámicamente.
            if (dataset.Is_Dataset == "S" && !dataset.DatasetDevices.Any())
            {
                // Para llamar a la API externa, necesitamos las credenciales del usuario.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    // No se puede proceder si el usuario no existe en la base de datos local.
                    return null;
                }

                List<Device>? devicesFromSource = null;
                List<Device>? devicesFromGroup = null;
                if (dataset.Id_Source.HasValue)
                {
                    devicesFromSource = await _sondaIMService.GetDeviceOfSource(dataset.Id_Source.Value, user.UserName);
                }
                if (dataset.Id_Group.HasValue)
                {
                    devicesFromGroup = await _sondaIMService.GetDeviceOfGroup(dataset.Id_Group.Value, user.UserName);
                }

                // 2. Determinar la lista final de dispositivos a partir de las listas obtenidas.
                List<Device> finalDeviceList = new List<Device>();

                if (devicesFromSource != null && devicesFromGroup != null)
                {
                    // Caso AND: Intersección de ambas listas. Se necesitan los devices que estén en ambas.
                    var deviceIdsFromGroup = new HashSet<int>(devicesFromGroup.Select(d => d.Id));
                    finalDeviceList = devicesFromSource.Where(d => deviceIdsFromGroup.Contains(d.Id)).ToList();
                }
                else if (devicesFromSource != null)
                {
                    // Solo se filtró por source.
                    finalDeviceList = devicesFromSource;
                }
                else if (devicesFromGroup != null)
                {
                    // Solo se filtró por grupo.
                    finalDeviceList = devicesFromGroup;
                }
                else
                {
                    // Fallback: si no hay ni source ni grupo, obtener todos.
                    finalDeviceList = await _sondaIMService.GetAllDevices(user.UserName) ?? new List<Device>();
                }

                if (finalDeviceList.Any())
                {
                    IEnumerable<Device> filteredDevices = finalDeviceList;

                    /*if (!string.IsNullOrEmpty(dataset.SensorName))
                    {
                        string sensorNameToFind = dataset.SensorName;
                        filteredDevices = filteredDevices.Where(d => d.Sensors != null && d.Sensors.Any(s => s.Name == sensorNameToFind));
                    }*/

                    // 3. Agregar los IDs de los devices encontrados al dataset.
                    var foundDeviceIds = filteredDevices.Select(d => d.Id).ToList();
                    foreach (var deviceId in foundDeviceIds)
                    {
                        dataset.DatasetDevices.Add(new DatasetDevice { Id_device = deviceId });
                    }
                }
            }

            // Si es un dataset interno ('N') o uno formal con devices ya seleccionados,
            // simplemente lo devolvemos tal como está.
            return dataset;
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario para edición, SIN aplicar lógica de búsqueda dinámica.
        /// Devuelve el dataset exactamente como está guardado en la base de datos.
        /// </summary>
        public async Task<DatasetIM?> GetDatasetIMByIdForEditAsync(int datasetId, string username)
        {
            return await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .Include(d => d.DatasetSources)
                .Include(d => d.DatasetSensors)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
        }

        public async Task<DatasetIM?> GetDatasetIMByIdForEditAsyncSinToken(int datasetId)
        {
            return await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .Include(d => d.DatasetSources)
                .Include(d => d.DatasetSensors)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        public async Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM dataset, CreateDatasetIMRequest request, string username)
        {
            if (dataset == null)
            {
                throw new ArgumentNullException(nameof(dataset), "El dataset no puede ser nulo.");
            }
            if (dataset == null)
            {
                throw new InvalidOperationException($"No se encontró el dataset con ID {dataset.Id}.");
            }

            // La validación de nombres duplicados se hace en la tabla general (UpdateDatasetAsyncIM)

            // Actualizar campos
            dataset.Name = request.Name;
            dataset.Description = request.Description;
            dataset.Id_Source = request.SourceId;
            dataset.Id_Group = request.GroupId;
            dataset.SensorName = request.SensorName;
            dataset.Is_Dataset = request.IsDataset;
            dataset.ContentType = request.ContentType;
            dataset.JsonFilters = request.JsonFilters;

            // Marcar explícitamente los campos nullable como modificados
            // para asegurar que EF detecte cuando se setean a null
            _context.Entry(dataset).Property(d => d.Id_Source).IsModified = true;
            _context.Entry(dataset).Property(d => d.Id_Group).IsModified = true;
            _context.Entry(dataset).Property(d => d.SensorName).IsModified = true;
            _context.Entry(dataset).Property(d => d.JsonFilters).IsModified = true;

            // Limpiar todas las relaciones existentes
            ClearExistingRelations(dataset);

            // Lógica de actualización según el tipo de dataset
            if (request.IsDataset == "S")
            {
                // Dataset formal: agregar DeviceIds específicos si existen
                // (La lógica dinámica de Source/Group se aplica en GetDatasetIMByIdAsync)
                if (request.DeviceIds != null && request.DeviceIds.Any())
                {
                    foreach (var deviceId in request.DeviceIds)
                    {
                        dataset.DatasetDevices.Add(new DatasetDevice 
                        { 
                            DatasetId = dataset.Id,
                            Id_device = deviceId,
                        });
                    }
                }
            }
            else if (request.IsDataset == "N")
            {
                // Dataset no formal: verificar si tiene filtros o DeviceIds específicos
                if ((request.Filters != null && request.Filters.Any()) || !string.IsNullOrEmpty(request.JsonFilters))
                {
                    // Procesar y persistir elementos filtrados
                    await ProcessFilteredRelationsForUpdate(dataset, request, username);
                }
                else if (request.DeviceIds != null && request.DeviceIds.Any())
                {
                    // Agregar devices específicos para dataset no formal sin filtros
                    foreach (var deviceId in request.DeviceIds)
                    {
                        dataset.DatasetDevices.Add(new DatasetDevice 
                        { 
                            DatasetId = dataset.Id,
                            Id_device = deviceId,
                        });
                    }
                }
            }

            _context.DatasetsIM.Update(dataset);
            await _context.SaveChangesAsync();

            return dataset;
        }

        private void ClearExistingRelations(DatasetIM dataset)
        {
            // Eliminar devices existentes
            var existingDevicesToRemove = dataset.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();
            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
            }
            dataset.DatasetDevices.Clear();

            // Eliminar sources existentes
            var existingSourcesToRemove = dataset.DatasetSources
                .Where(ds => ds.Id > 0)
                .ToList();
            if (existingSourcesToRemove.Any())
            {
                _context.DatasetSources.RemoveRange(existingSourcesToRemove);
            }
            dataset.DatasetSources.Clear();

            // Eliminar sensors existentes
            var existingSensorsToRemove = dataset.DatasetSensors
                .Where(ds => ds.Id > 0)
                .ToList();
            if (existingSensorsToRemove.Any())
            {
                _context.DatasetSensors.RemoveRange(existingSensorsToRemove);
            }
            dataset.DatasetSensors.Clear();
        }

        private async Task ProcessFilteredRelationsForUpdate(DatasetIM dataset, CreateDatasetIMRequest request, string username)
        {
            try
            {
                // Usar directamente los filtros de la request si están disponibles
                var filters = request.Filters;
                if (filters == null || !filters.Any())
                {
                    // Fallback: intentar deserializar desde JsonFilters si no hay filtros directos
                    if (!string.IsNullOrEmpty(request.JsonFilters))
                    {
                        filters = JsonSerializer.Deserialize<List<FilterCondition>>(request.JsonFilters);
                    }
                    
                    if (filters == null || !filters.Any())
                    {
                        return;
                    }
                }

                // Procesar según el ContentType
                switch (request.ContentType)
                {
                    case "1": // Device
                        await ProcessAndPersistDevices(dataset, filters, username);
                        break;
                    case "2": // Source
                        await ProcessAndPersistSources(dataset, filters, username);
                        break;
                    case "3": // Sensor
                        await ProcessAndPersistSensors(dataset, filters, username);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializando filtros JSON en actualización: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un dataset y sus relaciones con devices.
        /// </summary>
        public async Task DeleteDatasetIMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .Include(d => d.DatasetSources)
                .Include(d => d.DatasetSensors)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            // Eliminar las relaciones DatasetDevice que ya están en la BD (con ID > 0)
            var existingDevicesToRemove = dataset.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();

            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
            }

            // Eliminar las relaciones DatasetSource que ya están en la BD (con ID > 0)
            var existingSourcesToRemove = dataset.DatasetSources
                .Where(ds => ds.Id > 0)
                .ToList();

            if (existingSourcesToRemove.Any())
            {
                _context.DatasetSources.RemoveRange(existingSourcesToRemove);
            }

            // Eliminar las relaciones DatasetSensor que ya están en la BD (con ID > 0)
            var existingSensorsToRemove = dataset.DatasetSensors
                .Where(ds => ds.Id > 0)
                .ToList();

            if (existingSensorsToRemove.Any())
            {
                _context.DatasetSensors.RemoveRange(existingSensorsToRemove);
            }

            // Eliminar el dataset
            _context.DatasetsIM.Remove(dataset);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Identifica rápidamente a qué módulo pertenece un dataset chequeando las tablas.
        /// Retorna: "Insight Monitor", "Asset Manager", "Urban Monitor", "Event Manager", o null si no existe.
        /// </summary>
        public async Task<string?> IdentifyDatasetModuleAsync(int datasetId, string username)
        {
            // Check Insight Monitor table
            var existsInIM = await _context.DatasetsIM
                .AnyAsync(d => d.Id == datasetId && d.Username == username);
            if (existsInIM)
                return "Insight Monitor";

            // Check Asset Manager table
            var existsInAM = await _context.DatasetAM
                .AnyAsync(d => d.Id_Dataset == datasetId && d.Username == username);
            if (existsInAM)
                return "Asset Manager";

            // Check Urban Monitor table
            var existsInUM = await _context.DatasetsUM
                .AnyAsync(d => d.Id == datasetId && d.Username == username);
            if (existsInUM)
                return "Urban Monitor";

            // Check Event Manager table
            var existsInEM = await _context.DatasetsEM
                .AnyAsync(d => d.Id == datasetId && d.Username == username);
            if (existsInEM)
                return "Event Manager";

            return null;
        }
    }
}
