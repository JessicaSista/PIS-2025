using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    // --- Interfaz para el servicio de Visualizaciones ---
    public interface IVisualizacionService
    {
        Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request);
        Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username);
        Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username);
    }

    // --- Implementación del servicio ---
    public class VisualizacionService : IVisualizacionService
    {
        private readonly ApplicationDbContext _context;

        public VisualizacionService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea una nueva visualización y asocia los datasets correspondientes.
        /// </summary>
        public async Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Nombre))
            {
                throw new ArgumentException("El nombre de usuario y el nombre de la visualización son obligatorios.");
            }

            var nuevaVisualizacion = new Visualizacion
            {
                Nombre = request.Nombre,
                Username = request.Username,
                FechaDesde = request.FechaDesde,
                FechaHasta = request.FechaHasta,
                JsonDesign = request.JsonDiseñoGeneral
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

            return nuevaVisualizacion;
        }

        /// <summary>
        /// Obtiene todas las visualizaciones de un usuario específico.
        /// </summary>
        public async Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username)
        {
            return await _context.Visualizaciones
            .Include(v => v.GrupoDatasets)           // ← Incluye los GrupoDatasets
                .ThenInclude(gd => gd.Dataset)       // ← Incluye los Datasets dentro de cada GrupoDataset
            .Where(v => v.Username == username)
            .OrderByDescending(v => v.IdVisualizacion)
            .ToListAsync();
        }

        /// <summary>
        /// Obtiene una visualización por su ID, incluyendo los datasets asociados.
        /// </summary>
        public async Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username)
        {
            // Se usa Include y ThenInclude para cargar los datos relacionados
            return await _context.Visualizaciones
                .Include(v => v.GrupoDatasets)
                    .ThenInclude(gd => gd.Dataset)
                .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion && v.Username == username);
        }
    }
}