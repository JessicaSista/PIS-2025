using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
    }

    #endregion

    #region Implementación

    /// <summary>
    /// Servicio para la gestión de visualizaciones.
    /// </summary>
    public class VisualizacionService : IVisualizacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisualizacionService> _logger;

        /// <summary>
        /// Constructor del servicio de visualizaciones.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="logger">Logger para registrar información.</param>
        public VisualizacionService(ApplicationDbContext context, ILogger<VisualizacionService> logger)
        {
            _context = context;
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

        #endregion
    }

    #endregion
}