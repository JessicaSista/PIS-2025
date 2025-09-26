using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Validador para el módulo IM (Infrastructure Management)
    /// Maneja devices, sources, sensores y grupos
    /// </summary>
    public class DatasetIMValidator : IDatasetModuleValidator
    {
        private readonly ISondaIMService _sondaIMService;
        private readonly ILogger<DatasetIMValidator> _logger;

        public string ModuleName => "IM";
        public List<string> SupportedEntityTypes => new List<string> { "device", "source", "group", "sensor" };

        public DatasetIMValidator(ISondaIMService sondaIMService, ILogger<DatasetIMValidator> logger)
        {
            _sondaIMService = sondaIMService;
            _logger = logger;
        }

        public async Task<DatasetValidationResultDto> ValidateDatasetMembersAsync(
            DatasetValidationRequestDto validationRequest, 
            string username, 
            string password)
        {
            var result = new DatasetValidationResultDto { IsValid = true };

            try
            {
                // Validar dispositivos si se proporcionaron
                if (validationRequest.IdDevices != null && validationRequest.IdDevices.Any())
                {
                    foreach (var deviceId in validationRequest.IdDevices)
                    {
                        var device = await _sondaIMService.GetDeviceById(deviceId, username, password);
                        if (device == null)
                        {
                            result.InvalidDeviceIds.Add(deviceId);
                            result.IsValid = false;
                        }
                    }
                }

                // Validar source si se proporcionó
                if (validationRequest.IdSource.HasValue)
                {
                    var source = await _sondaIMService.GetSourceById(validationRequest.IdSource.Value, username, password);
                    if (source == null)
                    {
                        result.InvalidSourceIds.Add(validationRequest.IdSource.Value);
                        result.IsValid = false;
                    }
                }

                // Validar grupo si se proporcionó
                if (validationRequest.IdGroup.HasValue)
                {
                    var group = await _sondaIMService.GetDeviceGroupById(validationRequest.IdGroup.Value, username, password);
                    if (group == null)
                    {
                        result.InvalidGroupIds.Add(validationRequest.IdGroup.Value);
                        result.IsValid = false;
                    }
                }

                // TODO: Validar sensor - necesitaríamos un servicio para sensores
                // Por ahora asumimos que el sensor es válido

                if (!result.IsValid)
                {
                    result.Errors.Add("Algunos IDs no existen o no tienes permisos para acceder a ellos en el módulo IM.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando miembros del dataset en módulo IM");
                result.IsValid = false;
                result.Errors.Add("Error al validar los miembros del dataset en el módulo IM.");
            }

            return result;
        }

        public async Task<List<EntityInfoDto>> GetEntityInfoAsync(
            List<int> entityIds, 
            string entityType, 
            string username, 
            string password)
        {
            var entities = new List<EntityInfoDto>();

            try
            {
                switch (entityType.ToLower())
                {
                    case "device":
                        foreach (var deviceId in entityIds)
                        {
                            var device = await _sondaIMService.GetDeviceById(deviceId, username, password);
                            if (device != null)
                            {
                                entities.Add(new EntityInfoDto
                                {
                                    Id = device.Id,
                                    Name = device.Name ?? $"Device {device.Id}",
                                    Type = "device",
                                    AdditionalProperties = new Dictionary<string, object>
                                    {
                                        { "sourceId", device.SourceId ?? 0 },
                                        { "isActive", device.IsActive },
                                        { "latitude", device.Latitude ?? 0.0 },
                                        { "longitude", device.Longitude ?? 0.0 }
                                    }
                                });
                            }
                        }
                        break;

                    case "source":
                        foreach (var sourceId in entityIds)
                        {
                            var source = await _sondaIMService.GetSourceById(sourceId, username, password);
                            if (source != null)
                            {
                                entities.Add(new EntityInfoDto
                                {
                                    Id = source.Id,
                                    Name = source.Name ?? $"Source {source.Id}",
                                    Type = "source",
                                    AdditionalProperties = new Dictionary<string, object>
                                    {
                                        { "type", source.Type },
                                        { "isActive", source.IsActive },
                                        { "description", source.Description ?? "" }
                                    }
                                });
                            }
                        }
                        break;

                    case "group":
                        foreach (var groupId in entityIds)
                        {
                            var group = await _sondaIMService.GetDeviceGroupById(groupId, username, password);
                            if (group != null)
                            {
                                entities.Add(new EntityInfoDto
                                {
                                    Id = group.Id,
                                    Name = group.Name ?? $"Group {group.Id}",
                                    Type = "group",
                                    AdditionalProperties = new Dictionary<string, object>
                                    {
                                        { "description", group.Description ?? "" },
                                        { "isActive", group.IsActive }
                                    }
                                });
                            }
                        }
                        break;

                    case "sensor":
                        // TODO: Implementar cuando tengamos servicio de sensores
                        foreach (var sensorId in entityIds)
                        {
                            entities.Add(new EntityInfoDto
                            {
                                Id = sensorId,
                                Name = $"Sensor {sensorId}",
                                Type = "sensor",
                                AdditionalProperties = new Dictionary<string, object>()
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información de entidades en módulo IM");
            }

            return entities;
        }
    }
}
