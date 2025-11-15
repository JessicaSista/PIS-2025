using System.Linq.Dynamic.Core.Tokenizer;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using OmniMonitor.Server.Attributes;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SondaMainController : ControllerBase
    {
        private readonly ISondaIMService _sondaIMApiService;
        private readonly ISondaUMService _sondaUMApiService;
        private readonly ISondaAuthService _sondaAuthService;

        public SondaMainController(ISondaIMService sondaIMApiService, ISondaUMService sondaUMApiService, ISondaAuthService sondaAuthService)
        {
            _sondaIMApiService = sondaIMApiService;
            _sondaUMApiService = sondaUMApiService;
            _sondaAuthService = sondaAuthService;
        }
        [HttpGet("devices")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(List<Device>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Device>>> GetSondaDevices()
        {
            try
            {
                var username = User.Identity?.Name;
                List<Device>? devices = await _sondaIMApiService.GetAllDevices(username);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("devices/{id}")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(Device), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Device>> GetSondaDeviceById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                Device? device = await _sondaIMApiService.GetDeviceById(id, username);
                if (device == null)
                {
                    return NotFound($"No se encontró el dispositivo {id}");
                }

                return Ok(device);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("data")]
        [RequirePermission("Sensors.View")]
        [ProducesResponseType(typeof(List<DeviceData>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DeviceData>>> GetDeviceData(int deviceId, DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var username = User.Identity?.Name;
                List<DeviceData>? deviceData = await _sondaIMApiService.GetDeviceDataByDate(deviceId, dateFrom, dateTo, username);
                if (deviceData == null || deviceData.Count == 0)
                {
                    return NotFound($"No se encontraron datos para el dispositivo {deviceId} en el rango de fechas especificado.");
                }

                return Ok(deviceData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datos del device: {ex.Message}");
            }
        }

        [HttpGet("groups")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(List<DeviceGroup>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<DeviceGroup>>> GetAllDeviceGroups()
        {
            try
            {
                var username = User.Identity?.Name;
                List<DeviceGroup> groups = await _sondaIMApiService.GetAllDeviceGroups(username);
                return Ok(groups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("groups/{id}")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(DeviceGroup), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DeviceGroup>> GetDeviceGroupById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                DeviceGroup? group = await _sondaIMApiService.GetDeviceGroupById(id, username);
                if (group == null)
                {
                    return NotFound($"No se encontró el DeviceGroup {id}");
                }

                return Ok(group);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("devices/group/{id}")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(List<Device>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Device>>> GetDevicesOfGroup(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                List<Device>? devices = await _sondaIMApiService.GetDeviceOfGroup(id, username);
                if (devices == null || devices.Count == 0)
                {
                    return NotFound($"No se encontraron dispositivos para el grupo con ID {id}.");
                }

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los dispositivos del grupo: {ex.Message}");
            }
        }
        [HttpGet("sources")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(List<Source>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Source>>> GetAllSources()
        {
            try
            {
                var username = User.Identity?.Name;
                List<Source> sources = await _sondaIMApiService.GetAllSources(username);
                return Ok(sources);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("sources/{id}")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(Source), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Source>> GetSourceById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                Source? source = await _sondaIMApiService.GetSourceById(id, username);
                if (source == null)
                {
                    return NotFound($"No se encontró el Source {id}");
                }

                return Ok(source);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("devices/source/{id}")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(List<Device>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Device>>> GetDevicesOfSource(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                List<Device>? devices = await _sondaIMApiService.GetDeviceOfSource(id, username);
                if (devices == null || devices.Count == 0)
                {
                    return NotFound($"No se encontraron dispositivos para el source con ID {id}.");
                }

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los dispositivos del source: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los sensores únicos de los dispositivos seleccionados.
        /// </summary>
        [HttpPost("sensors/devices")]
        [RequirePermission("Sensors.View")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetSensorsByDevices([FromBody] List<int> deviceIds)
        {
            try
            {
                if (deviceIds == null || deviceIds.Count == 0)
                {
                    return Ok(new List<string>());
                }

                // Obtener información de todos los dispositivos seleccionados
                var allSensors = new List<string>();

                foreach (int deviceId in deviceIds)
                {
                    var username = User.Identity?.Name;
                    Device? device = await _sondaIMApiService.GetDeviceById(deviceId, username);
                    if (device != null && device.Sensors != null && device.Sensors.Count != 0)
                    {
                        var sensorNames = device.Sensors
                            .Where(s => !string.IsNullOrEmpty(s.Name))
                            .Select(s => s.Name!)
                            .ToList();
                        allSensors.AddRange(sensorNames);
                    }
                }

                // Obtener sensores únicos ordenados
                var uniqueSensors = allSensors
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                if (uniqueSensors.Count == 0)
                {
                    return NotFound($"No se encontraron sensores en los dispositivos seleccionados.");
                }

                return Ok(uniqueSensors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los sensores de los dispositivos: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene todos los sensores únicos de los dispositivos pertenecientes a una fuente específica.
        /// </summary>
        [HttpGet("sensors/source/{sourceId}")]
        [RequirePermission("Sensors.View")]
        [ProducesResponseType(typeof(List<string>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<string>>> GetSensorsBySource(int sourceId)
        {
            try
            {
                var username = User.Identity?.Name;

                // Obtener todos los dispositivos de la fuente
                List<Device>? devices = await _sondaIMApiService.GetDeviceOfSource(sourceId, username);

                if (devices == null || devices.Count == 0)
                {
                    return NotFound($"No se encontraron dispositivos para la fuente {sourceId}.");
                }

                // Extraer todos los sensores únicos de los dispositivos
                var uniqueSensors = devices
                    .Where(d => d.Sensors != null && d.Sensors.Count != 0)
                    .SelectMany(d => d.Sensors!)
                    .Where(s => !string.IsNullOrEmpty(s.Name))
                    .Select(s => s.Name!)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                if (uniqueSensors.Count == 0)
                {
                    return NotFound($"No se encontraron sensores en los dispositivos de la fuente {sourceId}.");
                }

                return Ok(uniqueSensors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los sensores de la fuente: {ex.Message}");
            }
        }

        [HttpGet("sensors/data")]
        [RequirePermission("Sensors.View")]
        [ProducesResponseType(typeof(List<SensorData>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SensorData>>> GetSensorData(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo)
        {
            try
            {
                var username = User.Identity?.Name;
                List<SensorData>? sensorData = await _sondaIMApiService.GetSensorDataByDate(deviceId, sensorName, dateFrom, dateTo, username);
                if (sensorData == null || sensorData.Count == 0)
                {
                    return NotFound($"No se encontraron datos para el sensor '{sensorName}' del dispositivo {deviceId} en el rango de fechas especificado.");
                }

                return Ok(sensorData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datos del sensor: {ex.Message}");
            }
        }

        [AllowAnonymous]
        [HttpGet("sensors/dataSinToken")]
        [ProducesResponseType(typeof(List<SensorData>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SensorData>>> GetSensorDataSinToken(int deviceId, string sensorName, DateTime dateFrom, DateTime dateTo, string? username = null)
        {
            try
            {
                string effectiveUsername = string.IsNullOrWhiteSpace(username) ? "visitante" : username;
                List<SensorData>? sensorData = await _sondaIMApiService.GetSensorDataByDateSinToken(deviceId, sensorName, dateFrom, dateTo, effectiveUsername);
                if (sensorData == null || sensorData.Count == 0)
                {
                    return NotFound($"No se encontraron datos para el sensor '{sensorName}' del dispositivo {deviceId} en el rango de fechas especificado.");
                }

                return Ok(sensorData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los datos del sensor: {ex.Message}");
            }
        }

        [HttpGet("zones")]
        [RequirePermission("Zones.View")]
        [ProducesResponseType(typeof(List<Zone>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Zone>>> GetAllZones()
        {
            try
            {
                var username = User.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                    return Unauthorized("No se pudo obtener el usuario del token.");

                var zones = await _sondaUMApiService.GetAllZones(username);

                return Ok(zones);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("zones/{id}")]
        [RequirePermission("Zones.View")]
        [ProducesResponseType(typeof(Zone), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Zone>> GetZoneById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                Zone? zone = await _sondaUMApiService.GetZoneById(id, username);
                if (zone == null)
                {
                    return NotFound($"No se encontró la zona {id}");
                }

                return Ok(zone);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("newsUM")]
        [RequirePermission("Zones.View")]
        [ProducesResponseType(typeof(List<News>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<News>>> GetAllNews(
            [FromQuery] int startIndex = 1,
            [FromQuery] string? queryString = null,
            [FromQuery] string? sort = null,
            [FromQuery] int count = 10)
        {
            try
            {
                // Pasar los parámetros al servicio
                var username = User.Identity?.Name;
                List<News> news = await _sondaUMApiService.GetAllNews(username, startIndex, queryString, sort, count);
                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("newsUM/{id}")]
        [RequirePermission("Zones.View")]
        [ProducesResponseType(typeof(News), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<News>> GetNewsById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                News? newsItem = await _sondaUMApiService.GetNewsById(id, username);
                if (newsItem == null)
                {
                    return NotFound($"No se encontró la noticia {id}");
                }

                return Ok(newsItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("zones/{id}/news")]
        [RequirePermission("Zones.View")]
        [ProducesResponseType(typeof(List<News>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<News>>> GetNewsByZoneId(
            int id,
            [FromQuery] int startIndex = 1,
            [FromQuery] string? queryString = null,
            [FromQuery] string? sort = null,
            [FromQuery] int count = 10)
        {
            try
            {
                var username = User.Identity?.Name;
                List<News> news = await _sondaUMApiService.GetNewsByZoneId(id, username, startIndex, queryString, sort, count);
                if (news == null || news.Count == 0)
                {
                    return NotFound($"No se encontraron noticias para la zona {id}.");
                }

                return Ok(news);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener las noticias de la zona: {ex.Message}");
            }
        }

        [HttpGet("events")]
        [RequirePermission("Events.View")]
        [ProducesResponseType(typeof(List<Event>), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Event>>> GetAllEvents()
        {
            try
            {
                var username = User.Identity?.Name;
                List<Event> events = await _sondaUMApiService.GetAllEvents(username);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("events/{id}")]
        [RequirePermission("Events.View")]
        [ProducesResponseType(typeof(Event), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Event>> GetEventById(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                Event? eventItem = await _sondaUMApiService.GetEventById(id, username);
                if (eventItem == null)
                {
                    return NotFound($"No se encontró el evento {id}");
                }

                return Ok(eventItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("zones/{id}/events")]
        [RequirePermission("Events.View")]
        [ProducesResponseType(typeof(List<Event>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<Event>>> GetEventsByZoneId(int id)
        {
            try
            {
                var username = User.Identity?.Name;
                List<Event> events = await _sondaUMApiService.GetEventsByZoneId(id, username);
                if (events == null || events.Count == 0)
                {
                    return NotFound($"No se encontraron eventos para la zona {id}.");
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al obtener los eventos de la zona: {ex.Message}");
            }
        }

        [HttpGet("kpi/deviceCount")]
        [RequirePermission("Devices.View")]
        [ProducesResponseType(typeof(int), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<int>> GetDeviceCount()
        {
            try
            {
                var username = User.Identity?.Name;
                int count = await _sondaIMApiService.GetSSDeviceCount(username);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet("kpi/dataStatus")]
        [RequirePermission("Sensors.View")]
        [ProducesResponseType(typeof(DeviceDataStatusResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DeviceDataStatusResponse>> GetDataStatus()
        {
            try
            {
                var username = User.Identity?.Name;
                DeviceDataStatusResponse? count = await _sondaIMApiService.GetSSDataStatus(username);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
