using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.Linq.Expressions;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetService
    {
        Task<DatasetResponseDto> CreateDatasetAsync(DatasetCreateDto createDto, int userId);
        Task<DatasetResponseDto?> GetDatasetByIdAsync(int datasetId, int userId);
        Task<DatasetListResponseDto> GetAllDatasetsAsync(int userId, DatasetListRequestDto request);
        Task<DatasetResponseDto> UpdateDatasetAsync(DatasetUpdateDto updateDto, int userId);
        Task<bool> DeleteDatasetAsync(int datasetId, int userId);
        Task<DatasetValidationResultDto> ValidateDatasetMembersAsync(DatasetValidationRequestDto validationRequest, string username, string password);
        Task<DatasetResponseDto> CreateInternalDatasetAsync(string tipoEntidad, int entityId, int sensorId, int userId);
    }

    public class DatasetService : IDatasetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEnumerable<IDatasetModuleValidator> _moduleValidators;
        private readonly ILogger<DatasetService> _logger;

        public DatasetService(
            ApplicationDbContext context,
            IEnumerable<IDatasetModuleValidator> moduleValidators,
            ILogger<DatasetService> logger)
        {
            _context = context;
            _moduleValidators = moduleValidators;
            _logger = logger;
        }

        public async Task<DatasetResponseDto> CreateDatasetAsync(DatasetCreateDto createDto, int userId)
        {
            // Verificar que el nombre no esté duplicado para el usuario
            var existingDataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Nombre == createDto.Nombre && d.IdUsuario == userId);

            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{createDto.Nombre}' para este usuario.");
            }

            // Validar que el usuario existe
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("Usuario no encontrado.");
            }

            // Crear el dataset
            var dataset = new Dataset
            {
                Nombre = createDto.Nombre,
                Descripcion = createDto.Descripcion,
                EsDataset = createDto.EsDataset,
                IdUsuario = userId,
                GrupoDevice = createDto.GrupoDevice,
                IdSource = createDto.IdSource,
                IdGroup = createDto.IdGroup,
                IdSensor = createDto.IdSensor,
                TipoEntidad = createDto.TipoEntidad,
                Modulo = createDto.Modulo,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync();

            // Agregar dispositivos si se proporcionaron
            if (createDto.IdDevices != null && createDto.IdDevices.Any())
            {
                foreach (var deviceId in createDto.IdDevices)
                {
                    var deviceGrupo = new DeviceGrupo
                    {
                        GrupoDevice = createDto.GrupoDevice ?? "",
                        IdDevice = deviceId,
                        IdDataset = dataset.Id,
                        FechaCreacion = DateTime.UtcNow
                    };
                    _context.DeviceGrupos.Add(deviceGrupo);
                }
                await _context.SaveChangesAsync();
            }

            return await GetDatasetResponseDtoAsync(dataset.Id, userId);
        }

        public async Task<DatasetResponseDto?> GetDatasetByIdAsync(int datasetId, int userId)
        {
            var dataset = await _context.Datasets
                .Include(d => d.DeviceGrupos)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.IdUsuario == userId);

            if (dataset == null)
                return null;

            return await GetDatasetResponseDtoAsync(datasetId, userId);
        }

        public async Task<DatasetListResponseDto> GetAllDatasetsAsync(int userId, DatasetListRequestDto request)
        {
            var query = _context.Datasets
                .Where(d => d.IdUsuario == userId);

            // Filtros
            if (!string.IsNullOrEmpty(request.EntityType))
            {
                query = query.Where(d => d.TipoEntidad == request.EntityType);
            }

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where(d => d.Nombre.Contains(request.SearchText) || 
                                       d.Descripcion.Contains(request.SearchText));
            }

            // Ordenamiento
            query = request.OrderBy?.ToLower() switch
            {
                "nombre" => request.OrderDescending ? query.OrderByDescending(d => d.Nombre) : query.OrderBy(d => d.Nombre),
                "fechacreacion" => request.OrderDescending ? query.OrderByDescending(d => d.FechaCreacion) : query.OrderBy(d => d.FechaCreacion),
                "fechamodificacion" => request.OrderDescending ? query.OrderByDescending(d => d.FechaModificacion) : query.OrderBy(d => d.FechaModificacion),
                _ => query.OrderBy(d => d.Nombre)
            };

            var totalCount = await query.CountAsync();

            var datasets = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DatasetListDto
                {
                    Id = d.Id,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion,
                    EsDataset = d.EsDataset,
                    TipoEntidad = d.TipoEntidad,
                    FechaCreacion = d.FechaCreacion,
                    FechaModificacion = d.FechaModificacion,
                    RecordCount = d.DeviceGrupos.Count
                })
                .ToListAsync();

            return new DatasetListResponseDto
            {
                Datasets = datasets,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public async Task<DatasetResponseDto> UpdateDatasetAsync(DatasetUpdateDto updateDto, int userId)
        {
            var dataset = await _context.Datasets
                .Include(d => d.DeviceGrupos)
                .FirstOrDefaultAsync(d => d.Id == updateDto.Id && d.IdUsuario == userId);

            if (dataset == null)
            {
                throw new ArgumentException("Dataset no encontrado.");
            }

            // Verificar que el nombre no esté duplicado (excluyendo el dataset actual)
            var existingDataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Nombre == updateDto.Nombre && d.IdUsuario == userId && d.Id != updateDto.Id);

            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{updateDto.Nombre}' para este usuario.");
            }

            // Actualizar propiedades
            dataset.Nombre = updateDto.Nombre;
            dataset.Descripcion = updateDto.Descripcion;
            dataset.GrupoDevice = updateDto.GrupoDevice;
            dataset.IdSource = updateDto.IdSource;
            dataset.IdGroup = updateDto.IdGroup;
            dataset.IdSensor = updateDto.IdSensor;
            dataset.FechaModificacion = DateTime.UtcNow;

            // Actualizar dispositivos
            if (updateDto.IdDevices != null)
            {
                // Eliminar dispositivos existentes
                _context.DeviceGrupos.RemoveRange(dataset.DeviceGrupos);

                // Agregar nuevos dispositivos
                foreach (var deviceId in updateDto.IdDevices)
                {
                    var deviceGrupo = new DeviceGrupo
                    {
                        GrupoDevice = updateDto.GrupoDevice ?? "",
                        IdDevice = deviceId,
                        IdDataset = dataset.Id,
                        FechaCreacion = DateTime.UtcNow
                    };
                    _context.DeviceGrupos.Add(deviceGrupo);
                }
            }

            await _context.SaveChangesAsync();
            return await GetDatasetResponseDtoAsync(dataset.Id, userId);
        }

        public async Task<bool> DeleteDatasetAsync(int datasetId, int userId)
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.IdUsuario == userId);

            if (dataset == null)
                return false;

            _context.Datasets.Remove(dataset);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DatasetValidationResultDto> ValidateDatasetMembersAsync(DatasetValidationRequestDto validationRequest, string username, string password)
        {
            var result = new DatasetValidationResultDto { IsValid = true };

            try
            {
                // Determinar qué módulo usar basado en el tipo de entidad
                var validator = GetValidatorForEntityType(validationRequest.TipoEntidad);
                
                if (validator == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Tipo de entidad '{validationRequest.TipoEntidad}' no soportado.");
                    return result;
                }

                // Usar el validador específico del módulo
                result = await validator.ValidateDatasetMembersAsync(validationRequest, username, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando miembros del dataset");
                result.IsValid = false;
                result.Errors.Add("Error al validar los miembros del dataset.");
            }

            return result;
        }

        public async Task<DatasetResponseDto> CreateInternalDatasetAsync(string tipoEntidad, int entityId, int sensorId, int userId)
        {
            var createDto = new DatasetCreateDto
            {
                Nombre = $"Dataset Interno - {tipoEntidad} {entityId}",
                Descripcion = $"Dataset interno creado automáticamente para {tipoEntidad} {entityId}",
                TipoEntidad = tipoEntidad,
                IdSensor = sensorId,
                EsDataset = "N" // Dataset interno
            };

            // Configurar según el tipo de entidad
            switch (tipoEntidad.ToLower())
            {
                case "device":
                    createDto.IdDevices = new List<int> { entityId };
                    break;
                case "source":
                    createDto.IdSource = entityId;
                    break;
                case "group":
                    createDto.IdGroup = entityId;
                    break;
                case "sensor":
                    // Para sensores, solo necesitamos el sensor ID
                    break;
            }

            return await CreateDatasetAsync(createDto, userId);
        }

        private async Task<DatasetResponseDto> GetDatasetResponseDtoAsync(int datasetId, int userId)
        {
            var dataset = await _context.Datasets
                .Include(d => d.DeviceGrupos)
                .FirstOrDefaultAsync(d => d.Id == datasetId && d.IdUsuario == userId);

            if (dataset == null)
                return null;

            var devices = new List<DeviceInfoDto>();
            
            if (dataset.EsDataset == "S" && dataset.DeviceGrupos.Any())
            {
                // Para datasets creados por usuario, obtener todos los dispositivos del grupo
                var deviceIds = dataset.DeviceGrupos.Select(dg => dg.IdDevice).ToList();
                devices = deviceIds.Select(id => new DeviceInfoDto
                {
                    Id = id,
                    Name = $"Device {id}", // TODO: Obtener nombre real de la API
                    GrupoDevice = dataset.GrupoDevice
                }).ToList();
            }
            else if (dataset.EsDataset == "N")
            {
                // Para datasets internos, mostrar el dispositivo específico
                if (dataset.DeviceGrupos.Any())
                {
                    var deviceGrupo = dataset.DeviceGrupos.First();
                    devices.Add(new DeviceInfoDto
                    {
                        Id = deviceGrupo.IdDevice,
                        Name = $"Device {deviceGrupo.IdDevice}",
                        GrupoDevice = deviceGrupo.GrupoDevice
                    });
                }
            }

            return new DatasetResponseDto
            {
                Id = dataset.Id,
                Nombre = dataset.Nombre,
                Descripcion = dataset.Descripcion,
                EsDataset = dataset.EsDataset,
                IdUsuario = dataset.IdUsuario,
                GrupoDevice = dataset.GrupoDevice,
                IdSource = dataset.IdSource,
                IdGroup = dataset.IdGroup,
                IdSensor = dataset.IdSensor,
                TipoEntidad = dataset.TipoEntidad,
                Modulo = dataset.Modulo,
                FechaCreacion = dataset.FechaCreacion,
                FechaModificacion = dataset.FechaModificacion,
                RecordCount = devices.Count,
                Devices = devices
            };
        }

        /// <summary>
        /// Obtiene el validador apropiado para el tipo de entidad
        /// </summary>
        private IDatasetModuleValidator? GetValidatorForEntityType(string entityType)
        {
            return _moduleValidators.FirstOrDefault(v => v.SupportedEntityTypes.Contains(entityType.ToLower()));
        }
    }
}
