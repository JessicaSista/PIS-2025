using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using OmniMonitor.Server.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatasetFilterController : ControllerBase
    {
    private readonly ApplicationDbContext _context;
    private readonly ISondaUMService _sondaUMService;
    private readonly ISondaAMService _sondaAMService;
    private readonly ISondaEMService _sondaEMService;
    private readonly ISondaIMService _sondaIMService;
    private readonly ISondaAuthService _sondaAuthService;
    private readonly ILogger<DatasetFilterController> _logger;


        public DatasetFilterController(ApplicationDbContext context, ISondaUMService sondaUMService, ISondaAMService sondaAMService, ISondaEMService sondaEMService, ISondaIMService sondaIMService, ISondaAuthService sondaAuthService, ILogger<DatasetFilterController> logger)
        {
            _context = context;
            _sondaUMService = sondaUMService;
            _sondaAMService = sondaAMService;
            _sondaEMService = sondaEMService;
            _sondaIMService = sondaIMService;
            _sondaAuthService = sondaAuthService;
            _logger = logger;
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Datasets.View")]
        [HttpGet("FiltrarPorModuloYEntidad")]
        public async Task<ActionResult<List<PropiedadEntidadDto>>> FiltrarPorModuloYEntidad(string modulo, int entidadId)
        {
            var resultado = new List<PropiedadEntidadDto>();

            switch (modulo)
            {
                case "UM":
                    switch (entidadId)
                    {
                        case 2: // Noticias
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Title", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DatePublished", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Summary", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EndDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Important", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Zone.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 1: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Date", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TypeId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ApprovalState", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ReviewedAt", Tipo = FilterValueType.Date });
                            break;
                    }
                    break;
                case "AM":
                    switch (entidadId)
                    {
                        case 2: // Asset
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExternalId", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Code", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LifeTimeToDate", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TypeDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BundleDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BrandDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StateDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ResponsibleDto.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Device.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ProviderDto.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 1: // EventTaskInstance
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventTaskDto.Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "StartDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Subject", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "FinalizedDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TakenBy.Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Critical", Tipo = FilterValueType.Boolean });
                            break;
                        case 3: // Stock
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Quantity", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Location", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Sku", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Minimum", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Bundle.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "BundleId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Supervisor.Name", Tipo = FilterValueType.String });
                            break;
                    }
                    break;
                case "EM":
                    switch (entidadId)
                    {
                        case 1: // Alertas
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceAddress", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertState", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertCategory.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "AlertData", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ModifiedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DeviceType", Tipo = FilterValueType.Number });
                            break;
                        case 2: // Eventos
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Origin", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DateTime", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreationDate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceType", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "State", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            break;
                        case 3: // Extensiones
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "ExtensionState", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TakenByUsername", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventDateTime", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "CreatedAt", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastModification", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "WorkZoneName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "EventName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Dangerous", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Address", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Categories.Name", Tipo = FilterValueType.Enum });
                            break;
                    }
                    break;
                case "IM":
                    switch (entidadId)
                    {
                        case 1: // Device
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LayerId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Latitude", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Longitude", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "XCoordinate", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "YCoordinate", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SourceId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Source.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "IsActive", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "SectorId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TenantId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Sensors.Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Groups.Name", Tipo = FilterValueType.Enum });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastDataReceived", Tipo = FilterValueType.Date });
                            break;
                        case 2: // Source
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Id", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Description", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TimeTolerance", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TimeRange", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Icon", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "IsActive", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "TenantId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "NoDataAlert", Tipo = FilterValueType.Boolean });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "NoDataSleepByDevice", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "NoDataInterval", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "OutputId", Tipo = FilterValueType.Number });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Devices.Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Sensors.Name", Tipo = FilterValueType.String });
                            break;
                        case 3: // Sensor
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Name", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "DisplayName", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Type", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "Integration", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastUpdate", Tipo = FilterValueType.Date });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastValue", Tipo = FilterValueType.String });
                            resultado.Add(new PropiedadEntidadDto { Nombre = "LastPersisted", Tipo = FilterValueType.Date });
                            break;
                    }
                    break;
            }

            return Ok(resultado);
        }

    
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [RequirePermission("Datasets.View")]
        [HttpGet("GetAtributoValores")]
        public async Task<ActionResult<List<string>>> GetAtributoValores(string modulo, int entidadId, string atributo)
        {
            
            // Get username from JWT
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest("Usuario no encontrado.");

            List<object> valores = new();
            IEnumerable<object> entidades = Enumerable.Empty<object>();
            bool moduloValido = true, entidadValida = true;

            switch (modulo)
            {
                case "UM":
                    if (entidadId == 2)
                        // Obtener todas las noticias (usar un count alto para obtener todas)
                        // La implementación usa startIndex y count, no page y pageSize
                        entidades = await _sondaUMService.GetAllNews(username, 1, null, null, 1000);
                    else if (entidadId == 1)
                        entidades = await _sondaUMService.GetAllEvents(username);
                    else
                        entidadValida = false;
                    break;
                case "AM":
                    if (entidadId == 2)
                        entidades = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                    else if (entidadId == 1)
                        entidades = await _sondaAMService.GetEventTaskInstances(
                            "1900-11-01,3030-11-06", // dates
                            null, // page
                            null, // queryString
                            null, // bundleId
                            null, // state
                            null, // sort
                            null, // taskTypeId
                            null, // groupId
                            null, // pageSize
                            false, // tasksAssignedToMe
                            false, // tasksPendingApproval
                            username // username
                        );
                    else if (entidadId == 3)
                        entidades = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                    else
                        entidadValida = false;
                    break;
                case "EM":
                    if (entidadId == 1)
                        entidades = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                    else if (entidadId == 2)
                        entidades = await _sondaEMService.GetEvents(null, null, null, null, username);
                    else if (entidadId == 3)
                        entidades = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                    else
                        entidadValida = false;
                    break;
                case "IM":
                    if (entidadId == 1) // Device
                        entidades = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                    else if (entidadId == 2) // Source
                        entidades = await _sondaIMService.GetAllSources(username) ?? new List<Source>();
                    else if (entidadId == 3) // Sensor (extraído de los devices)
                    {
                        var devices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                        var sensors = devices
                            .Where(d => d.Sensors != null)
                            .SelectMany(d => d.Sensors!)
                            .GroupBy(s => s.Name)
                            .Select(g => g.First())
                            .ToList();
                        entidades = sensors;
                    }
                    else
                        entidadValida = false;
                    break;
                default:
                    moduloValido = false;
                    break;
            }

            if (!moduloValido)
                return BadRequest("Módulo no definido");
            if (!entidadValida)
                return BadRequest("Entidad no definida para el módulo seleccionado");

            // Caso especial: Zone.Name para noticias - obtener todas las zonas directamente
            if (modulo == "UM" && entidadId == 2 && atributo == "Zone.Name")
            {
                var allZones = await _sondaUMService.GetAllZones(username);
                var zoneNames = allZones
                    .Where(z => z != null && !string.IsNullOrWhiteSpace(z.Name))
                    .Select(z => z.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                return Ok(zoneNames);
            }

            // Caso especial: Device.Name para Asset en AM - obtener todos los devices directamente desde IM
            // (Los devices en AM son los mismos que en IM, así que los obtenemos desde ahí)
            if (modulo == "AM" && entidadId == 2 && atributo == "Device.Name")
            {
                var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                var deviceNames = allDevices
                    .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Name))
                    .Select(d => d.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                return Ok(deviceNames);
            }

            // Caso especial: Bundle.Name para Stock en AM - obtener todos los bundles directamente
            if (modulo == "AM" && entidadId == 3 && atributo == "Bundle.Name")
            {
                var allBundles = await _sondaAMService.GetBundles(null, null, null, null, username);
                var bundleNames = allBundles
                    .Where(b => b != null && !string.IsNullOrWhiteSpace(b.Name))
                    .Select(b => b.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                return Ok(bundleNames);
            }

            // Caso especial: Devices.Name para Source en IM - obtener todos los devices directamente
            if (modulo == "IM" && entidadId == 2 && atributo == "Devices.Name")
            {
                var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                var deviceNames = allDevices
                    .Where(d => d != null && !string.IsNullOrWhiteSpace(d.Name))
                    .Select(d => d.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                return Ok(deviceNames);
            }

            // Caso especial: Sensors.Name para Device o Source en IM - obtener todos los sensores de todos los devices
            if (modulo == "IM" && (entidadId == 1 || entidadId == 2) && atributo == "Sensors.Name")
            {
                var allDevices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                var sensorNames = allDevices
                    .Where(d => d != null && d.Sensors != null)
                    .SelectMany(d => d.Sensors!)
                    .Where(s => s != null && !string.IsNullOrWhiteSpace(s.Name))
                    .Select(s => s.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                return Ok(sensorNames);
            }

            if (modulo == "EM" && entidadId == 2)
            {
                foreach (var entidad in entidades)
                {
                    var categoriasProp = entidad.GetType().GetProperty("Categories");
                    var categorias = categoriasProp?.GetValue(entidad) as IEnumerable<object>;
                    if (categorias != null)
                    {
                        foreach (var cat in categorias)
                        {
                            var id = cat.GetType().GetProperty("Id")?.GetValue(cat);
                            var name = cat.GetType().GetProperty("Name")?.GetValue(cat);
                        }
                    }
                    else
                    {
                    }
                }
            }

            var partes = atributo.Split('.');
            foreach (var entidad in entidades)
            {
                void ExtraerValores(object? actual, int parteIdx)
                {
                    if (actual == null || parteIdx >= partes.Length) return;
                    
                    System.Reflection.PropertyInfo? prop = null;
                    var tipoActual = actual.GetType();
                    var nombrePropiedad = partes[parteIdx];
                    
                    // Primero buscar por JsonPropertyName (esto es importante porque Zone tiene [JsonPropertyName("zone")])
                    var allProps = tipoActual.GetProperties(
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance);
                    
                    foreach (var p in allProps)
                    {
                        // Buscar por JsonPropertyName primero (case-insensitive)
                        var jsonAttr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                            .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                        
                        if (jsonAttr != null && jsonAttr.Name.Equals(nombrePropiedad, StringComparison.OrdinalIgnoreCase))
                        {
                            prop = p;
                            break;
                        }
                        
                        // También buscar por nombre de propiedad (case-insensitive)
                        if (p.Name.Equals(nombrePropiedad, StringComparison.OrdinalIgnoreCase))
                        {
                            prop = p;
                            break;
                        }
                    }
                    
                    // Si aún no se encuentra, intentar búsqueda exacta
                    if (prop == null)
                    {
                        prop = tipoActual.GetProperty(nombrePropiedad, 
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Instance);
                    }
                    
                    if (prop == null) return;
                    
                    var valor = prop.GetValue(actual);
                    
                    // Si el valor es null, no podemos continuar navegando
                    if (valor == null) 
                    {
                        // Si es la última parte, no hay nada que hacer
                        if (parteIdx == partes.Length - 1) return;
                        // Si no es la última parte, no podemos continuar
                        return;
                    }

                    // Verificar si es una colección (pero no string)
                    if (valor is System.Collections.IEnumerable enumerable && !(valor is string))
                    {
                        var coleccion = enumerable.Cast<object>();
                        
                        // Si es la última parte, agrego cada elemento
                        if (parteIdx == partes.Length - 1)
                        {
                            foreach (var item in coleccion)
                                if (item != null)
                                    valores.Add(item);
                        }
                        else
                        {
                            // Para cada elemento, sigo el path restante
                            foreach (var item in coleccion)
                                ExtraerValores(item, parteIdx + 1);
                        }
                    }
                    else
                    {
                        // Si es la última parte y no es colección, agrego el valor
                        if (parteIdx == partes.Length - 1)
                        {
                            // Agregar el valor (incluyendo strings vacíos por ahora, los filtraremos después)
                            if (valor != null)
                                valores.Add(valor);
                        }
                        else
                        {
                            // Continuar navegando el path para objetos anidados
                            // Esto maneja casos como Zone.Name donde Zone es un objeto
                            ExtraerValores(valor, parteIdx + 1);
                        }
                    }
                }
                ExtraerValores(entidad, 0);
            }

            // Convertir todos los valores a string y filtrar nulos/vacíos
            var valoresString = valores
                .Where(v => v != null)
                .Select(v => v.ToString() ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            return Ok(valoresString);
        }


    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [RequirePermission("Datasets.View")]
    [HttpPost("FiltrarDatos")]
    public async Task<ActionResult<List<object>>> FiltrarDatos([FromBody] OmniMonitor.Shared.Dtos.FiltrarDatosRequest request)
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Usuario no encontrado.");

        IEnumerable<object> entidades = Enumerable.Empty<object>();
        bool moduloValido = true, entidadValida = true;

        switch (request.Modulo)
        {
            case "UM":
                if (request.EntidadId == 2)
                    entidades = await _sondaUMService.GetAllNews(username);
                else if (request.EntidadId == 1)
                    entidades = await _sondaUMService.GetAllEvents(username);
                else
                    entidadValida = false;
                break;
            case "AM":
                if (request.EntidadId == 2)
                    entidades = await _sondaAMService.GetAssets(null, null, null, null, null, null, username);
                else if (request.EntidadId == 1)
                    entidades = await _sondaAMService.GetEventTaskInstances(
                        "1900-11-01,3030-11-06", null, null, null, null, null, null, null, null, false, false, username);
                else if (request.EntidadId == 3)
                    entidades = await _sondaAMService.GetAllStock(null, null, null, null, null, username);
                else
                    entidadValida = false;
                break;
            case "EM":
                if (request.EntidadId == 1)
                    entidades = await _sondaEMService.GetAlerts(null, null, null, null, null, null, null, null, null, username);
                else if (request.EntidadId == 2)
                    entidades = await _sondaEMService.GetEvents(null, null, null, null, username);
                else if (request.EntidadId == 3)
                    entidades = await _sondaEMService.GetExtensions(null, null, null, null, null, null, null, null, null, username);
                else
                    entidadValida = false;
                break;
            case "IM":
                if (request.EntidadId == 1) // Device
                    entidades = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                else if (request.EntidadId == 2) // Source
                {
                    var sources = await _sondaIMService.GetAllSources(username) ?? new List<Source>();
                    
                    // Si hay filtros que requieren Devices o Sensors, poblar esas propiedades
                    bool needsDevices = request.Filtros.Any(f => f.AttributeName.StartsWith("Devices.", StringComparison.OrdinalIgnoreCase));
                    bool needsSensors = request.Filtros.Any(f => f.AttributeName.StartsWith("Sensors.", StringComparison.OrdinalIgnoreCase));
                    
                    if (needsDevices || needsSensors)
                    {
                        // Poblar Devices para cada Source
                        foreach (var source in sources)
                        {
                            if (source != null)
                            {
                                var devices = await _sondaIMService.GetDeviceOfSource(source.Id, username) ?? new List<Device>();
                                source.Devices = devices;
                                
                                // Si también se necesitan Sensors, extraerlos de los devices
                                if (needsSensors && devices.Any())
                                {
                                    var sensors = devices
                                        .Where(d => d.Sensors != null)
                                        .SelectMany(d => d.Sensors!)
                                        .DistinctBy(s => s.Name)
                                        .ToList();
                                    source.Sensors = sensors;
                                }
                            }
                        }
                    }
                    
                    entidades = sources;
                }
                else if (request.EntidadId == 3) // Sensor (extraído de los devices)
                {
                    var devices = await _sondaIMService.GetAllDevices(username) ?? new List<Device>();
                    var sensors = devices
                        .Where(d => d.Sensors != null)
                        .SelectMany(d => d.Sensors!)
                        .GroupBy(s => s.Name)
                        .Select(g => g.First())
                        .ToList();
                    entidades = sensors;
                }
                else
                    entidadValida = false;
                break;
            default:
                moduloValido = false;
                break;
        }

        if (!moduloValido)
            return BadRequest("Módulo no definido");
        if (!entidadValida)
            return BadRequest("Entidad no definida para el módulo seleccionado");

        foreach (var filtro in request.Filtros)
        {
        }

        int idx = 0;
        foreach (var entidad in entidades)
        {
            foreach (var prop in entidad.GetType().GetProperties())
            {
                var val = prop.GetValue(entidad);
            }
            idx++;
        }

        // Filtrar usando ApiDataService.FilterObjects
        var filtrados = ApiDataService.StaticFilterObjects(entidades, request.Filtros);
        foreach (var obj in filtrados)
        {
        }
        return Ok(filtrados);
    }

}}
