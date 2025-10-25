using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.AM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    public interface IDatasetAmService
    {
        Task<DatasetAM> CreateDatasetAMAsync(CreateDatasetAMRequest request, int dataset);
        Task<List<DatasetAM>> GetAllDatasetAMsAsync(string username);
        Task<DatasetAM?> GetDatasetAMByIdAsync(int id, string username);
        Task<DatasetAM?> GetDatasetAMByIdForEditAsync(int id, string username);
        Task<DatasetAM> UpdateDatasetAMAsync(DatasetAM datasetAM, CreateDatasetAMRequest request);
        Task DeleteDatasetAMAsync(int id, string username);
    }

    public class DatasetAmService : IDatasetAmService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaAMService _sondaAMService;

        public DatasetAmService(ApplicationDbContext context, ISondaAMService sondaAMService)
        {
            _context = context;
            _sondaAMService = sondaAMService;
        }

        public async Task<DatasetAM> CreateDatasetAMAsync(CreateDatasetAMRequest request, int dataset)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Nombre))
                throw new ArgumentException("El nombre de usuario y el nombre del dataset son obligatorios.");

            var existingDataset = await _context.DatasetAM
                .FirstOrDefaultAsync(d => d.Username == request.Username && d.Nombre == request.Nombre);

            if (existingDataset != null)
            {
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{request.Nombre}' para el usuario '{request.Username}'.");
            }

            var newDatasetAM = new DatasetAM
            {
                Username = request.Username,
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                Is_Dataset = request.IsDataset,
                DatasetId = dataset,
                Type_Dataset = request.Type_Dataset,
                Id_Event_Task = request.Type_Dataset == 1 ? request.Id_Event_Task : null,
                Id_Asset_Type = request.Type_Dataset == 2 ? request.Id_Asset_Type : null
            };

            if (request.IsDataset == "S")
            {
                newDatasetAM.ContentType = "0"; // Dataset formal
            }
            else
            {
                newDatasetAM.ContentType = request.ContentType; // 1=device, 2=source, 3=sensor
            }


            if (request.Type_Dataset == 1 && request.Grupo_Event_Task_Instance_Ids != null)
            {
                // Si hay stocks, debe haber solo un event task instance seleccionado
                if (request.StockIds != null && request.StockIds.Count > 0)
                {
                    if (request.Grupo_Event_Task_Instance_Ids.Count != 1)
                        throw new InvalidOperationException("Solo se pueden asociar stocks si se selecciona un único Event Task Instance.");

                    // Asociar los stocks al único event task instance
                    var eventTaskInstance = new DatasetEventTaskInstance
                    {
                        Id_Event_Task_Instance = request.Grupo_Event_Task_Instance_Ids[0],
                        Grupo_Stock = request.StockIds.Select(stockId => new DatasetStock { Id_Stock = stockId }).ToList()
                    };
                    newDatasetAM.Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance> { eventTaskInstance };
                }
                else
                {
                    // Si no hay stocks, se pueden asociar varios event task instances sin stocks
                    newDatasetAM.Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance>();
                    foreach (var eventTaskInstanceId in request.Grupo_Event_Task_Instance_Ids)
                    {
                        newDatasetAM.Grupo_Event_Task_Instance.Add(new DatasetEventTaskInstance
                        {
                            Id_Event_Task_Instance = eventTaskInstanceId
                        });
                    }
                }
            }
            else if (request.Type_Dataset == 2 && request.Grupo_Asset_Ids != null)
            {
                newDatasetAM.Grupo_Asset = new List<DatasetAsset>();
                foreach (var id in request.Grupo_Asset_Ids)
                {
                    newDatasetAM.Grupo_Asset.Add(new DatasetAsset { Id_Asset = id });
                }
            }

            _context.DatasetAM.Add(newDatasetAM);
            await _context.SaveChangesAsync();
            return newDatasetAM;
        }

        public async Task<List<DatasetAM>> GetAllDatasetAMsAsync(string username)
        {
            return await _context.DatasetAM
                .Include(d => d.Grupo_Event_Task_Instance)
                    .ThenInclude(e => e.Grupo_Stock)
                .Include(d => d.Grupo_Asset)
                .Where(d => d.Username == username)
                .ToListAsync();
        }

                /// <summary>
        /// Obtiene un DatasetAM por su ID y nombre de usuario, incluyendo relaciones hijas.
        /// </summary>
        
        public async Task<DatasetAM?> GetDatasetAMByIdAsync(int id, string username)
        {
            if (id < 0)
                throw new ArgumentException("El id debe ser mayor o igual a 0.", nameof(id));

            var datasetAM = await _context.DatasetAM
                .Include(d => d.Grupo_Event_Task_Instance)
                    .ThenInclude(e => e.Grupo_Stock)
                .Include(d => d.Grupo_Asset)
                .FirstOrDefaultAsync(d => d.Id_Dataset == id && d.Username == username);

            if (datasetAM == null)
                return null;

            // Si es un dataset formal ('S') y no tiene relaciones hijas, buscar dinámicamente
            if (datasetAM.Is_Dataset == "S")
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    return datasetAM;
                }

                // Si es de tipo EventTask y no tiene instancias asociadas
                if (datasetAM.Type_Dataset == 1 && (datasetAM.Grupo_Event_Task_Instance == null || !datasetAM.Grupo_Event_Task_Instance.Any()))
                {
                    // Llama correctamente al método del servicio inyectado y usa username/password del contexto
                    var eventTaskInstances = await _sondaAMService.GetEventTaskInstances(
                        "1980-01-01T03:00:00,2050-10-31T03:00:00", // dates
                        null,                                      // page (int?)
                        "",                                        // queryString
                        null,                                      // bundleId
                        "",                                        // state
                        "",                                        // sort
                        datasetAM.Id_Event_Task,                   // taskTypeId
                        null,                                      // groupId
                        null,                                      // pageSize
                        false,                                     // tasksAssignedToMe
                        false,                                     // tasksPendingApproval
                        user.Username,
                        user.Password
                    );
                    if (eventTaskInstances != null && eventTaskInstances.Any())
                    {
                        datasetAM.Grupo_Event_Task_Instance = eventTaskInstances.Select(e => new DatasetEventTaskInstance
                        {
                            Id_Event_Task_Instance = e.Id,
                            Grupo_Stock = new List<DatasetStock>()
                        }).ToList();
                    }
                }
                // Si es de tipo Asset y no tiene assets asociados
                else if (datasetAM.Type_Dataset == 2 && (datasetAM.Grupo_Asset == null || !datasetAM.Grupo_Asset.Any()))
                {
                    // Aquí deberías llamar a la API externa de SondaAM para obtener los assets
                    // Ejemplo (ajusta el método según tu ISondaAMService):
                    // Llamada actualizada para coincidir con la firma de ISondaAMService
                    // page, queryString, bundles, assetTypeId, sort, pageSize, username, password
                    var assets = await _sondaAMService.GetAssets(null, null, null, datasetAM.Id_Asset_Type, null, null, user.Username, user.Password);
                    if (assets != null)
                    {
                        datasetAM.Grupo_Asset = assets.Select(a => new DatasetAsset
                        {
                            Id_Asset = a.Id
                        }).ToList();
                    }
                } else if (datasetAM.Type_Dataset == 1 && datasetAM.Grupo_Event_Task_Instance != null && datasetAM.Grupo_Event_Task_Instance.Count == 1)
                {
                    var eventTaskInstance = datasetAM.Grupo_Event_Task_Instance.First();
                    if (eventTaskInstance.Grupo_Stock == null || !eventTaskInstance.Grupo_Stock.Any())
                    {
                        var userDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                        if (userDb != null)
                        {
                            // Llama a la API externa para obtener stocks asociados a ese event task instance
                            var stocks = await _sondaAMService.GetEventTaskInstanceStock(eventTaskInstance.Id_Event_Task_Instance, userDb.Username, userDb.Password);
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

                /// <summary>
        /// Obtiene un DatasetAM por su ID y nombre de usuario para edición, SIN lógica dinámica.
        /// Devuelve el dataset exactamente como está guardado en la base de datos.
        /// </summary>
        public async Task<DatasetAM?> GetDatasetAMByIdForEditAsync(int id, string username)
        {
            return await _context.DatasetAM
                .Include(d => d.Grupo_Event_Task_Instance)
                    .ThenInclude(e => e.Grupo_Stock)
                .Include(d => d.Grupo_Asset)
                .FirstOrDefaultAsync(d => d.Id_Dataset == id && d.Username == username);
        }

        public async Task<DatasetAM> UpdateDatasetAMAsync(DatasetAM datasetAM, CreateDatasetAMRequest request)
        {

            if (datasetAM == null)
                throw new InvalidOperationException($"No se encontró el DatasetAM con ID {datasetAM.Id_Dataset}.");

            // Validar que no exista otro dataset con el mismo nombre (excluyendo el actual)
            if (datasetAM.Nombre != request.Nombre)
            {
                var duplicateDataset = await _context.DatasetAM
                    .FirstOrDefaultAsync(d => d.Username == datasetAM.Username && 
                                            d.Nombre == datasetAM.Nombre && 
                                            d.Id_Dataset != datasetAM.Id_Dataset);
                
                if (duplicateDataset != null)
                {
                    throw new InvalidOperationException($"Ya existe un dataset con el nombre '{datasetAM.Nombre}' para el usuario '{datasetAM.Username}'.");
                }
            }

            // Actualiza los campos simples
            datasetAM.Nombre = request.Nombre;
            datasetAM.Descripcion = request.Descripcion;
            datasetAM.Type_Dataset = request.Type_Dataset;
            datasetAM.Id_Event_Task = request.Id_Event_Task;
            datasetAM.Id_Asset_Type = request.Id_Asset_Type;
            datasetAM.Is_Dataset = request.IsDataset;
            datasetAM.ContentType = request.ContentType;

            // --- Actualizar Grupo_Event_Task_Instance y sus stocks ---
            // Eliminar los event task instances y stocks existentes
            if (datasetAM.Grupo_Event_Task_Instance != null && datasetAM.Grupo_Event_Task_Instance.Any())
            {
                foreach (var eventTaskInstance in datasetAM.Grupo_Event_Task_Instance)
                {
                    if (eventTaskInstance.Grupo_Stock != null && eventTaskInstance.Grupo_Stock.Any())
                    {
                        _context.Set<DatasetStock>().RemoveRange(eventTaskInstance.Grupo_Stock);
                    }
                }
                _context.Set<DatasetEventTaskInstance>().RemoveRange(datasetAM.Grupo_Event_Task_Instance);
                datasetAM.Grupo_Event_Task_Instance.Clear();
            }

            // Agregar los nuevos event task instances y stocks
            if (request.Grupo_Event_Task_Instance_Ids != null && request.Grupo_Event_Task_Instance_Ids.Any())
            {
                datasetAM.Grupo_Event_Task_Instance = new List<DatasetEventTaskInstance>();
                foreach (var eventTaskInstanceId in request.Grupo_Event_Task_Instance_Ids)
                {
                    var newEventTaskInstance = new DatasetEventTaskInstance
                    {
                        Id_Event_Task_Instance = eventTaskInstanceId,
                        Grupo_Stock = new List<DatasetStock>()
                    };
                    if (request.StockIds != null && request.StockIds.Any())
                    {
                        foreach (var stockId in request.StockIds)
                        {
                            newEventTaskInstance.Grupo_Stock.Add(new DatasetStock { Id_Stock = stockId });
                        }
                    }
                    datasetAM.Grupo_Event_Task_Instance.Add(newEventTaskInstance);
                }
            }

            // --- Actualizar Grupo_Asset ---
            // Eliminar assets existentes
            if (datasetAM.Grupo_Asset != null && datasetAM.Grupo_Asset.Any())
            {
                _context.Set<DatasetAsset>().RemoveRange(datasetAM.Grupo_Asset);
                datasetAM.Grupo_Asset.Clear();
            }

            // Agregar los nuevos assets
            if (request.Grupo_Asset_Ids != null && request.Grupo_Asset_Ids.Any())
            {
                datasetAM.Grupo_Asset = new List<DatasetAsset>();
                foreach (var idAsset in request.Grupo_Asset_Ids)
                {
                    datasetAM.Grupo_Asset.Add(new DatasetAsset { Id_Asset = idAsset });
                }
            }

            _context.DatasetAM.Update(datasetAM);
            await _context.SaveChangesAsync();
            return datasetAM;
        }

        public async Task DeleteDatasetAMAsync(int id, string username)
        {
            var datasetAM = await _context.DatasetAM
                .Include(d => d.Grupo_Event_Task_Instance)
                .ThenInclude(e => e.Grupo_Stock)
                .Include(d => d.Grupo_Asset)
                .FirstOrDefaultAsync(d => d.Id_Dataset == id && d.Username == username);

            if (datasetAM == null)
            {
                throw new InvalidOperationException($"No se encontró el DatasetAM con ID {id} para el usuario {username}.");
            }


            // Eliminar stocks asociados a event task instances (solo los que tienen Id > 0)
            if (datasetAM.Grupo_Event_Task_Instance != null)
            {
                foreach (var eventTaskInstance in datasetAM.Grupo_Event_Task_Instance)
                {
                    if (eventTaskInstance.Grupo_Stock != null && eventTaskInstance.Grupo_Stock.Any())
                    {
                        var stocksToRemove = eventTaskInstance.Grupo_Stock.Where(s => s.Grupo_Stock > 0).ToList();
                        if (stocksToRemove.Any())
                        {
                            _context.Set<DatasetStock>().RemoveRange(stocksToRemove);
                        }
                    }
                }
                var eventTaskInstancesToRemove = datasetAM.Grupo_Event_Task_Instance.Where(e => e.Id > 0).ToList();
                if (eventTaskInstancesToRemove.Any())
                {
                    _context.Set<DatasetEventTaskInstance>().RemoveRange(eventTaskInstancesToRemove);
                }
            }

            // Eliminar assets asociados (solo los que tienen Id > 0)
            if (datasetAM.Grupo_Asset != null && datasetAM.Grupo_Asset.Any())
            {
                var assetsToRemove = datasetAM.Grupo_Asset.Where(a => a.Grupo_Asset > 0).ToList();
                if (assetsToRemove.Any())
                {
                    _context.Set<DatasetAsset>().RemoveRange(assetsToRemove);
                }
            }

            // Eliminar el datasetAM
            _context.DatasetAM.Remove(datasetAM);
            await _context.SaveChangesAsync();
        }


    }
}
