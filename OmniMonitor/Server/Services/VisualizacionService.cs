using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Azure.Core;

using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Interfaz para el servicio de visualizaciones.
    /// </summary>
    public interface IVisualizacionService
    {
        /// <summary>
        /// Crea una nueva visualización y asocia los datasets correspondientes.
        /// </summary>
        /// <param name="request">Datos para la creación de la visualización.</param>
        /// <returns>La visualización creada.</returns>
        Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request);

        /// <summary>
        /// Obtiene todas las visualizaciones de un usuario específico.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Lista de visualizaciones.</returns>
        Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username);

        /// <summary>
        /// Obtiene una visualización por su ID, incluyendo los datasets asociados.
        /// </summary>
        /// <param name="idVisualizacion">ID de la visualización.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>Visualización o null.</returns>
        Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username);

        Task<VisualizationResponse> GetVisualizationDataAsync(VisualizationRequest req, string username);
    }

    #endregion

    #region Implementación

    /// <summary>
    /// Servicio para la gestión de visualizaciones.
    /// </summary>
    public class VisualizacionService : IVisualizacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IApiDataService _apiDataService;
        public VisualizacionService(ApplicationDbContext context, IApiDataService apiDataService)
        private readonly ILogger<VisualizacionService> _logger;

        /// <summary>
        /// Constructor del servicio de visualizaciones.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="logger">Logger para registrar información.</param>
        public VisualizacionService(ApplicationDbContext context, ILogger<VisualizacionService> logger)
        {
            _context = context;
            _apiDataService = apiDataService;

            _logger = logger;
        }

        #region Métodos Públicos

        /// <inheritdoc/>
        public async Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Nombre))
            {
                _logger.LogWarning("El nombre de usuario o el nombre de la visualización es nulo o vacío.");
                throw new ArgumentException("El nombre de usuario y el nombre de la visualización son obligatorios.");
            }

            var nuevaVisualizacion = new Visualizacion
            {
                Nombre = request.Nombre,
                Username = request.Username,
                FechaDesde = request.FechaDesde,
                FechaHasta = request.FechaHasta,
                JsonDesign = request.JsonDiseñoGeneral,
                GrupoDatasets = new List<GrupoDataset>()
            };

            // Añadir los datasets asociados a la visualización
            if (request.Datasets != null && request.Datasets.Any())
            {
                foreach (var datasetConfig in request.Datasets)
                {
                    nuevaVisualizacion.GrupoDatasets.Add(new GrupoDataset
                    {
                        DatasetId = datasetConfig.DatasetId,
                        JsonDesign = datasetConfig.JsonDiseño
                    });
                }
            }

            _context.Visualizaciones.Add(nuevaVisualizacion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Visualización creada correctamente para el usuario {Username} con nombre {Nombre}.", request.Username, request.Nombre);

            return nuevaVisualizacion;
        }

        /// <inheritdoc/>
        public async Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
                return new();
            }

            return await _context.Visualizaciones
                .AsNoTracking()
                .Include(v => v.GrupoDatasets)
                    .ThenInclude(gd => gd.Dataset)
                .Where(v => v.Username == username)
                .OrderByDescending(v => v.IdVisualizacion)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("El parámetro 'username' está vacío o es nulo.");
                return null;
            }

            return await _context.Visualizaciones
                .AsNoTracking()
                .Include(v => v.GrupoDatasets)
                    .ThenInclude(gd => gd.Dataset)
                .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion && v.Username == username);
        }


        public async Task<VisualizationResponse> GetVisualizationDataAsync(VisualizationRequest req, string username)
        {
            var operand = new JoinOperand
            {
                ModuleType = req.moduleType,
                DatasetId = req.datasetId,
                EntityName = req.entity,
                JoinPropertyName = null
            };

            var data = await _apiDataService.GetDataForOperand(operand, username);
            if (data == null || !data.Any())
                return new VisualizationResponse { Type = "unknown", Values = new() };

            var counts = new Dictionary<string, int>();
            string typeName = "unknown";

            foreach (var item in data)
            {
                if (item == null)
                    continue;

                // Usar reflection para obtener la propiedad
                var prop = item.GetType().GetProperty(req.column);
                if (prop == null)
                    continue;

                var value = prop.GetValue(item);

                // Detectar tipo la primera vez
                if (typeName == "unknown" && value != null)
                    typeName = value.GetType().Name;

                string key = value?.ToString() ?? "null";

                if (counts.ContainsKey(key))
                    counts[key]++;
                else
                    counts[key] = 1;
            }

            return new VisualizationResponse
            {
                Type = typeName,
                Values = counts.Select(kv => new VisualizationValue
                {
                    Name = kv.Key,
                    Value = kv.Value
                }).ToList()
            };
        }







        #endregion
    }

    #endregion
}