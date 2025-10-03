using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
// Se asume que existe un servicio y DTOs para interactuar con la API de Sonda
using OmniMonitor.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    // --- Interfaz para el servicio ---
    public interface IDatasetService
    {
        Task<Dataset> CreateDatasetAsync(CreateDatasetRequest request);
        Task<List<Dataset>> GetAllDatasetsAsync(string username);
        Task<Dataset?> GetDatasetByIdAsync(int datasetId, string username);
    }

    // --- Implementación del servicio ---
    public class DatasetService : IDatasetService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaIMService _sondaIMService;

        public DatasetService(ApplicationDbContext context, ISondaIMService sondaIMService)
        {
            _context = context;
            _sondaIMService = sondaIMService;
        }

        /// <summary>
        /// Crea un nuevo dataset, ya sea uno formal ('S') o uno interno para un solo elemento ('N').
        /// </summary>
        public async Task<Dataset> CreateDatasetAsync(CreateDatasetRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Name))
            {
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
            }

            var newDataset = new Dataset
            {
                Username = request.Username,
                Name = request.Name,
                Description = request.Description,
                Is_Dataset = request.IsDataset,
                Id_Source = request.SourceId,
                Id_Group = request.GroupId,
                SensorName = request.SensorName
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

            _context.Datasets.Add(newDataset);
            await _context.SaveChangesAsync();

            return newDataset;
        }

        /// <summary>
        /// Obtiene todos los datasets de un usuario específico.
        /// </summary>
        public async Task<List<Dataset>> GetAllDatasetsAsync(string username)
        {
            return await _context.Datasets
                .Where(d => d.Username == username)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un dataset por su ID y nombre de usuario, aplicando la lógica de carga de devices.
        /// </summary>
        public async Task<Dataset?> GetDatasetByIdAsync(int datasetId, string username)
        {
            var dataset = await _context.Datasets
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
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    // No se puede proceder si el usuario no existe en la base de datos local.
                    return null;
                }

                // --- LÓGICA MODIFICADA: Búsqueda dinámica optimizada ---
                List<Device>? devicesFromSource = null;
                List<Device>? devicesFromGroup = null;

                // 1. Obtener las listas de dispositivos de la API según los filtros proporcionados.
                if (dataset.Id_Source.HasValue)
                {
                    devicesFromSource = await _sondaIMService.GetDeviceOfSource(dataset.Id_Source.Value, user.Username, user.Password);
                }
                if (dataset.Id_Group.HasValue)
                {
                    devicesFromGroup = await _sondaIMService.GetDeviceOfGroup(dataset.Id_Group.Value, user.Username, user.Password);
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
                    finalDeviceList = await _sondaIMService.GetAllDevices(user.Username, user.Password) ?? new List<Device>();
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
    }
}

