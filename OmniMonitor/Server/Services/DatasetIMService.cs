using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para el servicio de gestión de datasets Insight Monitor.
    /// </summary>
    public interface IDatasetService
    {
        /// <summary>
        /// Crea un nuevo dataset IM.
        /// </summary>
        /// <param name="createDatasetImRequest">Datos para la creación del dataset.</param>
        /// <param name="datasetId">ID del dataset general asociado.</param>
        /// <returns>El dataset IM creado.</returns>
        Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest createDatasetImRequest, int datasetId);

        /// <summary>
        /// Obtiene todos los datasets IM de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de datasets IM.</returns>
        Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username);

        /// <summary>
        /// Obtiene un dataset IM por su ID y usuario.
        /// </summary>
        /// <param name="datasetId">ID del dataset IM.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset IM encontrado o null.</returns>
        Task<DatasetIM?> GetDatasetIMByIdAsync(int datasetId, string username);

        /// <summary>
        /// Obtiene un dataset IM para edición.
        /// </summary>
        /// <param name="datasetId">ID del dataset IM.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset IM encontrado o null.</returns>
        Task<DatasetIM?> GetDatasetIMByIdForEditAsync(int datasetId, string username);

        /// <summary>
        /// Actualiza un dataset IM existente.
        /// </summary>
        /// <param name="datasetIm">Entidad DatasetIM a actualizar.</param>
        /// <param name="updateRequest">Datos para la actualización.</param>
        /// <returns>El dataset IM actualizado.</returns>
        Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM datasetIm, CreateDatasetIMRequest updateRequest);

        /// <summary>
        /// Elimina un dataset IM.
        /// </summary>
        /// <param name="datasetId">ID del dataset IM.</param>
        /// <param name="username">Nombre de usuario.</param>
        Task DeleteDatasetIMAsync(int datasetId, string username);

        /// <summary>
        /// Identifica a qué módulo pertenece un dataset.
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Nombre del módulo o null.</returns>
        Task<string?> IdentifyDatasetModuleAsync(int datasetId, string username);
    }

    /// <summary>
    /// Servicio para la gestión de datasets Insight Monitor.
    /// </summary>
    public class DatasetIMService : IDatasetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaIMService _sondaImService;
        private readonly ILogger<DatasetIMService> _logger;

        /// <summary>
        /// Constructor del servicio DatasetIMService.
        /// </summary>
        public DatasetIMService(ApplicationDbContext context, ISondaIMService sondaImService, ILogger<DatasetIMService> logger)
        {
            _context = context;
            _sondaImService = sondaImService;
            _logger = logger;
        }

        #region Métodos públicos

        /// <inheritdoc/>
        public async Task<DatasetIM> CreateDatasetIMAsync(CreateDatasetIMRequest createDatasetImRequest, int datasetId)
        {
            if (string.IsNullOrEmpty(createDatasetImRequest.Username) || string.IsNullOrEmpty(createDatasetImRequest.Name))
            {
                _logger.LogWarning("El nombre de usuario o el nombre del dataset es nulo o vacío.");
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            var existingDatasetIm = await _context.DatasetsIM
                .AsNoTracking()
                .FirstOrDefaultAsync(d => string.Equals(d.Username, createDatasetImRequest.Username) && string.Equals(d.Name, createDatasetImRequest.Name));

            if (existingDatasetIm != null)
            {
                _logger.LogWarning("Ya existe un dataset con el nombre '{Name}' para el usuario '{Username}'.", createDatasetImRequest.Name, createDatasetImRequest.Username);
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{createDatasetImRequest.Name}' para el usuario '{createDatasetImRequest.Username}'.");
            }

            var newDatasetIm = new DatasetIM
            {
                Username = createDatasetImRequest.Username,
                Name = createDatasetImRequest.Name,
                Description = createDatasetImRequest.Description,
                Is_Dataset = createDatasetImRequest.IsDataset,
                Id_Source = createDatasetImRequest.SourceId,
                Id_Group = createDatasetImRequest.GroupId,
                SensorName = createDatasetImRequest.SensorName,
                DatasetId = datasetId,
                DatasetDevices = new List<DatasetDevice>()
            };

            if (string.Equals(createDatasetImRequest.IsDataset, "S"))
            {
                newDatasetIm.ContentType = "0";
            }
            else
            {
                if (createDatasetImRequest.DeviceIds != null && createDatasetImRequest.DeviceIds.Any())
                {
                    newDatasetIm.ContentType = "1";
                }
                else if (createDatasetImRequest.SourceId.HasValue)
                {
                    newDatasetIm.ContentType = "2";
                }
                else if (!string.IsNullOrEmpty(createDatasetImRequest.SensorName))
                {
                    newDatasetIm.ContentType = "3";
                }
            }

            if (createDatasetImRequest.DeviceIds != null && createDatasetImRequest.DeviceIds.Any())
            {
                foreach (var deviceId in createDatasetImRequest.DeviceIds)
                {
                    newDatasetIm.DatasetDevices.Add(new() { Id_device = deviceId });
                }
            }

            _context.DatasetsIM.Add(newDatasetIm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetIM creado correctamente para el usuario {Username} con nombre {Name}.", createDatasetImRequest.Username, createDatasetImRequest.Name);

            return newDatasetIm;
        }

        /// <inheritdoc/>
        public async Task<List<DatasetIM>> GetAllDatasetsIMAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("El nombre de usuario es nulo o vacío al intentar obtener todos los datasets IM.");
                return new();
            }

            var result = await _context.DatasetsIM
                .AsNoTracking()
                .Where(d => string.Equals(d.Username, username))
                .ToListAsync();

            _logger.LogInformation("Se obtuvieron {Count} datasets IM para el usuario {Username}.", result.Count, username);

            return result;
        }

        /// <inheritdoc/>
        public async Task<DatasetIM?> GetDatasetIMByIdAsync(int datasetId, string username)
        {
            var datasetIm = await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));

            if (datasetIm == null)
            {
                _logger.LogWarning("No se encontró el dataset IM con ID {DatasetId} para el usuario {Username}.", datasetId, username);
                return null;
            }

            if (string.Equals(datasetIm.Is_Dataset, "S") && !datasetIm.DatasetDevices.Any())
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => string.Equals(u.UserName, username));
                if (user == null)
                {
                    _logger.LogWarning("No se encontró el usuario {Username} en la base de datos.", username);
                    return null;
                }

                List<Device>? devicesFromSource = null;
                List<Device>? devicesFromGroup = null;

                if (datasetIm.Id_Source.HasValue)
                {
                    devicesFromSource = await _sondaImService.GetDeviceOfSource(datasetIm.Id_Source.Value, user.UserName);
                }
                if (datasetIm.Id_Group.HasValue)
                {
                    devicesFromGroup = await _sondaImService.GetDeviceOfGroup(datasetIm.Id_Group.Value, user.UserName);
                }

                List<Device> finalDeviceList = new();

                if (devicesFromSource != null && devicesFromGroup != null)
                {
                    var deviceIdsFromGroup = new HashSet<int>(devicesFromGroup.Select(d => d.Id));
                    finalDeviceList = devicesFromSource.Where(d => deviceIdsFromGroup.Contains(d.Id)).ToList();
                }
                else if (devicesFromSource != null)
                {
                    finalDeviceList = devicesFromSource;
                }
                else if (devicesFromGroup != null)
                {
                    finalDeviceList = devicesFromGroup;
                }
                else
                {
                    finalDeviceList = await _sondaImService.GetAllDevices(user.UserName) ?? new();
                }

                if (finalDeviceList.Any())
                {
                    var foundDeviceIds = finalDeviceList.Select(d => d.Id).ToList();
                    foreach (var deviceId in foundDeviceIds)
                    {
                        datasetIm.DatasetDevices.Add(new() { Id_device = deviceId });
                    }
                }
            }

            _logger.LogInformation("Se obtuvo el dataset IM con ID {DatasetId} para el usuario {Username}.", datasetId, username);

            return datasetIm;
        }

        /// <inheritdoc/>
        public async Task<DatasetIM?> GetDatasetIMByIdForEditAsync(int datasetId, string username)
        {
            return await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));
        }

        /// <inheritdoc/>
        public async Task<DatasetIM> UpdateDatasetIMAsync(DatasetIM datasetIm, CreateDatasetIMRequest updateRequest)
        {
            if (datasetIm == null)
            {
                _logger.LogWarning("El dataset a actualizar es nulo.");
                throw new ArgumentNullException(nameof(datasetIm), "El dataset no puede ser nulo.");
            }

            if (string.IsNullOrEmpty(updateRequest.Name))
            {
                _logger.LogWarning("El nombre del dataset es nulo o vacío.");
                throw new ArgumentException("El nombre del dataset es obligatorio.");
            }

            // Validar que no exista otro dataset con el mismo nombre (excluyendo el actual)
            if (!string.IsNullOrEmpty(dataset.Name) && dataset.Name != dataset.Name)
            {
                var duplicateDataset = await _context.DatasetsIM
                    .FirstOrDefaultAsync(d => d.Username == dataset.Username && 
                                            d.Name == dataset.Name && 
                                            d.Id != dataset.Id);
                
                if (duplicateDataset != null)
                {
                    throw new InvalidOperationException($"Ya existe un dataset con el nombre '{dataset.Name}' para el usuario '{dataset.Username}'.");
                }
            }
                }
            }

            datasetIm.Name = updateRequest.Name;
            datasetIm.Description = updateRequest.Description;
            datasetIm.Id_Source = updateRequest.SourceId;
            datasetIm.Id_Group = updateRequest.GroupId;
            datasetIm.SensorName = updateRequest.SensorName;
            datasetIm.Is_Dataset = updateRequest.IsDataset;
            datasetIm.ContentType = updateRequest.ContentType;

            _context.Entry(datasetIm).Property(d => d.Id_Source).IsModified = true;
            _context.Entry(datasetIm).Property(d => d.Id_Group).IsModified = true;
            _context.Entry(datasetIm).Property(d => d.SensorName).IsModified = true;

            var existingDevicesToRemove = datasetIm.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();

            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
            }

            datasetIm.DatasetDevices.Clear();

            if (updateRequest.DeviceIds != null)
            {
                foreach (var deviceId in updateRequest.DeviceIds)
                {
                    datasetIm.DatasetDevices.Add(new()
                    {
                        DatasetId = datasetIm.Id,
                        Id_device = deviceId
                    });
                }
            }

            _context.DatasetsIM.Update(datasetIm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetIM actualizado correctamente con ID {DatasetId} para el usuario {Username}.", datasetIm.Id, datasetIm.Username);

            return datasetIm;
        }

        /// <inheritdoc/>
        public async Task DeleteDatasetIMAsync(int datasetId, string username)
        {
            var datasetIm = await _context.DatasetsIM
                .Include(d => d.DatasetDevices)
                .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username));

            if (datasetIm == null)
            {
                _logger.LogWarning("No se encontró el dataset IM con ID {DatasetId} para el usuario {Username}.", datasetId, username);
                throw new InvalidOperationException($"No se encontró el dataset con ID {datasetId} para el usuario {username}.");
            }

            var existingDevicesToRemove = datasetIm.DatasetDevices
                .Where(dd => dd.Id > 0)
                .ToList();

            if (existingDevicesToRemove.Any())
            {
                _context.DatasetDevices.RemoveRange(existingDevicesToRemove);
            }

            _context.DatasetsIM.Remove(datasetIm);
            await _context.SaveChangesAsync();

            _logger.LogInformation("DatasetIM eliminado correctamente con ID {DatasetId} para el usuario {Username}.", datasetId, username);
        }

        /// <inheritdoc/>
        public async Task<string?> IdentifyDatasetModuleAsync(int datasetId, string username)
        {
            var existsInIm = await _context.DatasetsIM
                .AsNoTracking()
                .AnyAsync(d => d.Id == datasetId && string.Equals(d.Username, username));
            if (existsInIm)
            {
                return "Insight Monitor";
            }

            var existsInAm = await _context.DatasetAM
                .AsNoTracking()
                .AnyAsync(d => d.Id_Dataset == datasetId && string.Equals(d.Username, username));
            if (existsInAm)
            {
                return "Asset Manager";
            }

            var existsInUm = await _context.DatasetsUM
                .AsNoTracking()
                .AnyAsync(d => d.Id == datasetId && string.Equals(d.Username, username));
            if (existsInUm)
            {
                return "Urban Monitor";
            }

            var existsInEm = await _context.DatasetsEM
                .AsNoTracking()
                .AnyAsync(d => d.Id == datasetId && string.Equals(d.Username, username));
            if (existsInEm)
            {
                return "Event Manager";
            }

            return null;
        }

        #endregion
    }
}