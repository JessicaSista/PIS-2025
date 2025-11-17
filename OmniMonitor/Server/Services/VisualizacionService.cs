using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Azure.Core;
using Azure.Identity;

using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Services
{
    public interface IVisualizacionService
    {
    Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request, string username);
    Task<List<Visualizacion>> GetAllVisualizacionesAsync(string username);
    Task<List<Visualizacion>> GetAllVisualizacionesPaginatedAsync(string username, int page, int pageSize, string? query = null);
    Task<int> GetVisualizacionesCountAsync(string username, string? query = null);
    Task<Visualizacion?> GetVisualizacionByIdAsync(int idVisualizacion, string username);
    Task<Visualizacion?> GetVisualizacionByIdAsyncSinToken(int idVisualizacion);
    Task<VisualizationResponse> GetVisualizationDataAsync(VisualizationRequest req, string username);
    Task<VisualizationResponse> GetVisualizationDataSinTokenAsync(VisualizationRequest req);
    }

    public class VisualizacionService : IVisualizacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IApiDataService _apiDataService;
        public VisualizacionService(ApplicationDbContext context, IApiDataService apiDataService)
        {
            _context = context;
            _apiDataService = apiDataService;

        }

        private async Task EnsureUniqueNameAsync(string name, string username, int? excludeId = null)
        {
            var query = _context.Visualizaciones
                .Where(v => v.Username == username && v.Nombre == name);

            if (excludeId.HasValue)
            {
                query = query.Where(v => v.IdVisualizacion != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                throw new ArgumentException($"Ya existe una visualización con el nombre '{name}'.");
            }
        }

        /// <summary>
        /// Crea una nueva visualización y asocia los datasets correspondientes.
        /// </summary>
        public async Task<Visualizacion> CreateVisualizacionAsync(CreateVisualizacionRequest request, string username)
        {
            if (string.IsNullOrEmpty(request.Nombre))
            {
                throw new ArgumentException("El nombre de usuario y el nombre de la visualización son obligatorios.");
            }

            await EnsureUniqueNameAsync(request.Nombre, username);

            var nuevaVisualizacion = new Visualizacion
            {
                Nombre = request.Nombre,
                Username = username,
                FechaDesde = request.FechaDesde,
                FechaHasta = request.FechaHasta,
                JsonDesign = request.JsonDiseñoGeneral,
                Link = request.Link
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
            .Include(v => v.GrupoDatasets)           // ? Incluye los GrupoDatasets
                .ThenInclude(gd => gd.Dataset)       // ? Incluye los Datasets dentro de cada GrupoDataset
            .Where(v => v.Username == username)
            .OrderByDescending(v => v.IdVisualizacion)
            .ToListAsync();
        }

        
        public async Task<List<Visualizacion>> GetAllVisualizacionesPaginatedAsync(string username, int page, int pageSize, string? query = null)
        {
            var visualizacionesQuery = _context.Visualizaciones.Include(v => v.GrupoDatasets).ThenInclude(gd => gd.Dataset).Where(v => v.Username == username);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var loweredQuery = query.ToLowerInvariant();
                visualizacionesQuery = visualizacionesQuery.Where(v =>
                    (v.Nombre != null && v.Nombre.ToLower().Contains(loweredQuery)));
            }
            return await visualizacionesQuery
                .OrderByDescending(v => v.IdVisualizacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetVisualizacionesCountAsync(string username, string? query = null)
        {
            var visualizacionesQuery = _context.Visualizaciones.Where(v => v.Username == username);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var loweredQuery = query.ToLowerInvariant();
                visualizacionesQuery = visualizacionesQuery.Where(v =>
                    (v.Nombre != null && v.Nombre.ToLower().Contains(loweredQuery)));
            }
            return await visualizacionesQuery.CountAsync();
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
