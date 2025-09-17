using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Attributes;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System.ComponentModel.DataAnnotations;

namespace OmniMonitor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatasetController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatasetController> _logger;
    private readonly ISondaIMService _sondaApiService;

    public DatasetController(ApplicationDbContext context, ILogger<DatasetController> logger, ISondaIMService sondaApiService)
    {
        _context = context;
        _logger = logger;
        _sondaApiService = sondaApiService;
    }

    /// <summary>
    /// Obtiene la lista de datasets del usuario/empresa
    /// </summary>
    [HttpGet]
    [RequirePermission("Ver Datasets")]
    public async Task<ActionResult<DatasetListResponse>> GetDatasets([FromQuery] int? userId = null, [FromQuery] int? tenantId = null)
    {
        try
        {
            var query = _context.Datasets.AsQueryable();
            
            // Filtrar por usuario si se especifica
            if (userId.HasValue)
            {
                query = query.Where(d => d.UserId == userId.Value);
            }
            
            // Filtrar por tenant si se especifica
            if (tenantId.HasValue)
            {
                query = query.Where(d => d.TenantId == tenantId.Value);
            }

            var datasets = await query
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return Ok(new DatasetListResponse
            {
                Success = true,
                Message = "Datasets obtenidos exitosamente",
                Data = datasets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datasets");
            return StatusCode(500, new DatasetListResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Errors = new List<string> { "No se pudieron obtener los datasets" }
            });
        }
    }

    /// <summary>
    /// Obtiene las opciones necesarias para completar el modal de nuevo dataset
    /// </summary>
    [HttpGet("options")]
    [RequirePermission("Crear Datasets")]
    public async Task<ActionResult<DatasetOptionsResponse>> GetDatasetOptions([FromQuery] int? tenantId = null)
    {
        try
        {
            // Por ahora solo retornamos Insight Monitor como módulo fijo
            var modules = new List<string> { "Insight Monitor" };

            // Obtener datos reales de la API externa
            var sources = await _sondaApiService.GetAllSources("admin", "admin");
            var deviceGroups = await _sondaApiService.GetAllDeviceGroups("admin", "admin");
            
            // Obtener dispositivos (primera página)
            var devices = await _sondaApiService.GetAllDevicesByPage(1, "admin", "admin") ?? new List<Device>();

            // Obtener sensores (simulados por ahora)
            var sensors = new List<Sensor>
            {
                new Sensor { Name = "Temperatura", DisplayName = "Sensor de Temperatura", Type = "Temperature" },
                new Sensor { Name = "Humedad", DisplayName = "Sensor de Humedad", Type = "Humidity" },
                new Sensor { Name = "CO2", DisplayName = "Sensor de CO2", Type = "CO2" }
            };

            return Ok(new DatasetOptionsResponse
            {
                Success = true,
                Message = "Opciones obtenidas exitosamente",
                Data = new DatasetOptionsData
                {
                    Modules = modules,
                    Sources = sources,
                    DeviceGroups = deviceGroups,
                    Sensors = sensors,
                    Devices = devices
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener opciones de dataset");
            return StatusCode(500, new DatasetOptionsResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Errors = new List<string> { "No se pudieron obtener las opciones" }
            });
        }
    }

    /// <summary>
    /// Crea un nuevo dataset
    /// </summary>
    [HttpPost]
    [RequirePermission("Crear Datasets")]
    public async Task<ActionResult<DatasetResponse>> CreateDataset([FromBody] CreateDatasetRequest request)
    {
        try
        {
            // Validar el modelo
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(new DatasetResponse
                {
                    Success = false,
                    Message = "Datos de entrada inválidos",
                    Errors = errors
                });
            }

            // Validaciones de negocio
            var validationResult = await ValidateDatasetRequest(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new DatasetResponse
                {
                    Success = false,
                    Message = "Error de validación",
                    Errors = validationResult.Errors
                });
            }

            // Crear el dataset
            var dataset = new Dataset
            {
                Name = request.Name,
                Description = request.Description,
                Module = request.Module,
                SourceId = request.SourceId,
                DeviceGroupId = request.DeviceGroupId,
                SensorIds = request.SensorIds,
                DeviceIds = request.DeviceIds,
                UserId = request.UserId,
                TenantId = request.TenantId
            };

            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dataset creado exitosamente: {DatasetName} (ID: {DatasetId})", dataset.Name, dataset.Id);

            return Ok(new DatasetResponse
            {
                Success = true,
                Message = "Dataset creado exitosamente",
                Data = dataset
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_Dataset_Name_Module") == true)
        {
            _logger.LogWarning(ex, "Intento de crear dataset con nombre duplicado: {DatasetName}", request.Name);
            return Conflict(new DatasetResponse
            {
                Success = false,
                Message = "Ya existe un dataset con ese nombre en el módulo especificado",
                Errors = new List<string> { "El nombre del dataset debe ser único dentro del módulo" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear dataset: {DatasetName}", request.Name);
            return StatusCode(500, new DatasetResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Errors = new List<string> { "No se pudo crear el dataset" }
            });
        }
    }

    /// <summary>
    /// Obtiene un dataset específico por ID
    /// </summary>
    [HttpGet("{id}")]
    [RequirePermission("Ver Datasets")]
    public async Task<ActionResult<DatasetResponse>> GetDataset(int id)
    {
        try
        {
            var dataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dataset == null)
            {
                return NotFound(new DatasetResponse
                {
                    Success = false,
                    Message = "Dataset no encontrado",
                    Errors = new List<string> { "El dataset especificado no existe" }
                });
            }

            return Ok(new DatasetResponse
            {
                Success = true,
                Message = "Dataset obtenido exitosamente",
                Data = dataset
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dataset con ID: {DatasetId}", id);
            return StatusCode(500, new DatasetResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Errors = new List<string> { "No se pudo obtener el dataset" }
            });
        }
    }

    /// <summary>
    /// Valida que los sensores, dispositivos, fuentes y grupos existan en el sistema externo
    /// </summary>
    [HttpPost("validate-external-data")]
    [RequirePermission("Crear Datasets")]
    public async Task<ActionResult<ExternalDataValidationResponse>> ValidateExternalData([FromBody] ValidateExternalDataRequest request)
    {
        try
        {
            var errors = new List<string>();
            var validSensorIds = new List<string>();
            var validDeviceIds = new List<int>();
            var validSourceIds = new List<int>();
            var validDeviceGroupIds = new List<int>();

            // Simular validación de sensores (en el futuro esto vendrá de una API externa)
            if (request.SensorIds.Any())
            {
                var validSensors = new List<string> { "Temperatura", "Humedad", "CO2", "Potencia", "NivelDeBrillo", "NivelDeRuido", "HumedadDelSuelo", "TemperaturaDelSuelo" };
                
                foreach (var sensorId in request.SensorIds)
                {
                    if (validSensors.Contains(sensorId))
                    {
                        validSensorIds.Add(sensorId);
                    }
                    else
                    {
                        errors.Add($"El sensor '{sensorId}' no existe en el sistema");
                    }
                }
            }

            // Validar dispositivos usando la API externa
            if (request.DeviceIds.Any())
            {
                var allDevices = await _sondaApiService.GetAllDevicesByPage(1, "admin", "admin");
                var validDeviceIdsFromApi = allDevices?.Select(d => d.Id).ToList() ?? new List<int>();
                
                foreach (var deviceId in request.DeviceIds)
                {
                    if (validDeviceIdsFromApi.Contains(deviceId))
                    {
                        validDeviceIds.Add(deviceId);
                    }
                    else
                    {
                        errors.Add($"El dispositivo con ID '{deviceId}' no existe en el sistema");
                    }
                }
            }

            // Validar fuentes usando la API externa
            if (request.SourceIds.Any())
            {
                var allSources = await _sondaApiService.GetAllSources("admin", "admin");
                var validSourceIdsFromApi = allSources.Select(s => s.Id).ToList();
                
                foreach (var sourceId in request.SourceIds)
                {
                    if (validSourceIdsFromApi.Contains(sourceId))
                    {
                        validSourceIds.Add(sourceId);
                    }
                    else
                    {
                        errors.Add($"La fuente con ID '{sourceId}' no existe en el sistema");
                    }
                }
            }

            // Validar grupos de dispositivos usando la API externa
            if (request.DeviceGroupIds.Any())
            {
                var allDeviceGroups = await _sondaApiService.GetAllDeviceGroups("admin", "admin");
                var validDeviceGroupIdsFromApi = allDeviceGroups.Select(g => g.Id).ToList();
                
                foreach (var groupId in request.DeviceGroupIds)
                {
                    if (validDeviceGroupIdsFromApi.Contains(groupId))
                    {
                        validDeviceGroupIds.Add(groupId);
                    }
                    else
                    {
                        errors.Add($"El grupo de dispositivos con ID '{groupId}' no existe en el sistema");
                    }
                }
            }

            return Ok(new ExternalDataValidationResponse
            {
                Success = !errors.Any(),
                Message = errors.Any() ? "Algunos datos externos no son válidos" : "Todos los datos externos son válidos",
                IsValid = !errors.Any(),
                Errors = errors,
                ValidSensorIds = validSensorIds,
                ValidDeviceIds = validDeviceIds,
                ValidSourceIds = validSourceIds,
                ValidDeviceGroupIds = validDeviceGroupIds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar datos externos");
            return StatusCode(500, new ExternalDataValidationResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                IsValid = false,
                Errors = new List<string> { "No se pudieron validar los datos externos" }
            });
        }
    }

    /// <summary>
    /// Valida la solicitud de creación de dataset
    /// </summary>
    private async Task<ValidationResult> ValidateDatasetRequest(CreateDatasetRequest request)
    {
        var errors = new List<string>();

        // Validar que se proporcione exactamente una opción entre source o group
        if (request.SourceId.HasValue && request.DeviceGroupId.HasValue)
        {
            errors.Add("Debe elegir solo una opción entre Source o DeviceGroup, no ambas");
        }
        else if (!request.SourceId.HasValue && !request.DeviceGroupId.HasValue)
        {
            errors.Add("Debe elegir una opción entre Source o DeviceGroup");
        }

        // Validar que el nombre no esté vacío
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("El nombre del dataset es obligatorio");
        }
        else
        {
            // Validar que el nombre no esté duplicado en el mismo módulo, tenant y usuario
            var existingDataset = await _context.Datasets
                .FirstOrDefaultAsync(d => d.Name == request.Name && 
                                        d.Module == request.Module && 
                                        d.TenantId == request.TenantId &&
                                        d.UserId == request.UserId);
            
            if (existingDataset != null)
            {
                errors.Add("Ya existe un dataset con ese nombre en el módulo especificado");
            }
        }

        // Validar que el usuario existe
        if (request.UserId <= 0)
        {
            errors.Add("El ID de usuario es obligatorio y debe ser válido");
        }
        else
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
            if (!userExists)
            {
                errors.Add("El usuario especificado no existe");
            }
        }

        // Validar que el módulo sea válido
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            errors.Add("El módulo es obligatorio");
        }
        else if (request.Module != "Insight Monitor")
        {
            errors.Add("Solo se permite el módulo 'Insight Monitor' por ahora");
        }

        // Validar sensores y dispositivos si se proporcionan
        if (request.SensorIds.Any())
        {
            // En el futuro, validar que los sensores existan en la API externa
            // Por ahora solo validamos que no estén vacíos
            if (request.SensorIds.Any(s => string.IsNullOrWhiteSpace(s)))
            {
                errors.Add("Los IDs de sensores no pueden estar vacíos");
            }
        }

        if (request.DeviceIds.Any())
        {
            // En el futuro, validar que los dispositivos existan en la API externa
            // Por ahora solo validamos que no estén vacíos
            if (request.DeviceIds.Any(d => d <= 0))
            {
                errors.Add("Los IDs de dispositivos deben ser válidos");
            }
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }

    /// <summary>
    /// Resultado de validación
    /// </summary>
    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
