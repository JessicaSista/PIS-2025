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
                ContentType = request.ContentType, // Solo relevante si Is_Dataset es 'N'
                Id_Source = request.SourceId,
                Id_Group = request.GroupId,
                SensorName = request.SensorName
            };

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

                // 1. Obtener todos los devices de la API, manejando la paginación.
                List<Device> allDevices = new List<Device>();
                int currentPage = 1;
                List<Device>? pagedDevices;
                do
                {
                    // Se asume que el password almacenado es el correcto para la API.
                    pagedDevices = await _sondaIMService.GetAllDevicesByPage(currentPage, user.Username, user.Password);
                    if (pagedDevices != null && pagedDevices.Any())
                    {
                        allDevices.AddRange(pagedDevices);
                        currentPage++;
                    }
                } while (pagedDevices != null && pagedDevices.Any());

                // 2. Filtrar la lista completa de devices en memoria.
                IEnumerable<Device> filteredDevices = allDevices;

                if (dataset.Id_Source.HasValue)
                {
                    // El objeto Device tiene una propiedad 'SourceId', por lo que este filtro es directo.
                    filteredDevices = filteredDevices.Where(d => d.SourceId == dataset.Id_Source.Value);
                }
                if (dataset.Id_Group.HasValue)
                {
                    // El objeto Device tiene una lista de grupos. Verificamos si alguno de ellos coincide.
                    filteredDevices = filteredDevices.Where(d => d.Groups != null && d.Groups.Any(g => g.Id == dataset.Id_Group.Value));
                }

                if (!string.IsNullOrEmpty(dataset.SensorName))
                {
                    string sensorNameToFind = dataset.SensorName;

                    // Filtramos los devices de la API que contengan un sensor con el nombre especificado.
                    filteredDevices = filteredDevices.Where(d => d.Sensors != null && d.Sensors.Any(s => s.Name == sensorNameToFind));
                }

                // 3. Agregar los IDs de los devices encontrados al dataset.
                var foundDeviceIds = filteredDevices.Select(d => d.Id).ToList();
                foreach (var deviceId in foundDeviceIds)
                {
                    dataset.DatasetDevices.Add(new DatasetDevice { Id_device = deviceId });
                }
            }

            // Si es un dataset interno ('N') o uno formal con devices ya seleccionados,
            // simplemente lo devolvemos tal como está.
            return dataset;
        }
    }
}