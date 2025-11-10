using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

public interface IApiDataService
{
    Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand operand, string username);
    Task<IEnumerable<dynamic>> GetDataForOperandSinToken(JoinOperand operand);
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
                    case EntityName.Categoria:
                        var resultingCategorias = new List<dynamic>();
                        foreach (var datasetCategoria in datasetEM.DatasetCategory)
                        {
                            var category = await _sondaEMService.GetCategoryById(datasetCategoria.Id_Category, username);
                            if (category != null)
                            {
                                resultingCategorias.Add(category);
                            }
                        }
                        return resultingCategorias;
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
                var datasetIM = await _datasetService.GetDatasetIMByIdForEditAsyncSinToken(operand.DatasetId);
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
                            var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, PublicUsername);
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
                            var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, PublicUsername);
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

                        var source = await _sondaIMService.GetSourceById(datasetIM.Id_Source.Value, PublicUsername);
                        return source != null
                            ? new List<dynamic> { source }
                            : Enumerable.Empty<dynamic>();

                    case EntityName.Group:
                        if (!datasetIM.Id_Group.HasValue)
                            return Enumerable.Empty<dynamic>();

                        var group = await _sondaIMService.GetDeviceGroupById(datasetIM.Id_Group.Value, PublicUsername);
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

                switch (operand.EntityName)
                {
                    case EntityName.New:
                        if (datasetUM.DatasetNews == null || !datasetUM.DatasetNews.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicNews = new List<dynamic>();
                        foreach (var datasetNew in datasetUM.DatasetNews)
                        {
                            var news = await _sondaUMService.GetNewsById(datasetNew.Id_news, PublicUsername);
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
                            var eventDto = await _sondaUMService.GetEventById(datasetEvent.Id_event, PublicUsername);
                            if (eventDto != null)
                                publicEvents.Add(eventDto);
                        }

                        return publicEvents;

                    case EntityName.Zone:
                        if (!datasetUM.Id_Zone.HasValue)
                            return Enumerable.Empty<dynamic>();

                        var zone = await _sondaUMService.GetZoneById(datasetUM.Id_Zone.Value, PublicUsername);
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

                            var asset = await _sondaAMService.GetAssetById(assetId, PublicUsername);
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
                            var eventTask = await _sondaAMService.GetEventTaskInstanceById(eventDataset.Id_Event_Task_Instance, PublicUsername);
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
                                var stock = await _sondaAMService.GetStockById(stockDataset.Id_Stock, PublicUsername);
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

                switch (operand.EntityName)
                {
                    case EntityName.EventEM:
                        if (datasetEM.DatasetEvents == null || !datasetEM.DatasetEvents.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicEmEvents = new List<dynamic>();
                        foreach (var datasetEvent in datasetEM.DatasetEvents)
                        {
                            var eventDto = await _sondaEMService.GetEventById(datasetEvent.Id_event, PublicUsername);
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
                            var alertDto = await _sondaEMService.GetAlertById(datasetAlert.Id_alert, PublicUsername);
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
                            var extensionDto = await _sondaEMService.GetExtensionById(datasetExtension.Id_extension, PublicUsername);
                            if (extensionDto != null)
                                publicExtensions.Add(extensionDto);
                        }

                        return publicExtensions;

                    case EntityName.Categoria:
                        if (datasetEM.DatasetCategory == null || !datasetEM.DatasetCategory.Any())
                            return Enumerable.Empty<dynamic>();

                        var publicCategories = new List<dynamic>();
                        foreach (var datasetCategory in datasetEM.DatasetCategory)
                        {
                            var category = await _sondaEMService.GetCategoryById(datasetCategory.Id_Category, PublicUsername);
                            if (category != null)
                                publicCategories.Add(category);
                        }

                        return publicCategories;

                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for EventManger.");
                }

            default:
                throw new NotSupportedException($"Module type '{operand.ModuleType}' is not supported.");
        }
    }

    // Usar los tipos desde Shared.Dtos
    

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
                    var dateVal = value is DateTime dt ? dt : DateTime.Parse(value.ToString());
                    var start = range.ElementAt(0) is DateTime d1 ? d1 : DateTime.Parse(range.ElementAt(0).ToString());
                    var end = range.ElementAt(1) is DateTime d2 ? d2 : DateTime.Parse(range.ElementAt(1).ToString());
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
                        var prop = actual.GetType().GetProperty(partes[i]);
                        actual = prop?.GetValue(actual);
                    }
                }
                value = actual;
                if (value == null)
                {
                    Console.WriteLine($"[DEBUG] No se encontró la propiedad compuesta '{filter.AttributeName}' en el objeto. Claves disponibles: {string.Join(", ", dict.Keys)}");
                    matchesAll = false;
                    break;
                }
                Console.WriteLine($"[DEBUG] Filtrando '{filter.AttributeName}': valor='{value}' (tipo={value?.GetType().Name}), condición='{filter.Condition}' (tipo={filter.Condition?.GetType().Name}), tipoFiltro={filter.Type}, tipoValor={filter.ValueType}");
                if (!MatchesFilterStatic(value, filter))
                {
                    Console.WriteLine($"[DEBUG] No matchea el filtro para '{filter.AttributeName}'");
                    matchesAll = false;
                    break;
                }
            }
            if (matchesAll)
                filtered.Add(obj);
        }
        return filtered;
    }

    private static bool MatchesFilterStatic(object value, FilterCondition filter)
    {
        if (value == null) return false;
    Console.WriteLine($"[DEBUG] MatchesFilterStatic: value='{value}' (tipo={value?.GetType().Name}), condición='{filter.Condition}' (tipo={filter.Condition?.GetType().Name}), tipoFiltro={filter.Type}, tipoValor={filter.ValueType}");
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
                        start = DateTime.Parse(arr[0].GetString() ?? "");
                        end = DateTime.Parse(arr[1].GetString() ?? "");
                    }
                    else if (filter.Condition is IEnumerable<object> range && range.Count() == 2)
                    {
                        start = range.ElementAt(0) is DateTime d1 ? d1 : DateTime.Parse(range.ElementAt(0).ToString());
                        end = range.ElementAt(1) is DateTime d2 ? d2 : DateTime.Parse(range.ElementAt(1).ToString());
                    }
                    else
                    {
                        var arr = filter.Condition as object[];
                        start = DateTime.Parse(arr?[0]?.ToString() ?? "");
                        end = DateTime.Parse(arr?[1]?.ToString() ?? "");
                    }
                    var dateVal = value is DateTime dtv ? dtv : DateTime.Parse(value.ToString());
                    return dateVal >= start && dateVal <= end;
                }
                if (filter.Condition is System.Text.Json.JsonElement jeDate && jeDate.ValueKind == System.Text.Json.JsonValueKind.String)
                    condDate = DateTime.Parse(jeDate.GetString() ?? "");
                else if (filter.Condition is DateTime dt)
                    condDate = dt;
                else
                    condDate = DateTime.Parse(filter.Condition?.ToString() ?? "");
                if (filter.Type == FilterType.Equals)
                    return value is DateTime dv && dv == condDate;
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
                string condStr = filter.Condition is string str ? str
                    : filter.Condition is System.Text.Json.JsonElement je ? je.GetString()
                    : filter.Condition?.ToString() ?? "";
                switch (filter.Type)
                {
                    case FilterType.Equals:
                        return value is string sv && sv == condStr;
                    case FilterType.NotEquals:
                        return value is string snv && snv != condStr;
                    case FilterType.Contains:
                        return value is string sc && sc.Contains(condStr);
                    case FilterType.StartsWith:
                        return value is string ss && ss.StartsWith(condStr);
                    case FilterType.EndsWith:
                        return value is string se && se.EndsWith(condStr);
                    default:
                        return false;
                }
            case FilterValueType.Enum:
                // Si el valor es objeto y el filtro es compuesto, busca la propiedad indicada
                Console.WriteLine($"[DEBUG] Enum: valor para comparación='{value}' tipo={value?.GetType().Name}");
                // Manejar el caso de 'In' con array de valores
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
                    Console.WriteLine($"[DEBUG] Enum/In: valor final para comparación='{value?.ToString()}', valores comparados=[{string.Join(", ", valores)}]");
                    bool resultado = valores.Contains(value?.ToString());
                    Console.WriteLine($"[DEBUG] Enum/In: resultado comparación={resultado}");
                    return resultado;
                }
                // Manejar Equals para enums
                string condEnum = filter.Condition is string estr ? estr
                    : filter.Condition is System.Text.Json.JsonElement jee ? jee.GetString()
                    : filter.Condition?.ToString() ?? "";
                if (filter.Type == FilterType.Equals)
                    return value?.ToString() == condEnum;
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
