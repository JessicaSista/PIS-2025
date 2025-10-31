using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using OmniMonitor.Shared.Dtos.EM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Servicio para la gestión de datasets de Event Manager (EM).
    /// </summary>
    public interface IDatasetEMService
    {
        /// <summary>
        /// Crea un nuevo dataset EM.
        /// </summary>
        /// <param name="request">Datos para la creación del dataset.</param>
        /// <param name="dataset">Identificador del dataset padre.</param>
        /// <returns>El dataset EM creado.</returns>
        Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request, int dataset);

        /// <summary>
        /// Obtiene todos los datasets EM de un usuario.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de datasets EM asociados al usuario.</returns>
        Task<List<DatasetEM>> GetAllDatasetsEMAsync(string username);

        /// <summary>
        /// Obtiene un dataset EM por su ID y nombre de usuario, aplicando la lógica de carga dinámica.
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset EM encontrado o null si no existe.</returns>
        Task<DatasetEM?> GetDatasetEMByIdAsync(int datasetId, string username);

        /// <summary>
        /// Obtiene un dataset EM por su ID y nombre de usuario para edición (sin carga dinámica).
        /// </summary>
        /// <param name="datasetId">ID del dataset.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dataset EM encontrado o null si no existe.</returns>
        Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username);

        /// <summary>
        /// Elimina un dataset EM.
        /// </summary>
        /// <param name="datasetId">ID del dataset a eliminar.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Una tarea que representa la operación de eliminación.</returns>
        Task DeleteDatasetEMAsync(int datasetId, string username);

        /// <summary>
        /// Actualiza un dataset EM existente.
        /// </summary>
        /// <param name="datasetId">ID del dataset a actualizar.</param>
        /// <param name="request">Datos nuevos para la actualización.</param>
        /// <returns>El dataset EM actualizado.</returns>
        Task<DatasetEM> UpdateDatasetEMAsync(int datasetId, CreateDatasetEMRequest request);
    }

    #endregion

    #region Classes

    /// <summary>
    /// Implementación del servicio para la gestión de datasets de Event Manager (EM).
    /// </summary>
    public class DatasetEMService : IDatasetEMService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISondaEMService _sondaEMService;
        private readonly ILogger<DatasetEMService> _logger;

        /// <summary>
        /// Constructor del servicio DatasetEMService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="sondaEMService">Servicio de Sonda EM.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public DatasetEMService(ApplicationDbContext context, ISondaEMService sondaEMService, ILogger<DatasetEMService> logger)
        {
            _context = context;
            _sondaEMService = sondaEMService;
            _logger = logger;
        }

        #region Métodos públicos

        /// <inheritdoc />
        public async Task<DatasetEM> CreateDatasetEMAsync(CreateDatasetEMRequest request, int dataset)
        {
            try
            {
                _logger.LogInformation("Creando DatasetEM '{Name}' para usuario {Username}", request.Name, request.Username);

                await ValidateDuplicateName(request.Name, request.Username);

                DatasetEM newDataset = new()
                {
                    Name = request.Name,
                    Description = request.Description,
                    Username = request.Username,
                    Is_Dataset = request.IsDataset,
                    DatasetId = dataset,
                    ContentType = GetContentType(request).ToString()
                };

                _context.DatasetsEM.Add(newDataset);
                await _context.SaveChangesAsync();

                UpdateRelationsFromRequest(newDataset, request);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetEM '{Name}' creado correctamente para usuario {Username}", request.Name, request.Username);

                return newDataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando DatasetEM '{Name}' para usuario {Username}", request.Name, request.Username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<List<DatasetEM>> GetAllDatasetsEMAsync(string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los DatasetEM para usuario {Username}", username);

                List<DatasetEM> result = await _context.DatasetsEM
                    .AsNoTracking()
                    .Where(d => string.Equals(d.Username, username, StringComparison.Ordinal))
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                _logger.LogInformation("Se obtuvieron {Count} datasets EM para el usuario {Username}.", result.Count, username);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los DatasetEM para usuario {Username}", username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetEM?> GetDatasetEMByIdAsync(int datasetId, string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);

                DatasetEM? dataset = await _context.DatasetsEM
                    .AsNoTracking()
                    .Include(d => d.DatasetAlerts)
                    .Include(d => d.DatasetEvents)
                    .Include(d => d.DatasetExtensions)
                    .Include(d => d.DatasetCategory)
                    .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username, StringComparison.Ordinal));

                if (dataset == null)
                {
                    _logger.LogWarning("No se encontró el DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);
                    return null;
                }

                // Lógica de carga dinámica (comentada)
                // ...

                return dataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetEM?> GetDatasetEMByIdForEditAsync(int datasetId, string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo DatasetEM para edición con ID {DatasetId} para usuario {Username}", datasetId, username);

                DatasetEM? dataset = await _context.DatasetsEM
                    .AsNoTracking()
                    .Include(d => d.DatasetAlerts)
                    .Include(d => d.DatasetEvents)
                    .Include(d => d.DatasetExtensions)
                    .Include(d => d.DatasetCategory)
                    .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username, StringComparison.Ordinal));

                return dataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo DatasetEM para edición con ID {DatasetId} para usuario {Username}", datasetId, username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<DatasetEM> UpdateDatasetEMAsync(int datasetId, CreateDatasetEMRequest request)
        {
            try
            {
                _logger.LogInformation("Actualizando DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, request.Username);

                DatasetEM? existingDataset = await _context.DatasetsEM
                    .Include(d => d.DatasetAlerts)
                    .Include(d => d.DatasetEvents)
                    .Include(d => d.DatasetExtensions)
                    .Include(d => d.DatasetCategory)
                    .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, request.Username, StringComparison.Ordinal));

                if (existingDataset == null)
                {
                    _logger.LogWarning("No se encontró el DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, request.Username);
                    throw new InvalidOperationException($"No se encontró el dataset con ID '{datasetId}' para el usuario '{request.Username}'.");
                }

            // La validación de nombres duplicados se hace en la tabla general (UpdateDatasetAsyncEM)
            // para garantizar unicidad global entre todos los módulos

                existingDataset.Name = request.Name;
                existingDataset.Description = request.Description;
                existingDataset.Is_Dataset = request.IsDataset;
                existingDataset.ContentType = GetContentType(request).ToString();

                _context.DatasetAlerts.RemoveRange(existingDataset.DatasetAlerts);
                _context.DatasetEventsEM.RemoveRange(existingDataset.DatasetEvents);
                _context.DatasetExtensions.RemoveRange(existingDataset.DatasetExtensions);
                _context.DatasetCategory.RemoveRange(existingDataset.DatasetCategory);

                existingDataset.DatasetAlerts.Clear();
                existingDataset.DatasetEvents.Clear();
                existingDataset.DatasetExtensions.Clear();
                existingDataset.DatasetCategory.Clear();

                UpdateRelationsFromRequest(existingDataset, request);

                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetEM con ID {DatasetId} actualizado correctamente para usuario {Username}", datasetId, request.Username);

                return existingDataset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, request.Username);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeleteDatasetEMAsync(int datasetId, string username)
        {
            try
            {
                _logger.LogInformation("Eliminando DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);

                DatasetEM? dataset = await _context.DatasetsEM
                    .FirstOrDefaultAsync(d => d.Id == datasetId && string.Equals(d.Username, username, StringComparison.Ordinal));

                if (dataset == null)
                {
                    _logger.LogWarning("No se encontró el DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);
                    throw new InvalidOperationException($"No se encontró el dataset con ID '{datasetId}' para el usuario '{username}'.");
                }

                _context.DatasetsEM.Remove(dataset);
                await _context.SaveChangesAsync();

                _logger.LogInformation("DatasetEM con ID {DatasetId} eliminado correctamente para usuario {Username}", datasetId, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando DatasetEM con ID {DatasetId} para usuario {Username}", datasetId, username);
                throw;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Actualiza las relaciones del dataset EM a partir de la request.
        /// </summary>
        /// <param name="dataset">Entidad DatasetEM a actualizar.</param>
        /// <param name="request">Request con los IDs de las entidades relacionadas.</param>
        private static void UpdateRelationsFromRequest(DatasetEM dataset, CreateDatasetEMRequest request)
        {
            if (request.AlertIds != null && request.AlertIds.Any())
            {
                dataset.DatasetAlerts = request.AlertIds.Select(id => new DatasetAlert
                {
                    DatasetId = dataset.Id,
                    Id_alert = id
                }).ToList();
            }

            if (request.EventIds != null && request.EventIds.Any())
            {
                dataset.DatasetEvents = request.EventIds.Select(id => new DatasetEventEM
                {
                    DatasetId = dataset.Id,
                    Id_event = id
                }).ToList();
            }


            if (request.ExtensionIds?.Any() == true)
            {
                dataset.DatasetExtensions = request.ExtensionIds.Select(id => new DatasetExtension
                {
                    DatasetId = dataset.Id,
                    Id_extension = id
                }).ToList();
            }

            if (request.CategoryIds?.Any() == true)
            {
                dataset.DatasetCategory = request.CategoryIds.Select(id => new DatasetCategory
                {
                    DatasetId = dataset.Id,
                    Id_Category = id
                }).ToList();
            }
        }

        private static DatasetContentType GetContentType(CreateDatasetEMRequest r)
        {
            DatasetContentType type = DatasetContentType.None;
            if (r.AlertIds?.Any() == true) type |= DatasetContentType.Alerts;
            if (r.EventIds?.Any() == true) type |= DatasetContentType.Events;
            if (r.ExtensionIds?.Any() == true) type |= DatasetContentType.Extensions;
            if (r.CategoryIds?.Any() == true) type |= DatasetContentType.Category;
            return type;
        }

        private async Task ValidateDuplicateName(string name, string username, int? excludeId = null)
        {
            var query = _context.DatasetsEM
                .Where(d => d.Name == name && d.Username == username);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            if (await query.AnyAsync())
                throw new InvalidOperationException($"Ya existe un dataset con el nombre '{name}' para el usuario '{username}'.");
        }
    }

    [Flags]
    public enum DatasetContentType
    {
        None = 0,
        Alerts = 1,
        Events = 2,
        Extensions = 4,
        Category = 8
    }
}
#endregion
#endregion