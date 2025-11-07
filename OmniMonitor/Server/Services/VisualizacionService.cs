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
    // --- Interfaz para el servicio de Visualizaciones ---
    public interface IVisualizacionService
    {
        Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request);
        Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username);
    Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username);
    Task<Visualizacion?> GetVisualizacionByIdAsyncSinToken(int idVisualizacion);
        Task<VisualizationResponse> GetVisualizationDataAsync(VisualizationRequest req, string username);
        Task<VisualizationResponse> GetVisualizationDataSinTokenAsync(VisualizationRequest req);
    }

    // --- Implementación del servicio ---
    public class VisualizacionService : IVisualizacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IApiDataService _apiDataService;
        public VisualizacionService(ApplicationDbContext context, IApiDataService apiDataService)
        {
            _context = context;
            _apiDataService = apiDataService;

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
            return await _context.Visualizaciones
                .Include(v => v.GrupoDatasets)
                    .ThenInclude(gd => gd.Dataset)
                .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion && v.Username == username);
        }

        public async Task<Visualizacion?> GetVisualizacionByIdAsyncSinToken(int idVisualizacion)
        {
             return await _context.Visualizaciones
                .Include(v => v.GrupoDatasets)
                    .ThenInclude(gd => gd.Dataset)
                .FirstOrDefaultAsync(v => v.IdVisualizacion == idVisualizacion);
        }

        public async Task<VisualizationResponse> GetVisualizationDataAsync(VisualizationRequest req, string username)
        {
            var operand = new JoinOperand
            {
                ModuleType = req.moduleType,
                DatasetId = req.datasetId,
                EntityName = req.entity,
                JoinPropertyName = string.Empty
            };

            var data = await _apiDataService.GetDataForOperand(operand, username);
            if (data == null || !data.Any())
                return new VisualizationResponse { Type = "unknown", Values = new () };

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
                if (typeName == "unknown" && value is not null)
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

        public async Task<VisualizationResponse> GetVisualizationDataSinTokenAsync(VisualizationRequest req)
        {
            var operand = new JoinOperand
            {
                ModuleType = req.moduleType,
                DatasetId = req.datasetId,
                EntityName = req.entity,
                JoinPropertyName = string.Empty
            };

            var data = await _apiDataService.GetDataForOperandSinToken(operand);
            if (data == null || !data.Any())
                return new VisualizationResponse { Type = "unknown", Values = new () };

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
                if (typeName == "unknown" && value is not null)
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
    }
}