using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Servicio para la gestión de datasets de Asset Manager (AM).
    /// </summary>
    public interface IDatasetAmService
    {
        /// <summary>
        /// Crea un nuevo DatasetAM.
        /// </summary>
        /// <param name="request">Datos para la creación del dataset.</param>
        /// <param name="dataset">Identificador del dataset padre.</param>
        /// <returns>El dataset creado.</returns>
    Task<List<DatasetReducedAMDTO>> GetReducedAssetsByDatasetIdAsync(int datasetId, string username);
    Task<List<DatasetReducedAMEventsDTO>> GetReducedEventsByDatasetIdAsync(int datasetId, string username);
        Task<DatasetAM> CreateDatasetAMAsync(CreateDatasetAMRequest request, int dataset);

        /// <summary>
        /// Obtiene todos los DatasetAMs de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de datasets AM asociados al usuario.</returns>
        Task<List<DatasetAM>> GetAllDatasetAMsAsync(string username);

        /// <summary>
        /// Obtiene un DatasetAM por su ID y usuario, con lógica dinámica.
        /// </summary>
        /// <param name="id">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset encontrado o null si no existe.</returns>
        Task<DatasetAM?> GetDatasetAMByIdAsync(int id, string username);

        /// <summary>
        /// Obtiene un DatasetAM por su ID y usuario para edición (sin lógica dinámica).
        /// </summary>
        /// <param name="id">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset encontrado o null si no existe.</returns>
        Task<DatasetAM?> GetDatasetAMByIdForEditAsync(int id, string username);

        /// <summary>
        /// Actualiza un DatasetAM existente.
        /// </summary>
        /// <param name="datasetAM">Instancia existente del dataset a actualizar.</param>
        /// <param name="request">Datos nuevos para la actualización.</param>
        /// <returns>El dataset actualizado.</returns>
        Task<DatasetAM> UpdateDatasetAMAsync(DatasetAM datasetAM, CreateDatasetAMRequest request);

        /// <summary>
        /// Elimina un DatasetAM por su ID y usuario.
        /// </summary>
        /// <param name="id">ID del dataset a eliminar.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Una tarea que representa la operación de eliminación.</returns>
        Task DeleteDatasetAMAsync(int id, string username);
    }

    #endregion

    #region Classes

    /// <inheritdoc />
    public class DatasetAmService : IDatasetAmService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;
        private readonly ILogger<DatasetAmService> _logger;

        public DatasetAmService(ApplicationDbContext context, ISondaAMService sondaAMService, ILogger<DatasetAmService> logger)
        {
            _context = context;
            _sondaAMService = sondaAMService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<DatasetAM> CreateDatasetAMAsync(CreateDatasetAMRequest request, int dataset)
        {
            try
            {
                _logger.LogInformation("Creando DatasetAM '{Nombre}' para usuario {Username}", request.Nombre, request.Username);

                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Nombre))
                {
                    throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");
                }

                DatasetAM? existingDataset = await _context.DatasetAM
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => string.Equals(d.Username, request.Username, StringComparison.Ordinal) && string.Equals(d.Nombre, request.Nombre, StringComparison.Ordinal));

                if (existingDataset != null)
                {
                    _logger.LogWarning("Ya existe un dataset con el nombre '{Nombre}' para el usuario '{Username}'", request.Nombre, request.Username);
                    throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Nombre}' para el usuario '{request.Username}'.");
                }

                DatasetAM newDatasetAM = new()
                {
                    Username = request.Username,
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    Is_Dataset = request.IsDataset,
                    DatasetId = dataset,
                    Type_Dataset = request.Type_Dataset,
                    Id_Event_Task = request.Type_Dataset == 1 ? request.Id_Event_Task : null,
                    Id_Asset_Type = request.Type_Dataset == 2 ? request.Id_Asset_Type : null,
                    ContentType = string.Equals(request.IsDataset, "S", StringComparison.Ordinal) ? "0" : request.ContentType
                };

                if (request.Type_Dataset == 1 && request.Grupo_Event_Task_Instance_Ids != null)
                {
                    if (request.StockIds != null && request.StockIds.Count > 0)
                    {
                        if (request.Grupo_Event_Task_Instance_Ids.Count != 1)
                        {
                            throw new InvalidOperationException("Solo se pueden asociar stocks si se selecciona un único Event Task Instance.");
                        }

                        DatasetEventTaskInstance eventTaskInstance = new()
                        {
                            Id_Event_Task_Instance = request.Grupo_Event_Task_Instance_Ids[0],
                            Grupo_Stock = request.StockIds.Select(stockId => new DatasetStock { Id_Stock = stockId }).ToList()
                        };
                        newDatasetAM.Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { eventTaskInstance };
                    }
                    else
                    {
                        newDatasetAM.Grupo_Event_Task_Instance = request.Grupo_Event_Task_Instance_Ids
                            .Select(eventTaskInstanceId => new DatasetEventTaskInstance
                            {
                                Id_Event_Task_Instance = eventTaskInstanceId
                            }).ToList();
                    }
                }
                else if (request.Type_Dataset == 2 && request.Grupo_Asset_Ids != null)
                {
                    newDatasetAM.Grupo_Asset = request.Grupo_Asset_Ids
                        .Select(id => new DatasetAsset { Id_Asset = id }).ToList();
                }

                _context.DatasetAM.Add(newDatasetAM);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetAM '{Nombre}' creado correctamente para usuario {Username}", request.Nombre, request.Username);

                return newDatasetAM;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando DatasetAM '{Nombre}' para usuario {Username}", request.Nombre, request.Username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<DatasetAM>> GetAllDatasetAMsAsync(string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los DatasetAMs para usuario {Username}", username);

                List<DatasetAM> result = await _context.DatasetAM
                    .AsNoTracking()
                    .Include(d => d.Grupo_Event_Task_Instance)
                        .ThenInclude(e => e.Grupo_Stock)
                    .Include(d => d.Grupo_Asset)
                    .Where(d => string.Equals(d.Username, username, StringComparison.Ordinal))
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los DatasetAMs para usuario {Username}", username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetAM?> GetDatasetAMByIdAsync(int id, string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo DatasetAM con ID {Id} para usuario {Username}", id, username);

                if (id < 0)
                {
                    throw new ArgumentException("El id debe ser mayor o igual a 0.", nameof(id));
                }

                DatasetAM? datasetAM = await _context.DatasetAM
                    .AsNoTracking()
                    .Include(d => d.Grupo_Event_Task_Instance)
                        .ThenInclude(e => e.Grupo_Stock)
                    .Include(d => d.Grupo_Asset)
                    .FirstOrDefaultAsync(d => d.Id_Dataset == id && string.Equals(d.Username, username, StringComparison.Ordinal));

                if (datasetAM == null)
                {
                    _logger.LogWarning("No se encontró el DatasetAM con ID {Id} para usuario {Username}", id, username);
                    return null;
                }

                // Lógica dinámica para datasets formales
                if (string.Equals(datasetAM.Is_Dataset, "S", StringComparison.Ordinal))
                {
                    User? user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => string.Equals(u.UserName, username, StringComparison.Ordinal));
                    if (user != null)
                    {
                        if (datasetAM.Type_Dataset == 1 && (datasetAM.Grupo_Event_Task_Instance == null || !datasetAM.Grupo_Event_Task_Instance.Any()))
                        {
                            List<EventTaskInstanceDto>? eventTaskInstances = await _sondaAMService.GetEventTaskInstances(
                                "1980-01-01T03:00:00,2050-10-31T03:00:00",
                                null, string.Empty, null, string.Empty, string.Empty, datasetAM.Id_Event_Task, null, null, false, false, user.UserName
                            );
                            if (eventTaskInstances != null && eventTaskInstances.Any())
                            {
                                datasetAM.Grupo_Event_Task_Instance = eventTaskInstances
                                    .Select(e => new DatasetEventTaskInstance
                                    {
                                        Id_Event_Task_Instance = e.Id,
                                        Grupo_Stock = new List<DatasetStock>()
                                    }).ToList();
                            }
                        }
                        else if (datasetAM.Type_Dataset == 2 && (datasetAM.Grupo_Asset == null || !datasetAM.Grupo_Asset.Any()))
                        {
                            List<AssetDto>? assets = await _sondaAMService.GetAssets(null, null, null, datasetAM.Id_Asset_Type, null, null, user.UserName);
                            if (assets != null)
                            {
                                datasetAM.Grupo_Asset = assets.Select(a => new DatasetAsset
                                {
                                    Id_Asset = a.Id
                                }).ToList();
                            }
                        }
                        else if (datasetAM.Type_Dataset == 1 && datasetAM.Grupo_Event_Task_Instance != null && datasetAM.Grupo_Event_Task_Instance.Count == 1)
                        {
                            DatasetEventTaskInstance eventTaskInstance = datasetAM.Grupo_Event_Task_Instance.First();
                            if (eventTaskInstance.Grupo_Stock == null || !eventTaskInstance.Grupo_Stock.Any())
                            {
                                List<EventTaskInstanceStockDto>? stocks = await _sondaAMService.GetEventTaskInstanceStock(eventTaskInstance.Id_Event_Task_Instance, user.UserName);
                                if (stocks != null)
                                {
                                    eventTaskInstance.Grupo_Stock = stocks.Select(s => new DatasetStock { Id_Stock = s.Id }).ToList();
                                }
                            }
                        }
                    }
                }

                return datasetAM;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo DatasetAM con ID {Id} para usuario {Username}", id, username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetAM?> GetDatasetAMByIdForEditAsync(int id, string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo DatasetAM para edición con ID {Id} para usuario {Username}", id, username);

                DatasetAM? datasetAM = await _context.DatasetAM
                    .AsNoTracking()
                    .Include(d => d.Grupo_Event_Task_Instance)
                        .ThenInclude(e => e.Grupo_Stock)
                    .Include(d => d.Grupo_Asset)
                    .FirstOrDefaultAsync(d => d.Id_Dataset == id && string.Equals(d.Username, username, StringComparison.Ordinal));

                return datasetAM;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo DatasetAM para edición con ID {Id} para usuario {Username}", id, username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetAM> UpdateDatasetAMAsync(DatasetAM datasetAM, CreateDatasetAMRequest request)
        {
            try
            {
                _logger.LogInformation("Actualizando DatasetAM con ID {Id}", datasetAM?.Id_Dataset);

                if (datasetAM == null)
                {
                    throw new InvalidOperationException($"No se encontró el DatasetAM con ID {datasetAM?.Id_Dataset}.");
                }

                if (!string.Equals(datasetAM.Nombre, request.Nombre, StringComparison.Ordinal))
                {
                    DatasetAM? duplicateDataset = await _context.DatasetAM
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => string.Equals(d.Username, datasetAM.Username, StringComparison.Ordinal) &&
                                                string.Equals(d.Nombre, datasetAM.Nombre, StringComparison.Ordinal) &&
                                                d.Id_Dataset != datasetAM.Id_Dataset);

                    if (duplicateDataset != null)
                    {
                        _logger.LogWarning("Ya existe un dataset con el nombre '{Nombre}' para el usuario '{Username}'", datasetAM.Nombre, datasetAM.Username);
                        throw new InvalidOperationException($"Ya existe un dataset con el nombre '{datasetAM.Nombre}' para el usuario '{datasetAM.Username}'.");
                    }
                }

                datasetAM.Nombre = request.Nombre;
                datasetAM.Descripcion = request.Descripcion;
                datasetAM.Type_Dataset = request.Type_Dataset;
                datasetAM.Id_Event_Task = request.Id_Event_Task;
                datasetAM.Id_Asset_Type = request.Id_Asset_Type;
                datasetAM.Is_Dataset = request.IsDataset;
                datasetAM.ContentType = request.ContentType;

                // Actualizar Grupo_Event_Task_Instance y stocks
                if (datasetAM.Grupo_Event_Task_Instance != null && datasetAM.Grupo_Event_Task_Instance.Any())
                {
                    foreach (DatasetEventTaskInstance eventTaskInstance in datasetAM.Grupo_Event_Task_Instance)
                    {
                        if (eventTaskInstance.Grupo_Stock != null && eventTaskInstance.Grupo_Stock.Any())
                        {
                            _context.Set<DatasetStock>().RemoveRange(eventTaskInstance.Grupo_Stock);
                        }
                    }
                    _context.Set<DatasetEventTaskInstance>().RemoveRange(datasetAM.Grupo_Event_Task_Instance);
                    datasetAM.Grupo_Event_Task_Instance.Clear();
                }

                if (request.Grupo_Event_Task_Instance_Ids != null && request.Grupo_Event_Task_Instance_Ids.Any())
                {
                    datasetAM.Grupo_Event_Task_Instance = request.Grupo_Event_Task_Instance_Ids
                        .Select(eventTaskInstanceId =>
                        {
                            DatasetEventTaskInstance newEventTaskInstance = new()
                            {
                                Id_Event_Task_Instance = eventTaskInstanceId,
                                Grupo_Stock = new List<DatasetStock>()
                            };
                            if (request.StockIds != null && request.StockIds.Any())
                            {
                                newEventTaskInstance.Grupo_Stock = request.StockIds
                                    .Select(stockId => new DatasetStock { Id_Stock = stockId }).ToList();
                            }
                            return newEventTaskInstance;
                        }).ToList();
                }

                // Actualizar Grupo_Asset
                if (datasetAM.Grupo_Asset != null && datasetAM.Grupo_Asset.Any())
                {
                    _context.Set<DatasetAsset>().RemoveRange(datasetAM.Grupo_Asset);
                    datasetAM.Grupo_Asset.Clear();
                }

                if (request.Grupo_Asset_Ids != null && request.Grupo_Asset_Ids.Any())
                {
                    datasetAM.Grupo_Asset = request.Grupo_Asset_Ids
                        .Select(idAsset => new DatasetAsset { Id_Asset = idAsset }).ToList();
                }

                _context.DatasetAM.Update(datasetAM);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetAM con ID {Id} actualizado correctamente", datasetAM.Id_Dataset);

                return datasetAM;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando DatasetAM con ID {Id}", datasetAM?.Id_Dataset);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteDatasetAMAsync(int id, string username)
        {
            try
            {
                _logger.LogInformation("Eliminando DatasetAM con ID {Id} para usuario {Username}", id, username);

                DatasetAM? datasetAM = await _context.DatasetAM
                    .Include(d => d.Grupo_Event_Task_Instance)
                    .ThenInclude(e => e.Grupo_Stock)
                    .Include(d => d.Grupo_Asset)
                    .FirstOrDefaultAsync(d => d.Id_Dataset == id && string.Equals(d.Username, username, StringComparison.Ordinal));

                if (datasetAM == null)
                {
                    _logger.LogWarning("No se encontró el DatasetAM con ID {Id} para el usuario {Username}", id, username);
                    throw new InvalidOperationException($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
                }

                if (datasetAM.Grupo_Event_Task_Instance != null)
                {
                    foreach (DatasetEventTaskInstance eventTaskInstance in datasetAM.Grupo_Event_Task_Instance)
                    {
                        if (eventTaskInstance.Grupo_Stock != null && eventTaskInstance.Grupo_Stock.Any())
                        {
                            List<DatasetStock> stocksToRemove = eventTaskInstance.Grupo_Stock.Where(s => s.Grupo_Stock > 0).ToList();
                            if (stocksToRemove.Any())
                            {
                                _context.Set<DatasetStock>().RemoveRange(stocksToRemove);
                            }
                        }
                    }
                    List<DatasetEventTaskInstance> eventTaskInstancesToRemove = datasetAM.Grupo_Event_Task_Instance.Where(e => e.Id > 0).ToList();
                    if (eventTaskInstancesToRemove.Any())
                    {
                        _context.Set<DatasetEventTaskInstance>().RemoveRange(eventTaskInstancesToRemove);
                    }
                }

                if (datasetAM.Grupo_Asset != null && datasetAM.Grupo_Asset.Any())
                {
                    List<DatasetAsset> assetsToRemove = datasetAM.Grupo_Asset.Where(a => a.Grupo_Asset > 0).ToList();
                    if (assetsToRemove.Any())
                    {
                        _context.Set<DatasetAsset>().RemoveRange(assetsToRemove);
                    }
                }

                _context.DatasetAM.Remove(datasetAM);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetAM con ID {Id} eliminado correctamente para usuario {Username}", id, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando DatasetAM con ID {Id} para usuario {Username}", id, username);
                throw;
            }
        }

        public async Task<List<DatasetReducedAMDTO>> GetReducedAssetsByDatasetIdAsync(int datasetId, string username)
        {
            if (datasetId <= 0)
                throw new ArgumentException("Debe especificar un id de dataset válido.");

            var assets = await _context.Set<DatasetAsset>()
                .Where(a => a.DatasetAMId == datasetId)
                .ToListAsync();

            var reducedList = new List<DatasetReducedAMDTO>();
            foreach (var asset in assets)
            {
                if (int.TryParse(asset.Id_Asset, out int assetId))
                {
                    var assetInfo = await _sondaAMService.GetAssetById(assetId, username);
                    if (assetInfo != null)
                    {
                        reducedList.Add(new DatasetReducedAMDTO
                        {
                            nombre = assetInfo.Name,
                            codigo = assetInfo.Code,
                            address = assetInfo.Address,
                            referencia = assetInfo.Reference,
                            bundle = assetInfo.BundleDto?.Name ?? assetInfo.BundleId.ToString(),
                            brand = assetInfo.BrandDto?.Name,
                            state = assetInfo.StateDto?.Name,
                            modelo = assetInfo.ModelDto?.Name,
                            responsable = assetInfo.ResponsibleDto?.Name,
                            proveedor = assetInfo.ProviderDto?.Name
                        });
                    }
                }
            }
            return reducedList;
        }

    public async Task<List<DatasetReducedAMEventsDTO>> GetReducedEventsByDatasetIdAsync(int datasetId, string username)
        {
            if (datasetId <= 0)
                throw new ArgumentException("Debe especificar un id de dataset válido.");

            var events = await _context.Set<DatasetEventTaskInstance>()
                .Where(a => a.DatasetAMId == datasetId)
                .ToListAsync();

            var reducedList = new List<DatasetReducedAMEventsDTO>();
            foreach (var eventItem in events)
            {
                // Obtener el DTO completo usando el servicio externo
                var eventTaskInstanceDto = await _sondaAMService.GetEventTaskInstanceById(eventItem.Id_Event_Task_Instance, username);
                if (eventTaskInstanceDto != null)
                {
                    reducedList.Add(new DatasetReducedAMEventsDTO
                    {
                        eventTask = eventTaskInstanceDto.EventTaskDto?.Subject,
                        autor = eventTaskInstanceDto.TakenBy?.Name,
                        state = eventTaskInstanceDto.State,
                        subject = eventTaskInstanceDto.Subject,
                        takenBy = eventTaskInstanceDto.TakenBy?.Name,
                        critico = eventTaskInstanceDto.Critical.HasValue ? (eventTaskInstanceDto.Critical.Value ? "Sí" : "No") : "No"
                    });
                }
            }
            return reducedList;
        }
    }
    #endregion
}
