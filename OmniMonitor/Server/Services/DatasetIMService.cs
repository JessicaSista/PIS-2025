using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
// It is assumed that there is a service and DTOs to interact with the Sonda API
using OmniMonitor.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    // --- Service interface ---
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

    // --- Service implementation ---
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
        /// Creates a new dataset, either a formal one ('S') or an internal one for a single element ('N').
        /// </summary>
        public async Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest request, int dataset)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            // Validate that there is no other dataset with the same name for the same user
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
               newDataset.ContentType = "0"; // 0 to indicate a formal dataset
            }
            else // If IsDataset is 'N'
            {
                if (request.DeviceIds != null && request.DeviceIds.Any())
                {
                    newDataset.ContentType = "1"; // 1 to indicate a device
                }
                else if (request.SourceId.HasValue)
                {
                    newDataset.ContentType = "2"; // 2 to indicate a source
                }
                else if (!string.IsNullOrEmpty(request.SensorName))
                {
                    newDataset.ContentType = "3"; // 3 to indicate a sensor
                }
            }

            // If the user selected specific devices, we add them.
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
        /// Gets all datasets for a specific user.
        /// </summary>
        public async Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username)
        {
            return await _context.DatasetsIM
                .Where(d => d.Username == username)
                .ToListAsync();
        }

        /// <summary>
        /// Gets a dataset by its ID and username, applying the device loading logic.
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

            // If it's a formal dataset ('S') and no devices were selected, we search for them dynamically.
            if (dataset.Is_Dataset == "S" && !dataset.DatasetDevices.Any())
            {
                // To call the external API, we need the user's credentials.
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);
                if (user == null)
                {
                    // Cannot proceed if the user does not exist in the local database.
                    return null;
                }

                // --- MODIFIED LOGIC: Optimized dynamic search ---
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

                // 2. Determine the final list of devices from the obtained lists.
                List<Device> finalDeviceList = new List<Device>();

                if (devicesFromSource != null && devicesFromGroup != null)
                {
                    // AND case: Intersection of both lists. Devices that are in both are needed.
                    var deviceIdsFromGroup = new HashSet<int>(devicesFromGroup.Select(d => d.Id));
                    finalDeviceList = devicesFromSource.Where(d => deviceIdsFromGroup.Contains(d.Id)).ToList();
                }
                else if (devicesFromSource != null)
                {
                    // Only filtered by source.
                    finalDeviceList = devicesFromSource;
                }
                else if (devicesFromGroup != null)
                {
                    // Only filtered by group.
                    finalDeviceList = devicesFromGroup;
                }
                else
                {
                    // Fallback: if there's no source or group, get all.
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

                    // 3. Add the IDs of the found devices to the dataset.
                    var foundDeviceIds = filteredDevices.Select(d => d.Id).ToList();
                    foreach (var deviceId in foundDeviceIds)
                    {
                        dataset.DatasetDevices.Add(new DatasetDevice { Id_device = deviceId });
                    }
                }
            }

            // If it's an internal dataset ('N') or a formal one with already selected devices,
            // we simply return it as is.
            return dataset;
        }

        /// <summary>
        /// Gets a dataset by its ID and username for editing, WITHOUT applying dynamic search logic.
        /// Returns the dataset exactly as stored in the database.
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
        /// Updates an existing dataset.
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

            // Duplicate name validation is done in the general table (UpdateDatasetAsyncIM)
            // to guarantee global uniqueness across all modules

            // Update fields
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

            // Update the list of devices
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

            // Delete the dataset
            _context.DatasetsIM.Remove(dataset);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Quickly identifies which module a dataset belongs to by checking the tables.
        /// Returns: "Insight Monitor", "Asset Manager", "Urban Monitor", "Event Manager", or null if it doesn't exist.
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
