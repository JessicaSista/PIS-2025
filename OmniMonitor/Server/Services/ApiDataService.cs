using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Servicio para la obtención de datos dinámicos según operandos de unión entre módulos.
    /// </summary>
    public interface IApiDataService
    {
        /// <summary>
        /// Obtiene datos dinámicos para un operando de unión y usuario.
        /// </summary>
        /// <param name="joinOperand">Operando de unión que describe el origen y tipo de datos.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos según el módulo y entidad solicitada.</returns>
        Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand joinOperand, string username);
    }

    /// <inheritdoc />
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
        private readonly ILogger<ApiDataService> _logger;

        /// <summary>
        /// Constructor de ApiDataService.
        /// </summary>
        public ApiDataService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            ISondaEMService sondaEMService,
            ISondaIMService sondaIMService,
            ISondaAMService sondaAMService,
            IDatasetService datasetService,
            IDatasetUMService datasetUMService,
            IDatasetEMService datasetEMService,
            ISondaUMService sondaUMService,
            IDatasetAmService datasetAMService,
            ILogger<ApiDataService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _sondaEMService = sondaEMService;
            _sondaIMService = sondaIMService;
            _sondaAMService = sondaAMService;
            _sondaUMService = sondaUMService;
            _datasetService = datasetService;
            _datasetUMService = datasetUMService;
            _datasetEMService = datasetEMService;
            _datasetAMService = datasetAMService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene datos dinámicos para un operando de unión y usuario.
        /// </summary>
        /// <param name="joinOperand">Operando de unión que describe el origen y tipo de datos.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos según el módulo y entidad solicitada.</returns>
        public async Task<IEnumerable<dynamic>> GetDataForOperand(JoinOperand joinOperand, string username)
        {
            try
            {
                _logger.LogInformation("Iniciando obtención de datos para módulo {ModuleType} y entidad {EntityName} (usuario: {Username})", joinOperand.ModuleType, joinOperand.EntityName, username);

                switch (joinOperand.ModuleType)
                {
                    case ModuleType.InsightMonitor:
                        return await HandleInsightMonitorAsync(joinOperand, username);
                    case ModuleType.UrbanMonitor:
                        return await HandleUrbanMonitorAsync(joinOperand, username);
                    case ModuleType.AssetManager:
                        return await HandleAssetManagerAsync(joinOperand, username);
                    case ModuleType.EventManager:
                        return await HandleEventManagerAsync(joinOperand, username);
                    default:
                        _logger.LogWarning("Tipo de módulo '{ModuleType}' no soportado.", joinOperand.ModuleType);
                        throw new NotSupportedException($"Module type '{joinOperand.ModuleType}' is not supported.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo datos para operand {Operand} de usuario {Username}", joinOperand, username);
                throw;
            }
        }

        #region Métodos privados por módulo

        /// <summary>
        /// Procesa la obtención de datos para InsightMonitor.
        /// </summary>
        /// <param name="joinOperand">Operando de unión.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos.</returns>
        private async Task<IEnumerable<dynamic>> HandleInsightMonitorAsync(JoinOperand joinOperand, string username)
        {
            var datasetIm = await _datasetService.GetDatasetIMByIdAsync(joinOperand.DatasetId, username);
            if (datasetIm == null)
            {
                _logger.LogWarning("No se encontró DatasetIM con ID {DatasetId} para usuario {Username}", joinOperand.DatasetId, username);
                return Enumerable.Empty<dynamic>();
            }

            _logger.LogInformation("Procesando InsightMonitor para entidad {EntityName}", joinOperand.EntityName);

            switch (joinOperand.EntityName)
            {
                case EntityName.Device:
                    return await ProcessListAsync(
                        datasetIm.DatasetDevices,
                        device => _sondaIMService.GetDeviceById(device.Id_device, username),
                        device => device != null
                    );
                case EntityName.Sensor:
                    return await ProcessListAsync(
                        datasetIm.DatasetDevices,
                        async device =>
                        {
                            var deviceResult = await _sondaIMService.GetDeviceById(device.Id_device, username);
                            if (deviceResult?.Sensors == null)
                            {
                                return new();
                            }
                            return deviceResult.Sensors
                                .Where(sensor => sensor != null && string.Equals(sensor.Name, datasetIm.SensorName))
                                .ToList() ?? new();
                        }
                    );
                case EntityName.Source:
                    return await GetSingleAsync(() => _sondaIMService.GetSourceById(datasetIm.Id_Source.Value, username));
                case EntityName.Group:
                    return await GetSingleAsync(() => _sondaIMService.GetDeviceGroupById(datasetIm.Id_Group.Value, username));
                default:
                    _logger.LogWarning("Entidad '{EntityName}' no soportada para Insight Monitor.", joinOperand.EntityName);
                    throw new NotSupportedException($"Entity '{joinOperand.EntityName}' is not supported for Insight Monitor.");
            }
        }

        /// <summary>
        /// Procesa la obtención de datos para UrbanMonitor.
        /// </summary>
        /// <param name="joinOperand">Operando de unión.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos.</returns>
        private async Task<IEnumerable<dynamic>> HandleUrbanMonitorAsync(JoinOperand joinOperand, string username)
        {
            var datasetUm = await _datasetUMService.GetDatasetUMByIdAsync(joinOperand.DatasetId, username);
            if (datasetUm == null)
            {
                _logger.LogWarning("No se encontró DatasetUM con ID {DatasetId} para usuario {Username}", joinOperand.DatasetId, username);
                return Enumerable.Empty<dynamic>();
            }

            _logger.LogInformation("Procesando UrbanMonitor para entidad {EntityName}", joinOperand.EntityName);

            switch (joinOperand.EntityName)
            {
                case EntityName.New:
                    return await ProcessListAsync(
                        datasetUm.DatasetNews,
                        news => _sondaUMService.GetNewsById(news.Id_news, username),
                        news => news != null
                    );
                case EntityName.EventUM:
                    return await ProcessListAsync(
                        datasetUm.DatasetEvents,
                        eventUm => _sondaUMService.GetEventById(eventUm.Id_event, username),
                        eventUm => eventUm != null
                    );
                case EntityName.Zone:
                    return await GetSingleAsync(() => _sondaUMService.GetZoneById(datasetUm.Id_Zone.Value, username));
                default:
                    _logger.LogWarning("Entidad '{EntityName}' no soportada para UrbanMonitor.", joinOperand.EntityName);
                    throw new NotSupportedException($"Entity '{joinOperand.EntityName}' is not supported for UrbanMonitor.");
            }
        }

        /// <summary>
        /// Procesa la obtención de datos para AssetManager.
        /// </summary>
        /// <param name="joinOperand">Operando de unión.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos.</returns>
        private async Task<IEnumerable<dynamic>> HandleAssetManagerAsync(JoinOperand joinOperand, string username)
        {
            var datasetAm = await _datasetAMService.GetDatasetAMByIdAsync(joinOperand.DatasetId, username);
            if (datasetAm == null)
            {
                _logger.LogWarning("No se encontró DatasetAM con ID {DatasetId} para usuario {Username}", joinOperand.DatasetId, username);
                return Enumerable.Empty<dynamic>();
            }

            _logger.LogInformation("Procesando AssetManager para entidad {EntityName}", joinOperand.EntityName);

            switch (joinOperand.EntityName)
            {
                case EntityName.Asset:
                    return await ProcessListAsync(
                        datasetAm.Grupo_Asset,
                        asset => _sondaAMService.GetAssetById(int.Parse(asset.Id_Asset), username),
                        asset => asset != null
                    );
                case EntityName.EventAM:
                    return await ProcessListAsync(
                        datasetAm.Grupo_Event_Task_Instance,
                        eventAm => _sondaAMService.GetEventTaskInstanceById(eventAm.Id_Event_Task_Instance, username),
                        eventAm => eventAm != null
                    );
                case EntityName.Stock:
                    return await ProcessNestedListAsync(
                        datasetAm.Grupo_Event_Task_Instance,
                        eventAm => eventAm.Grupo_Stock,
                        stock => _sondaAMService.GetStockById(stock.Id_Stock, username),
                        stock => stock != null
                    );
                default:
                    _logger.LogWarning("Entidad '{EntityName}' no soportada para AssetManager.", joinOperand.EntityName);
                    throw new NotSupportedException($"Entity '{joinOperand.EntityName}' is not supported for AssetManager.");
            }
        }

        /// <summary>
        /// Procesa la obtención de datos para EventManager.
        /// </summary>
        /// <param name="joinOperand">Operando de unión.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de objetos dinámicos.</returns>
        private async Task<IEnumerable<dynamic>> HandleEventManagerAsync(JoinOperand joinOperand, string username)
        {
            var datasetEm = await _datasetEMService.GetDatasetEMByIdAsync(joinOperand.DatasetId, username);
            if (datasetEm == null)
            {
                _logger.LogWarning("No se encontró DatasetEM con ID {DatasetId} para usuario {Username}", joinOperand.DatasetId, username);
                return Enumerable.Empty<dynamic>();
            }

            _logger.LogInformation("Procesando EventManager para entidad {EntityName}", joinOperand.EntityName);

            switch (joinOperand.EntityName)
            {
                case EntityName.EventEM:
                    return await ProcessListAsync(
                        datasetEm.DatasetEvents,
                        eventEm => _sondaEMService.GetEventById(eventEm.Id_event, username),
                        eventEm => eventEm != null
                    );
                case EntityName.Alert:
                    return await ProcessListAsync(
                        datasetEm.DatasetAlerts,
                        alert => _sondaEMService.GetAlertById(alert.Id_alert, username),
                        alert => alert != null
                    );
                case EntityName.Extension:
                    return await ProcessListAsync(
                        datasetEm.DatasetExtensions,
                        extension => _sondaEMService.GetExtensionById(extension.Id_extension, username),
                        extension => extension != null
                    );
                case EntityName.Categoria:
                    return await ProcessListAsync(
                        datasetEm.DatasetCategory,
                        category => _sondaEMService.GetCategoryById(category.Id_Category, username),
                        category => category != null
                    );
                default:
                    _logger.LogWarning("Entidad '{EntityName}' no soportada para EventManager.", joinOperand.EntityName);
                    throw new NotSupportedException($"Entity '{joinOperand.EntityName}' is not supported for EventManager.");
            }
        }

        #endregion

        #region Métodos auxiliares genéricos

        /// <summary>
        /// Procesa una lista y retorna los resultados no nulos.
        /// </summary>
        /// <typeparam name="T">Tipo de elemento de entrada.</typeparam>
        /// <typeparam name="TResult">Tipo de resultado.</typeparam>
        /// <param name="items">Elementos a procesar.</param>
        /// <param name="selector">Función asíncrona de selección.</param>
        /// <param name="filter">Filtro opcional para los resultados.</param>
        /// <returns>Lista de resultados no nulos.</returns>
        private async Task<IEnumerable<dynamic>> ProcessListAsync<T, TResult>(IEnumerable<T> items, Func<T, Task<TResult>> selector, Func<TResult, bool>? filter = null)
        {
            if (items == null)
            {
                return Enumerable.Empty<dynamic>();
            }

            var tasks = items.Select(selector);
            var results = await Task.WhenAll(tasks);
            if (filter != null)
            {
                return results.Where(filter).Cast<dynamic>().ToList();
            }
            else
            {
                return results.Cast<dynamic>().ToList();
            }
        }

        /// <summary>
        /// Procesa una lista anidada (por ejemplo, stocks dentro de eventos).
        /// </summary>
        /// <typeparam name="T">Tipo de elemento padre.</typeparam>
        /// <typeparam name="TChild">Tipo de elemento hijo.</typeparam>
        /// <typeparam name="TResult">Tipo de resultado.</typeparam>
        /// <param name="items">Elementos padres.</param>
        /// <param name="childSelector">Función para obtener los hijos.</param>
        /// <param name="selector">Función asíncrona de selección.</param>
        /// <param name="filter">Filtro opcional para los resultados.</param>
        /// <returns>Lista de resultados no nulos.</returns>
        private async Task<IEnumerable<dynamic>> ProcessNestedListAsync<T, TChild, TResult>(
            IEnumerable<T> items,
            Func<T, IEnumerable<TChild>> childSelector,
            Func<TChild, Task<TResult>> selector,
            Func<TResult, bool>? filter = null)
        {
            if (items == null)
            {
                return Enumerable.Empty<dynamic>();
            }

            var childItems = items.SelectMany(childSelector ?? (_ => Enumerable.Empty<TChild>()));
            var tasks = childItems.Select(selector);
            var results = await Task.WhenAll(tasks);
            if (filter != null)
            {
                return results.Where(filter).Cast<dynamic>().ToList();
            }
            else
            {
                return results.Cast<dynamic>().ToList();
            }
        }

        /// <summary>
        /// Procesa una llamada que retorna un solo elemento.
        /// </summary>
        /// <typeparam name="TResult">Tipo de resultado.</typeparam>
        /// <param name="func">Función asíncrona que retorna el resultado.</param>
        /// <returns>Lista con un solo elemento o vacía.</returns>
        private async Task<IEnumerable<dynamic>> GetSingleAsync<TResult>(Func<Task<TResult>> func)
        {
            var result = await func();
            if (result != null)
            {
                return new List<dynamic> { result };
            }
            else
            {
                return new List<dynamic>();
            }
        }

        #endregion
    }
}