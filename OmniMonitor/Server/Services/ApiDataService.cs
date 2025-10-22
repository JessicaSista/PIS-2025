using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

public interface IApiDataService
{
    Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand operand, string username);
}

public class ApiDataService : IApiDataService
{
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
        var password = await _context.Users
            .Where(u => u.Username == username)
            .Select(u => u.Password)
            .FirstOrDefaultAsync();

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
                                var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, username, password);
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
                                var device = await _sondaIMService.GetDeviceById(datasetDevice.Id_device, username, password);
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
                        var source = await _sondaIMService.GetSourceById(datasetIM.Id_Source.Value, username, password);
                        
                        if (source != null)
                        {
                            return new List<dynamic> { source };
                        }
                        else
                        {
                            return new List<dynamic>();
                        }

                    case EntityName.Group:
                        var group = await _sondaIMService.GetDeviceGroupById(datasetIM.Id_Group.Value, username, password);
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
                                var newDto = await _sondaUMService.GetNewsById(DatasetNew.Id_news, username, password);
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
                                var EventDto = await _sondaUMService.GetEventById(DatasetEvent.Id_event, username, password);
                                if (EventDto != null)
                                {
                                    resultingEvents.Add(EventDto);
                                }
                            }
                        }
                        return resultingEvents;
                    case EntityName.Zone:
                        var zone = await _sondaUMService.GetZoneById(datasetUM.Id_Zone.Value, username, password);
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
                            var asset = await _sondaAMService.GetAssetById(datasetID, username, password);
                            if (asset != null) { 
                                resultingAssets.Add(asset); 
                            }
                        }

                        return resultingAssets;

                    case EntityName.EventAM:
                        var resultingEvent = new List<dynamic>();
                        foreach (var eventDataset in datasetAM.Grupo_Event_Task_Instance)
                        {
                            var eventDto = await _sondaAMService.GetEventTaskInstanceById(eventDataset.Id_Event_Task_Instance, username, password);
                            if (eventDto != null)
                            {
                                resultingEvent.Add(eventDto);
                            }
                        }
                        return resultingEvent;

                    case EntityName.Stock:
                        var resultingStock = new List<dynamic>();
                        foreach (var eventDataset in datasetAM.Grupo_Event_Task_Instance)
                        {
                            if (eventDataset != null)
                            {
                                foreach (var stockDataset in eventDataset.Grupo_Stock)
                                {
                                    if (stockDataset != null)
                                    {
                                        var stock = await _sondaAMService.GetStockById(stockDataset.Id_Stock, username, password);
                                        if (stock != null)
                                        {
                                            resultingStock.Add(stock);
                                        }
                                    }
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
                            var eventDto = await _sondaEMService.GetEventById(datasetEvent.Id_event, username, password);
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
                            var alertDto = await _sondaEMService.GetAlertById(datasetAlert.Id_alert, username, password);
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
                            var extensionDto = await _sondaEMService.GetExtensionById(datasetExtension.Id_extension, username, password);
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
                            var category = _sondaEMService.GetCategoryById(datasetCategoria.Id_Category, username, password);
                        }
                        return resultingCategorias;
                    default:
                        throw new NotSupportedException($"Entity '{operand.EntityName}' is not supported for EventManger.");
                }

            default:
                throw new NotSupportedException($"Module type '{operand.ModuleType}' is not supported.");
        }
    }
}