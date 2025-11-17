using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

public interface IApiDataService
{
    Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand operand, string username);
    Task<IEnumerable<dynamic>> GetDataForOperandSinToken(JoinOperand operand);
    Task<List<object>> GetNotFormalDataForOperand(JoinOperand operand, string username);
    Task<List<object>> GetNotFormalDataForOperandSinToken(JoinOperand operand);
}

public class ApiDataService : IApiDataService
{
    private const string PublicUsername = "visitante";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApplicationDbContext _context;
    private readonly ISondaEMService _sondaEMService;
    private readonly ISondaIMService _sondaIMService;
    private readonly ISondaAMService _sondaAMService;
    private readonly ISondaUMService _sondaUMService;
    private readonly IDatasetService _datasetService;
    private readonly IDatasetUMService _datasetUMService;
    private readonly IDatasetEMService _datasetEMService;
    private readonly IDatasetAmService _datasetAMService;

    public ApiDataService(IHttpClientFactory httpClientFactory, ApplicationDbContext context, ISondaEMService sondaEMService,
        ISondaIMService sondaIMService, ISondaAMService sondaAMService, IDatasetService datasetService,
        IDatasetUMService datasetUMService, IDatasetEMService datasetEMService, ISondaUMService SondaUMService, IDatasetAmService datasetAMService)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _sondaEMService = sondaEMService;
        _sondaIMService = sondaIMService;
        _sondaAMService = sondaAMService;
        _sondaUMService = SondaUMService;
        _datasetAMService = datasetAMService;
        _datasetService = datasetService;
        _datasetEMService = datasetEMService;
        _datasetUMService = datasetUMService;
    }

    public async Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand operand, string username)
    {
        var client = _httpClientFactory.CreateClient();

        switch (operand.ModuleType)
        {
            case ModuleType.InsightMonitor:
                var datasetIM = await _datasetService.GetDatasetIMByIdAsync(operand.DatasetId, username);
                if (datasetIM == null) return Enumerable.Empty<dynamic>();

                switch (operand.EntityName)
                {
                    case EntityName.Device:
                        var resultingDevices = new List<Device>();

                        if (datasetIM?.DatasetDevices != null)
                        {
                            foreach (var datasetDevice in datasetIM.DatasetDevices)
                            {
                                var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, username);
                                if (device != null)
                                {
                                    resultingDevices.Add(device);
                                }
                            }
                        }
                        return resultingDevices;
                    case EntityName.Sensor:
                        var resultingSensors = new List<Sensor>();

                        if (datasetIM?.DatasetDevices != null)
                        {
                            foreach (var datasetDevice in datasetIM.DatasetDevices)
                            {
                                var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, username);
                                if (device != null)
                                {
                                    foreach (var sensor in device.Sensors)
                                    {
                                        if (sensor != null && sensor.Name == datasetIM.SensorName)
                                        {
                                            resultingSensors.Add(sensor);
                                        }
                                    }
                                }
                            }
                        }
                        return resultingSensors;

                    case EntityName.Source:
                        var source = await _sondaIMService.GetSourceById(datasetIM.Id_Source.Value, username);
                        
                        if (source != null)
                        {
                            return new List<dynamic> { source };
                        }
                        else
                        {
                            return new List<dynamic>();
                        }

                    case EntityName.Group:
                        var group = await _sondaIMService.GetDeviceGroupById(datasetIM.Id_Group.Value, username);
                        if (group != null)
                        {
                            return new List<dynamic> { group };
                        } 
                        else 
                        { 
                            return new List<dynamic>(); 
                        }
                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for Insight Monitor.");
                }
            case ModuleType.UrbanMonitor:
                var datasetUM = await _datasetUMService.GetDatasetUMByIdAsync(operand.DatasetId, username);
                if (datasetUM == null) return Enumerable.Empty<dynamic>();

                switch(operand.EntityName)
                {
                    case EntityName.New:
                        var resultingNews = new List<dynamic>();
                        if (datasetUM.DatasetNews != null)
                        {
                            foreach (var DatasetNew in datasetUM.DatasetNews)
                            {
                                var newDto = await _sondaUMService.GetNewsById(DatasetNew.Id_news, username);
                                if (newDto != null)
                                {
                                    resultingNews.Add(newDto);
                                }
                            }
                        }
                        return resultingNews;
                    case EntityName.EventUM:
                        var resultingEvents = new List<dynamic>();
                        if (datasetUM.DatasetEvents != null)
                        {
                            foreach (var DatasetEvent in datasetUM.DatasetEvents)
                            {
                                var EventDto = await _sondaUMService.GetEventById(DatasetEvent.Id_event, username);
                                if (EventDto != null)
                                {
                                    resultingEvents.Add(EventDto);
                                }
                            }
                        }
                        return resultingEvents;
                    case EntityName.Zone:
                        var zone = await _sondaUMService.GetZoneById(datasetUM.Id_Zone.Value, username);
                        if (zone != null)
                        {
                            return new List<dynamic> { zone };
                        }
                        else
                        {
                            return new List<dynamic>();
                        }

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for UrbanMonitor.");
                }

            case ModuleType.AssetManager:
                var datasetAM = await _datasetAMService.GetDatasetAMByIdAsync(operand.DatasetId, username);
                if (datasetAM == null) return Enumerable.Empty<dynamic>();

                switch(operand.EntityName)
                {
                    case EntityName.Asset:
                        var resultingAssets = new List<dynamic>();

                        foreach (var assetDataset in datasetAM.Grupo_Asset)
                        {
                            int datasetID = int.Parse(assetDataset.Id_Asset);
                            var asset = await _sondaAMService.GetAssetById(datasetID, username);
                            if (asset != null) { 
                                resultingAssets.Add(asset); 
                            }
                        }

                        return resultingAssets;

                    case EntityName.EventAM:
                        var resultingEvent = new List<dynamic>();
                        foreach (var eventDataset in datasetAM.Grupo_Event_Task_Instance)
                        {
                            var eventDto = await _sondaAMService.GetEventTaskInstanceById(eventDataset.Id_Event_Task_Instance, username);
                            if (eventDto != null)
                            {
                                resultingEvent.Add(eventDto);
                            }
                        }
                        return resultingEvent;

                    case EntityName.Stock:
                        var resultingStock = new List<dynamic>();
                        foreach (var stockDataset in datasetAM.Grupo_Stock)
                        {
                            if (stockDataset != null)
                            {
                                var stock = await _sondaAMService.GetStockById(stockDataset.Id_Stock, username);
                                if (stock != null)
                                {
                                    resultingStock.Add(stock);
                                }
                            }
                        }
                        return resultingStock;
                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for AssetManager.");
                }

            case ModuleType.EventManager:
                var datasetEM = await _datasetEMService.GetDatasetEMByIdAsync(operand.DatasetId, username);
                if (datasetEM == null) return Enumerable.Empty<dynamic>();

                switch (operand.EntityName)
                {
                    case EntityName.EventEM:
                        var resultingEvent = new List<dynamic>();
                        foreach (var datasetEvent in datasetEM.DatasetEvents)
                        {
                            var eventDto = await _sondaEMService.GetEventById(datasetEvent.Id_event, username);
                            if (eventDto != null)
                            {
                                resultingEvent.Add(eventDto);
                            }
                        }
                        return resultingEvent;

                    case EntityName.Alert:
                        var resultingAlert = new List<dynamic>();
                        foreach (var datasetAlert in datasetEM.DatasetAlerts)
                        {
                            var alertDto = await _sondaEMService.GetAlertById(datasetAlert.Id_alert, username);
                            if (alertDto != null)
                            {
                                resultingAlert.Add(alertDto);
                            }
                        }
                        return resultingAlert;

                    case EntityName.Extension:
                        var resultingExtension = new List<dynamic>();
                        foreach (var datasetExtension  in datasetEM.DatasetExtensions)
                        {
                            var extensionDto = await _sondaEMService.GetExtensionById(datasetExtension.Id_extension, username);
                            if (extensionDto != null)
                            {
                                resultingExtension.Add(extensionDto);
                            }
                        }
                        return resultingExtension;

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for EventManger.");
                }

            default:
                throw new NotSupportedException($"Module type '{operand.ModuleType}' is not supported.");
        }
    }

    public async Task<IEnumerable<dynamic>> GetDataForOperandSinToken(JoinOperand operand)
    {
        switch (operand.ModuleType)
        {
            case ModuleType.InsightMonitor:
                var datasetIMInfo = await _datasetService.GetDatasetIMByIdForEditAsyncSinToken(operand.DatasetId);
                if (datasetIMInfo == null)
                    return Enumerable.Empty<dynamic>();

                var imOwner = string.IsNullOrWhiteSpace(datasetIMInfo.Username)
                    ? PublicUsername
                    : datasetIMInfo.Username;

                var datasetIM = await _datasetService.GetDatasetIMByIdAsync(operand.DatasetId, imOwner);
                if (datasetIM == null)
                    return Enumerable.Empty<dynamic>();

                switch (operand.EntityName)
                {
                    case EntityName.Device:
                        if (datasetIM.DatasetDevices == null || !datasetIM.DatasetDevices.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicDevices = new List<dynamic>();
                        foreach (var datasetDevice in datasetIM.DatasetDevices)
                        {
                            var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, imOwner);
                            if (device != null)
                                publicDevices.Add(device);
                        }

                        return publicDevices;

                    case EntityName.Sensor:
                        if (datasetIM.DatasetDevices == null || !datasetIM.DatasetDevices.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicSensors = new List<dynamic>();
                        foreach (var datasetDevice in datasetIM.DatasetDevices)
                        {
                            var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, imOwner);
                            if (device?.Sensors == null)
                                continue;

                            foreach (var sensor in device.Sensors)
                            {
                                if (sensor != null && sensor.Name == datasetIM.SensorName)
                                    publicSensors.Add(sensor);
                            }
                        }

                        return publicSensors;

                    case EntityName.Source:
                        if (!datasetIM.Id_Source.HasValue)
                            return Enumerable.Empty<dynamic>();

                        var source = await _sondaIMService.GetSourceById(datasetIM.Id_Source.Value, imOwner);
                        return source != null
                            ? new List<dynamic> { source }
                            : Enumerable.Empty<dynamic>();

                    case EntityName.Group:
                        if (!datasetIM.Id_Group.HasValue)
                            return Enumerable.Empty<dynamic>();

                        var group = await _sondaIMService.GetDeviceGroupById(datasetIM.Id_Group.Value, imOwner);
                        return group != null
                            ? new List<dynamic> { group }
                            : Enumerable.Empty<dynamic>();

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for Insight Monitor.");
                }

            case ModuleType.UrbanMonitor:
                var datasetUM = await _datasetUMService.GetDatasetUMByIdAsyncSinToken(operand.DatasetId);
                if (datasetUM == null)
                    return Enumerable.Empty<dynamic>();

                var umOwner = string.IsNullOrWhiteSpace(datasetUM.Username)
                    ? PublicUsername
                    : datasetUM.Username;

                switch (operand.EntityName)
                {
                    case EntityName.New:
                        if (datasetUM.DatasetNews == null || !datasetUM.DatasetNews.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicNews = new List<dynamic>();
                        foreach (var datasetNew in datasetUM.DatasetNews)
                        {
                            var news = await _sondaUMService.GetNewsById(datasetNew.Id_news, umOwner);
                            if (news != null)
                                publicNews.Add(news);
                        }

                        return publicNews;

                    case EntityName.EventUM:
                        if (datasetUM.DatasetEvents == null || !datasetUM.DatasetEvents.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicEvents = new List<dynamic>();
                        foreach (var datasetEvent in datasetUM.DatasetEvents)
                        {
                            var eventDto = await _sondaUMService.GetEventById(datasetEvent.Id_event, umOwner);
                            if (eventDto != null)
                                publicEvents.Add(eventDto);
                        }

                        return publicEvents;

                    case EntityName.Zone:
                        if (!datasetUM.Id_Zone.HasValue)
                            return Enumerable.Empty<dynamic>();

                        var zone = await _sondaUMService.GetZoneById(datasetUM.Id_Zone.Value, umOwner);
                        return zone != null
                            ? new List<dynamic> { zone }
                            : Enumerable.Empty<dynamic>();

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for UrbanMonitor.");
                }

            case ModuleType.AssetManager:
                var datasetAM = await _datasetAMService.GetDatasetAMByIdAsyncSinToken(operand.DatasetId);
                if (datasetAM == null)
                    return Enumerable.Empty<dynamic>();

                var amOwner = string.IsNullOrWhiteSpace(datasetAM.Username)
                    ? PublicUsername
                    : datasetAM.Username;

                switch (operand.EntityName)
                {
                    case EntityName.Asset:
                        if (datasetAM.Grupo_Asset == null || !datasetAM.Grupo_Asset.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicAssets = new List<dynamic>();
                        foreach (var assetDataset in datasetAM.Grupo_Asset)
                        {
                            if (!int.TryParse(assetDataset.Id_Asset, out var assetId))
                                continue;

                            var asset = await _sondaAMService.GetAssetById(assetId, amOwner);
                            if (asset != null)
                                publicAssets.Add(asset);
                        }

                        return publicAssets;

                    case EntityName.EventAM:
                        if (datasetAM.Grupo_Event_Task_Instance == null || !datasetAM.Grupo_Event_Task_Instance.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicEventTasks = new List<dynamic>();
                        foreach (var eventDataset in datasetAM.Grupo_Event_Task_Instance)
                        {
                            var eventTask = await _sondaAMService.GetEventTaskInstanceById(eventDataset.Id_Event_Task_Instance, amOwner);
                            if (eventTask != null)
                                publicEventTasks.Add(eventTask);
                        }

                        return publicEventTasks;

                    case EntityName.Stock:
                        if (datasetAM.Grupo_Event_Task_Instance == null || !datasetAM.Grupo_Event_Task_Instance.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicStocks = new List<dynamic>();
                        foreach (var eventDataset in datasetAM.Grupo_Event_Task_Instance)
                        {
                            if (eventDataset?.Grupo_Stock == null)
                                continue;

                            foreach (var stockDataset in eventDataset.Grupo_Stock)
                            {
                                var stock = await _sondaAMService.GetStockById(stockDataset.Id_Stock, amOwner);
                                if (stock != null)
                                    publicStocks.Add(stock);
                            }
                        }

                        return publicStocks;

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for AssetManager.");
                }

            case ModuleType.EventManager:
                var datasetEM = await _datasetEMService.GetDatasetEMByIdAsyncSinToken(operand.DatasetId);
                if (datasetEM == null)
                    return Enumerable.Empty<dynamic>();

                var emOwner = string.IsNullOrWhiteSpace(datasetEM.Username)
                    ? PublicUsername
                    : datasetEM.Username;

                switch (operand.EntityName)
                {
                    case EntityName.EventEM:
                        if (datasetEM.DatasetEvents == null || !datasetEM.DatasetEvents.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicEmEvents = new List<dynamic>();
                        foreach (var datasetEvent in datasetEM.DatasetEvents)
                        {
                            var eventDto = await _sondaEMService.GetEventById(datasetEvent.Id_event, emOwner);
                            if (eventDto != null)
                                publicEmEvents.Add(eventDto);
                        }

                        return publicEmEvents;

                    case EntityName.Alert:
                        if (datasetEM.DatasetAlerts == null || !datasetEM.DatasetAlerts.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicAlerts = new List<dynamic>();
                        foreach (var datasetAlert in datasetEM.DatasetAlerts)
                        {
                            var alertDto = await _sondaEMService.GetAlertById(datasetAlert.Id_alert, emOwner);
                            if (alertDto != null)
                                publicAlerts.Add(alertDto);
                        }

                        return publicAlerts;

                    case EntityName.Extension:
                        if (datasetEM.DatasetExtensions == null || !datasetEM.DatasetExtensions.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicExtensions = new List<dynamic>();
                        foreach (var datasetExtension in datasetEM.DatasetExtensions)
                        {
                            var extensionDto = await _sondaEMService.GetExtensionById(datasetExtension.Id_extension, emOwner);
                            if (extensionDto != null)
                                publicExtensions.Add(extensionDto);
                        }

                        return publicExtensions;

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for EventManger.");
                }

            default:
                throw new NotSupportedException($"Module type '{operand.ModuleType}' is not supported.");
        }
    }

    

    /// <summary>
    /// Filtra una lista de objetos dinámicos según una lista de condiciones de filtro.
    /// </summary>
    public List<dynamic> FilterObjects(IEnumerable<dynamic> objects, List<FilterCondition> filters)
    {
        var filtered = new List<dynamic>();
        foreach (var obj in objects)
        {
            bool matchesAll = true;
            var dict = ObjectToDictionary(obj);
            foreach (var filter in filters)
            {
                object value;
                if (!dict.TryGetValue(filter.AttributeName, out value))
                {
                    matchesAll = false;
                    break;
                }
                if (!MatchesFilter(value, filter))
                {
                    matchesAll = false;
                    break;
                }
            }
            if (matchesAll)
                filtered.Add(obj);
        }
        return filtered;
    }

    private bool MatchesFilter(object value, FilterCondition filter)
    {
        if (value == null) return false;
        switch (filter.ValueType)
        {
            case FilterValueType.Date:
                if (filter.Type == FilterType.Between && filter.Condition is IEnumerable<object> range && range.Count() == 2)
                {
                    DateTime dateVal;
                    if (value is DateTime dt)
                        dateVal = dt;
                    else
                    {
                        var dateStr = value?.ToString();
                        if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateVal))
                            return false;
                    }
                    
                    DateTime start;
                    if (range.ElementAt(0) is DateTime d1)
                        start = d1;
                    else
                    {
                        var startStr = range.ElementAt(0)?.ToString();
                        if (string.IsNullOrWhiteSpace(startStr) || !DateTime.TryParse(startStr, out start))
                            return false;
                    }
                    
                    DateTime end;
                    if (range.ElementAt(1) is DateTime d2)
                        end = d2;
                    else
                    {
                        var endStr = range.ElementAt(1)?.ToString();
                        if (string.IsNullOrWhiteSpace(endStr) || !DateTime.TryParse(endStr, out end))
                            return false;
                    }
                    
                    return dateVal >= start && dateVal <= end;
                }
                return false;
            case FilterValueType.Number:
                switch (filter.Type)
                {
                    case FilterType.Equals:
                        return value.Equals(filter.Condition);
                    case FilterType.NotEquals:
                        return !value.Equals(filter.Condition);
                    case FilterType.GreaterThan:
                        if (value is IComparable comp1 && filter.Condition is IComparable comp2)
                            return comp1.CompareTo(comp2) > 0;
                        return false;
                    case FilterType.LessThan:
                        if (value is IComparable comp3 && filter.Condition is IComparable comp4)
                            return comp3.CompareTo(comp4) < 0;
                        return false;
                    default:
                        return false;
                }
            case FilterValueType.String:
                switch (filter.Type)
                {
                    case FilterType.Equals:
                        return value.Equals(filter.Condition);
                    case FilterType.NotEquals:
                        return !value.Equals(filter.Condition);
                    case FilterType.Contains:
                        return value is string s && filter.Condition is string cond && s.Contains(cond);
                    default:
                        return false;
                }
            case FilterValueType.Enum:
                if (filter.Type == FilterType.In && filter.Condition is IEnumerable<object> list)
                    return list.Contains(value);
                return false;

            case FilterValueType.Boolean:
                if (filter.Type == FilterType.Equals)
                    return value.Equals(filter.Condition);
                if (filter.Type == FilterType.NotEquals)
                    return !value.Equals(filter.Condition);
                return false;
            default:
                return false;
        }
    }

    private IDictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (obj is IDictionary<string, object> dict)
        {
            return new Dictionary<string, object>(dict, StringComparer.OrdinalIgnoreCase);
        }
        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            dictionary[property.Name] = property.GetValue(obj) ?? default!;
        }
        return dictionary;
    }

        /// <summary>
    /// Método estático para filtrar objetos sin instanciar dependencias.
    /// </summary>
    public static List<dynamic> StaticFilterObjects(IEnumerable<dynamic> objects, List<FilterCondition> filters)
    {
        var filtered = new List<dynamic>();
        foreach (var obj in objects)
        {
            bool matchesAll = true;
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (obj != null)
            {
                if (obj is IDictionary<string, object> d)
                    dict = new Dictionary<string, object>(d, StringComparer.OrdinalIgnoreCase);
                else
                {
                    foreach (var property in obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                    {
                        dict[property.Name] = property.GetValue(obj) ?? default!;
                    }
                }
            }
            foreach (var filter in filters)
            {
                object value = null;
                var partes = filter.AttributeName.Split('.');
                object actual = dict;
                for (int i = 0; i < partes.Length; i++)
                {
                    if (actual == null) break;
                    if (actual is IDictionary<string, object> d)
                    {
                        d.TryGetValue(partes[i], out actual);
                    }
                    else
                    {
                        // Buscar por JsonPropertyName primero, luego por nombre de propiedad
                        var tipoActual = actual.GetType();
                        System.Reflection.PropertyInfo? prop = null;
                        
                        // Buscar por JsonPropertyName (case-insensitive)
                        var allProps = tipoActual.GetProperties(
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Instance);
                        
                        foreach (var p in allProps)
                        {
                            var jsonAttr = p.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                                .FirstOrDefault() as System.Text.Json.Serialization.JsonPropertyNameAttribute;
                            
                            if (jsonAttr != null && jsonAttr.Name.Equals(partes[i], StringComparison.OrdinalIgnoreCase))
                            {
                                prop = p;
                                break;
                            }
                            
                            if (p.Name.Equals(partes[i], StringComparison.OrdinalIgnoreCase))
                            {
                                prop = p;
                                break;
                            }
                        }
                        
                        // Si aún no se encuentra, intentar búsqueda exacta
                        if (prop == null)
                        {
                            prop = tipoActual.GetProperty(partes[i], 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance);
                        }
                        
                        actual = prop?.GetValue(actual);
                    }
                    
                    // Manejo especial para colecciones en enums (como Categories.Name)
                    if (filter.ValueType == FilterValueType.Enum && actual != null && i < partes.Length - 1 && 
                        actual is System.Collections.IEnumerable enumerable && !(actual is string))
                    {
                        var remainingPath = string.Join(".", partes.Skip(i + 1));
                        var collectionValues = new List<object>();
                        
                        foreach (var item in enumerable)
                        {
                            if (item == null) continue;
                            
                            var itemValue = item;
                            var remainingParts = partes.Skip(i + 1).ToArray();
                            
                            // Navegar el path restante en cada item de la colección
                            for (int j = 0; j < remainingParts.Length; j++)
                            {
                                if (itemValue == null) break;
                                var itemProp = itemValue.GetType().GetProperty(remainingParts[j]);
                                itemValue = itemProp?.GetValue(itemValue);
                            }
                            
                            if (itemValue != null)
                                collectionValues.Add(itemValue);
                        }
                        
                        actual = collectionValues;
                        break; // Salir del loop principal porque ya procesamos el path completo
                    }
                }
                value = actual;
                if (value == null)
                {
                    matchesAll = false;
                    break;
                }
                
                // Validación adicional para fechas: si es DateTime? null o DateTime default, no procesar
                if (filter.ValueType == FilterValueType.Date)
                {
                    var valueType = value.GetType();
                    if (valueType == typeof(DateTime))
                    {
                        var dt = (DateTime)value;
                        if (dt == default(DateTime))
                        {
                            matchesAll = false;
                            break;
                        }
                    }
                    else if (valueType == typeof(DateTime?) || (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(Nullable<>) && valueType.GetGenericArguments()[0] == typeof(DateTime)))
                    {
                        var dtn = (DateTime?)value;
                        if (!dtn.HasValue)
                        {
                            matchesAll = false;
                            break;
                        }
                    }
                }
                
                if (!MatchesFilterStatic(value, filter))
                {
                    matchesAll = false;
                    break;
                }
            }
            if (matchesAll)
                filtered.Add(obj);
        }
        return filtered;
    }

    public async Task<List<object>> GetNotFormalDataForOperand(JoinOperand operand, string username)
    {
        var result = new List<object>();
        
        if (operand.ModuleType != ModuleType.InsightMonitor)
            return result;

        // Buscar el dataset no formal
        var datasetIM = await _context.Set<DatasetIM>()
            .Include(d => d.DatasetDevices)
            .Include(d => d.DatasetSources)
            .Include(d => d.DatasetSensors)
            .FirstOrDefaultAsync(d => d.Id == operand.DatasetId);

        if (datasetIM == null || datasetIM.Is_Dataset != "N")
            return result;

        switch (operand.EntityName)
        {
            case EntityName.Device:
                if (datasetIM.DatasetDevices != null && datasetIM.DatasetDevices.Any())
                {
                    foreach (var deviceRef in datasetIM.DatasetDevices)
                    {
                        var device = await _sondaIMService.GetDeviceById(deviceRef.Id_device, username);
                        if (device != null)
                        {
                            result.Add(device);
                        }
                    }
                }
                break;

            case EntityName.Source:
                if (datasetIM.DatasetSources != null && datasetIM.DatasetSources.Any())
                {
                    foreach (var sourceRef in datasetIM.DatasetSources)
                    {
                        var source = await _sondaIMService.GetSourceById(sourceRef.Id_source, username);
                        if (source != null)
                        {
                            result.Add(source);
                        }
                    }
                }
                break;

            case EntityName.Sensor:
                if (datasetIM.DatasetSensors != null && datasetIM.DatasetSensors.Any())
                {
                    var sensorNames = datasetIM.DatasetSensors.Select(s => s.SensorName).ToHashSet();
                    
                    // Buscar sensores en los devices del dataset
                    if (datasetIM.DatasetDevices != null && datasetIM.DatasetDevices.Any())
                    {
                        foreach (var deviceRef in datasetIM.DatasetDevices)
                        {
                            var device = await _sondaIMService.GetDeviceById(deviceRef.Id_device, username);
                            if (device?.Sensors != null)
                            {
                                foreach (var sensor in device.Sensors)
                                {
                                    if (sensorNames.Contains(sensor.Name))
                                    {
                                        result.Add(sensor);
                                    }
                                }
                            }
                        }
                    }
                }
                break;
        }

        return result;
    }

    public async Task<List<object>> GetNotFormalDataForOperandSinToken(JoinOperand operand)
    {
        var result = new List<object>();
        
        if (operand.ModuleType != ModuleType.InsightMonitor)
            return result;

        // Buscar el dataset no formal
        var datasetIM = await _context.Set<DatasetIM>()
            .Include(d => d.DatasetDevices)
            .Include(d => d.DatasetSources)
            .Include(d => d.DatasetSensors)
            .FirstOrDefaultAsync(d => d.Id == operand.DatasetId);

        if (datasetIM == null || datasetIM.Is_Dataset != "N")
            return result;

        switch (operand.EntityName)
        {
            case EntityName.Device:
                if (datasetIM.DatasetDevices != null && datasetIM.DatasetDevices.Any())
                {
                    foreach (var deviceRef in datasetIM.DatasetDevices)
                    {
                        var device = await _sondaIMService.GetDeviceById(deviceRef.Id_device, PublicUsername);
                        if (device != null)
                        {
                            result.Add(device);
                        }
                    }
                }
                break;

            case EntityName.Source:
                if (datasetIM.DatasetSources != null && datasetIM.DatasetSources.Any())
                {
                    foreach (var sourceRef in datasetIM.DatasetSources)
                    {
                        var source = await _sondaIMService.GetSourceById(sourceRef.Id_source, PublicUsername);
                        if (source != null)
                        {
                            result.Add(source);
                        }
                    }
                }
                break;

            case EntityName.Sensor:
                if (datasetIM.DatasetSensors != null && datasetIM.DatasetSensors.Any())
                {
                    var sensorNames = datasetIM.DatasetSensors.Select(s => s.SensorName).ToHashSet();
                    
                    // Buscar sensores en los devices del dataset
                    if (datasetIM.DatasetDevices != null && datasetIM.DatasetDevices.Any())
                    {
                        foreach (var deviceRef in datasetIM.DatasetDevices)
                        {
                            var device = await _sondaIMService.GetDeviceById(deviceRef.Id_device, PublicUsername);
                            if (device?.Sensors != null)
                            {
                                foreach (var sensor in device.Sensors)
                                {
                                    if (sensorNames.Contains(sensor.Name))
                                    {
                                        result.Add(sensor);
                                    }
                                }
                            }
                        }
                    }
                }
                break;
        }

        return result;
    }

    private static bool MatchesFilterStatic(object value, FilterCondition filter)
    {
        if (value == null) return false;
        
        // Si el valor es DateTime y es default (MinValue), tratarlo como null para filtros de fecha
        if (filter.ValueType == FilterValueType.Date)
        {
            if (value is DateTime dt)
            {
                if (dt == default(DateTime))
                    return false;
            }
            else
            {
                // Verificar si es un tipo nullable de DateTime
                var type = value.GetType();
                if (type == typeof(DateTime?) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0] == typeof(DateTime)))
                {
                    var nullableValue = (DateTime?)value;
                    if (!nullableValue.HasValue)
                        return false;
                }
            }
        }
        
        switch (filter.ValueType)
        {
            case FilterValueType.Date:
                DateTime condDate;
                if (filter.Type == FilterType.Between)
                {
                    DateTime start, end;
                    if (filter.Condition is System.Text.Json.JsonElement jeArr && jeArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var arr = jeArr.EnumerateArray().ToArray();
                        if (arr.Length < 2)
                            return false;
                        
                        // Intentar parsear como DateTime directamente
                        if (arr[0].ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var startStr = arr[0].GetString();
                            if (string.IsNullOrWhiteSpace(startStr) || !DateTime.TryParse(startStr, out start))
                                return false;
                        }
                        else
                        {
                            // Intentar deserializar como DateTime
                            try
                            {
                                start = System.Text.Json.JsonSerializer.Deserialize<DateTime>(arr[0].GetRawText());
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        
                        if (arr[1].ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var endStr = arr[1].GetString();
                            if (string.IsNullOrWhiteSpace(endStr) || !DateTime.TryParse(endStr, out end))
                                return false;
                        }
                        else
                        {
                            try
                            {
                                end = System.Text.Json.JsonSerializer.Deserialize<DateTime>(arr[1].GetRawText());
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }
                    else if (filter.Condition is DateTime[] dateArray && dateArray.Length == 2)
                    {
                        start = dateArray[0];
                        end = dateArray[1];
                    }
                    else if (filter.Condition is IEnumerable<object> range && range.Count() == 2)
                    {
                        var first = range.ElementAt(0);
                        var second = range.ElementAt(1);
                        
                        if (first is DateTime d1)
                            start = d1;
                        else if (first is System.Text.Json.JsonElement je1)
                        {
                            if (je1.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var startStr = je1.GetString();
                                if (string.IsNullOrWhiteSpace(startStr) || !DateTime.TryParse(startStr, out start))
                                    return false;
                            }
                            else
                            {
                                try
                                {
                                    start = System.Text.Json.JsonSerializer.Deserialize<DateTime>(je1.GetRawText());
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        }
                        else if (!DateTime.TryParse(first?.ToString(), out start))
                            return false;
                        
                        if (second is DateTime d2)
                            end = d2;
                        else if (second is System.Text.Json.JsonElement je2)
                        {
                            if (je2.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                var endStr = je2.GetString();
                                if (string.IsNullOrWhiteSpace(endStr) || !DateTime.TryParse(endStr, out end))
                                    return false;
                            }
                            else
                            {
                                try
                                {
                                    end = System.Text.Json.JsonSerializer.Deserialize<DateTime>(je2.GetRawText());
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        }
                        else if (!DateTime.TryParse(second?.ToString(), out end))
                            return false;
                    }
                    else
                    {
                        var arr = filter.Condition as object[];
                        if (arr == null || arr.Length < 2)
                            return false;
                        
                        var startStr = arr[0]?.ToString();
                        var endStr = arr[1]?.ToString();
                        if (string.IsNullOrWhiteSpace(startStr) || string.IsNullOrWhiteSpace(endStr))
                            return false;
                        if (!DateTime.TryParse(startStr, out start) || !DateTime.TryParse(endStr, out end))
                            return false;
                    }
                    
                    // Manejar el valor de fecha que puede venir como DateTime, string, o JsonElement
                    DateTime dateVal;
                    if (value is DateTime dtv)
                    {
                        dateVal = dtv;
                    }
                    else if (value is System.Text.Json.JsonElement jeValue)
                    {
                        if (jeValue.ValueKind == System.Text.Json.JsonValueKind.String)
                        {
                            var dateStr = jeValue.GetString();
                            if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateVal))
                                return false;
                        }
                        else
                        {
                            var dateStr = jeValue.GetRawText().Trim('"');
                            if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateVal))
                                return false;
                        }
                    }
                    else
                    {
                        var dateStr = value?.ToString();
                        if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateVal))
                            return false;
                    }
                    
                    return dateVal >= start && dateVal <= end;
                }
                
                // Parsear la condición de fecha
                if (filter.Condition is System.Text.Json.JsonElement jeDate && jeDate.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var condDateStr = jeDate.GetString();
                    if (string.IsNullOrWhiteSpace(condDateStr) || !DateTime.TryParse(condDateStr, out condDate))
                        return false;
                }
                else if (filter.Condition is DateTime dt)
                {
                    condDate = dt;
                }
                else
                {
                    var condDateStr2 = filter.Condition?.ToString();
                    if (string.IsNullOrWhiteSpace(condDateStr2) || !DateTime.TryParse(condDateStr2, out condDate))
                        return false;
                }
                    
                // Manejar el valor de fecha que puede venir como DateTime, string, o JsonElement
                DateTime dateValue;
                if (value is DateTime dv)
                {
                    dateValue = dv;
                }
                else if (value is System.Text.Json.JsonElement jeValueDate)
                {
                    if (jeValueDate.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var dateStr = jeValueDate.GetString();
                        if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateValue))
                            return false;
                    }
                    else
                    {
                        var dateStr = jeValueDate.GetRawText().Trim('"');
                        if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateValue))
                            return false;
                    }
                }
                else
                {
                    var dateStr = value?.ToString();
                    if (string.IsNullOrWhiteSpace(dateStr) || !DateTime.TryParse(dateStr, out dateValue))
                        return false;
                }
                
                if (filter.Type == FilterType.Equals)
                    return dateValue == condDate;
                return false;
            case FilterValueType.Number:
                double condNum;
                if (filter.Condition is System.Text.Json.JsonElement jeNum && jeNum.ValueKind == System.Text.Json.JsonValueKind.Number)
                    condNum = jeNum.GetDouble();
                else if (double.TryParse(filter.Condition?.ToString(), out var num))
                    condNum = num;
                else
                    condNum = 0;
                double valueNum = value is int vi ? vi : value is double vd ? vd : double.TryParse(value?.ToString(), out var vn) ? vn : 0;
                switch (filter.Type)
                {
                    case FilterType.Equals:
                        return valueNum == condNum;
                    case FilterType.NotEquals:
                        return valueNum != condNum;
                    case FilterType.GreaterThan:
                        return valueNum > condNum;
                    case FilterType.LessThan:
                        return valueNum < condNum;
                    default:
                        return false;
                }
            case FilterValueType.String:
                string condStr = "";
                if (filter.Condition is string str)
                {
                    condStr = str;
                }
                else if (filter.Condition is System.Text.Json.JsonElement je)
                {
                    // Intentar obtener como string primero
                    if (je.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        condStr = je.GetString() ?? "";
                    }
                    else if (je.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        // Si es un número, convertirlo a string
                        condStr = je.GetRawText(); // Obtiene el valor como string sin comillas
                    }
                    else
                    {
                        // Si no es string ni número, convertir a string
                        condStr = je.GetRawText();
                    }
                }
                else
                {
                    condStr = filter.Condition?.ToString() ?? "";
                }
                
                // Convertir el valor a string para comparar
                string valueStr = value?.ToString() ?? "";
                
                switch (filter.Type)
                {
                    case FilterType.Equals:
                        return valueStr == condStr;
                    case FilterType.NotEquals:
                        return valueStr != condStr;
                    case FilterType.Contains:
                        return valueStr.Contains(condStr, StringComparison.OrdinalIgnoreCase);
                    case FilterType.StartsWith:
                        return valueStr.StartsWith(condStr, StringComparison.OrdinalIgnoreCase);
                    case FilterType.EndsWith:
                        return valueStr.EndsWith(condStr, StringComparison.OrdinalIgnoreCase);
                    default:
                        return false;
                }
            case FilterValueType.Enum:
                // Si el valor es objeto y el filtro es compuesto, busca la propiedad indicada
                
                // Si value es una colección (como List<string> de nombres de categorías)
                if (value is System.Collections.IEnumerable enumerable && !(value is string))
                {
                    if (filter.Type == FilterType.In)
                    {
                        List<string> conditionValues = new();
                        if (filter.Condition is System.Text.Json.JsonElement jeArray && jeArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in jeArray.EnumerateArray())
                            {
                                if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                                    conditionValues.Add(item.GetString() ?? "");
                                else
                                    conditionValues.Add(item.ToString());
                            }
                        }
                        else if (filter.Condition is IEnumerable<object> list)
                        {
                            foreach (var item in list)
                                conditionValues.Add(item?.ToString() ?? "");
                        }
                        
                        // Normalizar valores de condición para comparación Unicode
                        var normalizedConditionValues = conditionValues.Select(v => v.Normalize(System.Text.NormalizationForm.FormC)).ToList();
                        
                        // Verificar si algún valor de la colección está en la condición
                        foreach (var item in enumerable)
                        {
                            if (item?.ToString() != null)
                            {
                                string normalizedItem = item.ToString().Normalize(System.Text.NormalizationForm.FormC);
                                if (normalizedConditionValues.Contains(normalizedItem, StringComparer.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                }
                
                // Manejar el caso de 'In' con array de valores (valor simple)
                if (filter.Type == FilterType.In)
                {
                    List<string> valores = new();
                    if (filter.Condition is System.Text.Json.JsonElement jeArray && jeArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var item in jeArray.EnumerateArray())
                        {
                            if (item.ValueKind == System.Text.Json.JsonValueKind.String)
                                valores.Add(item.GetString() ?? "");
                            else
                                valores.Add(item.ToString());
                        }
                    }
                    else if (filter.Condition is IEnumerable<object> list)
                    {
                        foreach (var item in list)
                            valores.Add(item?.ToString() ?? "");
                    }
                    else if (filter.Condition is string s)
                    {
                        valores.Add(s);
                    }
                    // Normalizar valores para comparación Unicode
                    string normalizedValue = (value?.ToString() ?? "").Normalize(System.Text.NormalizationForm.FormC);
                    var normalizedValores = valores.Select(v => v.Normalize(System.Text.NormalizationForm.FormC)).ToList();
                    
                    bool resultado = normalizedValores.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase);
                    return resultado;
                }
                // Manejar Equals para enums
                string condEnum = "";
                if (filter.Condition is string estr)
                {
                    condEnum = estr;
                }
                else if (filter.Condition is System.Text.Json.JsonElement jee)
                {
                    // Normalizar el string del JsonElement para manejar caracteres Unicode correctamente
                    if (jee.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        condEnum = jee.GetString() ?? "";
                    }
                    else
                    {
                        // Si no es string, intentar deserializar como string
                        condEnum = jee.GetRawText().Trim('"');
                        // Si viene como escape Unicode, deserializarlo
                        if (condEnum.StartsWith("\\u"))
                        {
                            condEnum = System.Text.RegularExpressions.Regex.Unescape(condEnum);
                        }
                    }
                }
                else
                {
                    condEnum = filter.Condition?.ToString() ?? "";
                }
                
                // Normalizar ambos strings para comparación (normalizar caracteres Unicode)
                string enumValueStr = value?.ToString() ?? "";
                condEnum = condEnum.Normalize(System.Text.NormalizationForm.FormC);
                enumValueStr = enumValueStr.Normalize(System.Text.NormalizationForm.FormC);
                
                if (filter.Type == FilterType.Equals)
                {
                    bool result = string.Equals(enumValueStr, condEnum, StringComparison.OrdinalIgnoreCase);
                    return result;
                }
                if (filter.Type == FilterType.NotEquals)
                {
                    return !string.Equals(enumValueStr, condEnum, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            case FilterValueType.Boolean:
                bool condBool;
                if (filter.Condition is System.Text.Json.JsonElement jeBool && jeBool.ValueKind == System.Text.Json.JsonValueKind.True)
                    condBool = true;
                else if (filter.Condition is System.Text.Json.JsonElement jeBool2 && jeBool2.ValueKind == System.Text.Json.JsonValueKind.False)
                    condBool = false;
                else if (bool.TryParse(filter.Condition?.ToString(), out var b))
                    condBool = b;
                else
                    condBool = false;
                if (filter.Type == FilterType.Equals)
                    return value is bool bv && bv == condBool;
                if (filter.Type == FilterType.NotEquals)
                    return value is bool bnv && bnv != condBool;
                return false;
            default:
                return false;
        }
    }
}
