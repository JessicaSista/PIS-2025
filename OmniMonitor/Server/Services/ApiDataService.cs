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
    private readonly IDatasetService _datasetService;
    private readonly IDatasetUMService _datasetUMService;
    private readonly IDatasetEMService _datasetEMService;

    public ApiDataService(IHttpClientFactory httpClientFactory, ApplicationDbContext context, ISondaEMService sondaEMService,
        ISondaIMService sondaIMService, ISondaAMService sondaAMService, IDatasetService datasetService,
        IDatasetUMService datasetUMService, IDatasetEMService datasetEMService)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _sondaEMService = sondaEMService;
        _sondaIMService = sondaIMService;
        _sondaAMService = sondaAMService;
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
                        var source = _sondaIMService.GetSourceById(datasetIM.Id_Source.Value, username, password);
                        if (source != null)
                        {
                            return new List<dynamic> { source };
                        }
                        else
                        {
                            return new List<dynamic>();
                        }

                    case EntityName.Group:
                        var group = _sondaIMService.GetDeviceGroupById(datasetIM.Id_Group.Value, username, password);
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
                return null;

            case ModuleType.AssetManager:
                return null;

            case ModuleType.EventManager:
                return null;

            default:
                throw new NotSupportedException($"Module type '{operand.ModuleType}' is not supported.");
        }
    }
}