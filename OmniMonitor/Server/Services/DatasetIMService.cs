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
    public interface IDatasetService
    {
        Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest request, int dataset);
        Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username);
        Task<DatasetIM?> GetDatasetIMByIdAsync(int datasetId, string username);
        Task<DatasetIM?> GetDatasetIMByIdForEditAsync(int datasetId, string username);
        Task<DatasetIM?> GetDatasetIMByIdForEditAsyncSinToken(int datasetId);
        Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM dataset, CreateDatasetIMRequest request);
        Task DeleteDatasetIMAsync(int datasetId, string username);
        Task<string?> IdentifyDatasetModuleAsync(int datasetId, string username);
    }

    // --- Implementación del servicio ---
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
        public async Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest request, int dataset)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            // Validar que no exista otro dataset con el mismo nombre para el mismo usuario
            var existingDataset = await _context.DatasetsIM
                .FirstOrDefaultAsync(d => d.Username == request.Username && d.Name == request.Name);
            
            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Name}' para el usuario '{request.Username}'.");
            }

            var newDataset = new DatasetIM
            {
                Username = request.Username,
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

                // --- LÓGICA MODIFICADA: Búsqueda dinámica optimizada ---
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
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);
        }

        public async Task<DatasetIM?> GetDatasetIMByIdForEditAsyncSinToken(int datasetId)
        {
            return await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .FirstOrDefaultAsync(d => d.Id == datasetId);
        }

        /// <summary>
        /// Actualiza un dataset existente.
        /// </summary>
        public async Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM dataset, CreateDatasetIMRequest request)
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
            // para garantizar unicidad global entre todos los módulos

            // Actualizar campos
            dataset.Name = request.Name;
            dataset.Description = request.Description;
            dataset.Id_Source = request.SourceId;
            dataset.Id_Group = request.GroupId;
            dataset.SensorName = request.SensorName;
            dataset.Is_Dataset = request.IsDataset;
            dataset.ContentType = request.ContentType;

            // Marcar explícitamente los campos nullable como modificados
            // para asegurar que EF detecte cuando se setean a null
            _context.Entry(dataset).Property(d => d.Id_Source).IsModified = true;
            _context.Entry(dataset).Property(d => d.Id_Group).IsModified = true;
            _context.Entry(dataset).Property(d => d.SensorName).IsModified = true;

            // Actualizar la lista de devices
            // Solo eliminar los devices que ya están guardados en la BD (con ID > 0)
            var existingDevicesToRemove = dataset.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();

            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
            }

            // Limpiar toda la colección
            dataset.DatasetDevices.Clear();

            // Agregar los nuevos devices si existen
            if (request.DeviceIds != null)
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

            _context.DatasetsIM.Update(dataset);
            await _context.SaveChangesAsync();

            return dataset;
        }

        /// <summary>
        /// Elimina un dataset y sus relaciones con devices.
        /// </summary>
        public async Task DeleteDatasetIMAsync(int datasetId, string username)
        {
            var dataset = await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.Username == username);

            if (dataset == null)
            {
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            // Solo eliminar las relaciones DatasetDevice que ya están en la BD (con ID > 0)
            var existingDevicesToRemove = dataset.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();

            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
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
