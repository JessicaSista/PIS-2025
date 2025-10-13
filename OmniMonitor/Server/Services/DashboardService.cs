using Microsoft.EntityFrameworkCore;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para el servicio de Dashboards
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardResponse> CreateDashboardAsync(CreateDashboardRequest request, string username);
        Task<DashboardResponse?> GetDashboardByIdAsync(int idDashboard, string username);
        Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username);
        Task<bool> ValidateCardIdsAsync(List<int> cardIds);
        Task<bool> ValidateLayoutAsync(DashboardLayout layout);
    }

    /// <summary>
    /// Implementación del servicio de Dashboards
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea un nuevo dashboard con sus tarjetas configuradas
        /// </summary>
        public async Task<DashboardResponse> CreateDashboardAsync(CreateDashboardRequest request, string username)
        {
            // Validar que el usuario no tenga ya un dashboard con el mismo nombre
            var existingDashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.Username == username && d.Nombre == request.Nombre);

            if (existingDashboard != null)
            {
                throw new ArgumentException($"Ya existe un dashboard con el nombre '{request.Nombre}' para el usuario '{username}'.");
            }

            // Validar cardIds (IdVisualizacion) si se proporcionan
            if (request.Layout?.Tarjetas != null && request.Layout.Tarjetas.Any())
            {
                var cardIds = request.Layout.Tarjetas.Select(t => t.CardId).ToList();
                if (!await ValidateCardIdsAsync(cardIds))
                {
                    throw new ArgumentException("Uno o más IdVisualizacion no existen en el sistema.");
                }

                // Validar layout
                if (!await ValidateLayoutAsync(request.Layout))
                {
                    throw new ArgumentException("El layout contiene superposiciones inválidas o configuraciones fuera de rango.");
                }
            }

            // Crear el dashboard
            var nuevoDashboard = new Dashboard
            {
                Username = username,
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                JsonDiseno = request.Layout != null ? JsonSerializer.Serialize(request.Layout) : null,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Dashboards.Add(nuevoDashboard);
            await _context.SaveChangesAsync();

            // Agregar las tarjetas si se proporcionan
            if (request.Layout?.Tarjetas != null && request.Layout.Tarjetas.Any())
            {
                foreach (var tarjeta in request.Layout.Tarjetas)
                {
                    var grupoVisualizacion = new GrupoVisualizacion
                    {
                        GrupoVisualizacionId = nuevoDashboard.IdDashboard,
                        IdVisualizacion = tarjeta.CardId,
                        PosicionX = tarjeta.PosicionX,
                        PosicionY = tarjeta.PosicionY,
                        Ancho = tarjeta.Ancho,
                        Alto = tarjeta.Alto,
                        PropsConfiguracion = tarjeta.Props != null ? JsonSerializer.Serialize(tarjeta.Props) : null,
                        FechaAgregado = DateTime.UtcNow
                    };

                    _context.GrupoVisualizaciones.Add(grupoVisualizacion);
                }

                await _context.SaveChangesAsync();
            }

            // Retornar el dashboard completo
            return await GetDashboardByIdAsync(nuevoDashboard.IdDashboard, username) 
                ?? throw new InvalidOperationException("Error al recuperar el dashboard creado.");
        }

        /// <summary>
        /// Obtiene un dashboard por su ID con todas sus tarjetas
        /// </summary>
        public async Task<DashboardResponse?> GetDashboardByIdAsync(int idDashboard, string username)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                    .ThenInclude(gv => gv.Visualizacion)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);

            if (dashboard == null)
                return null;

            var response = new DashboardResponse
            {
                IdDashboard = dashboard.IdDashboard,
                Username = dashboard.Username,
                Nombre = dashboard.Nombre,
                Descripcion = dashboard.Descripcion,
                GrupoVisualizacion = dashboard.GrupoVisualizacion,
                JsonDiseno = dashboard.JsonDiseno,
                FechaCreacion = dashboard.FechaCreacion,
                FechaModificacion = dashboard.FechaModificacion
            };

            // Deserializar el layout si existe
            if (!string.IsNullOrEmpty(dashboard.JsonDiseno))
            {
                try
                {
                    response.Layout = JsonSerializer.Deserialize<DashboardLayout>(dashboard.JsonDiseno);
                }
                catch (JsonException)
                {
                    // Si hay error en la deserialización, se deja como null
                    response.Layout = null;
                }
            }

            // Mapear las tarjetas
            response.Tarjetas = dashboard.GrupoVisualizaciones.Select(gv => new DashboardCardResponse
            {
                IdGrupoVisualizacion = gv.IdGrupoVisualizacion,
                CardId = gv.IdVisualizacion,
                PosicionX = gv.PosicionX,
                PosicionY = gv.PosicionY,
                Ancho = gv.Ancho,
                Alto = gv.Alto,
                PropsConfiguracion = gv.PropsConfiguracion,
                FechaAgregado = gv.FechaAgregado,
                Visualizacion = gv.Visualizacion != null ? new VisualizacionInfo
                {
                    IdVisualizacion = gv.Visualizacion.IdVisualizacion,
                    Nombre = gv.Visualizacion.Nombre,
                    FechaDesde = gv.Visualizacion.FechaDesde,
                    FechaHasta = gv.Visualizacion.FechaHasta,
                    JsonDesign = gv.Visualizacion.JsonDesign
                } : null
            }).ToList();

            return response;
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario
        /// </summary>
        public async Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username)
        {
            return await _context.Dashboards
                .Where(d => d.Username == username)
                .Select(d => new DashboardSummaryResponse
                {
                    IdDashboard = d.IdDashboard,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion,
                    FechaCreacion = d.FechaCreacion,
                    FechaModificacion = d.FechaModificacion,
                    CantidadTarjetas = d.GrupoVisualizaciones.Count
                })
                .OrderByDescending(d => d.FechaModificacion)
                .ToListAsync();
        }

        /// <summary>
        /// Valida que todos los cardIds (IdVisualizacion) existan en el sistema
        /// </summary>
        public async Task<bool> ValidateCardIdsAsync(List<int> cardIds)
        {
            if (cardIds == null || !cardIds.Any())
                return true;

            var existingIds = await _context.Visualizaciones
                .Where(v => cardIds.Contains(v.IdVisualizacion))
                .Select(v => v.IdVisualizacion)
                .ToListAsync();

            return existingIds.Count == cardIds.Count;
        }

        /// <summary>
        /// Valida que el layout no tenga superposiciones inválidas
        /// </summary>
        public async Task<bool> ValidateLayoutAsync(DashboardLayout layout)
        {
            if (layout?.Tarjetas == null || !layout.Tarjetas.Any())
                return true;

            // Validar que no haya superposiciones
            var tarjetas = layout.Tarjetas.ToList();
            for (int i = 0; i < tarjetas.Count; i++)
            {
                for (int j = i + 1; j < tarjetas.Count; j++)
                {
                    if (TarjetasSeSuperponen(tarjetas[i], tarjetas[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica si dos tarjetas se superponen
        /// </summary>
        private bool TarjetasSeSuperponen(DashboardCard tarjeta1, DashboardCard tarjeta2)
        {
            var x1 = tarjeta1.PosicionX;
            var y1 = tarjeta1.PosicionY;
            var w1 = tarjeta1.Ancho;
            var h1 = tarjeta1.Alto;

            var x2 = tarjeta2.PosicionX;
            var y2 = tarjeta2.PosicionY;
            var w2 = tarjeta2.Ancho;
            var h2 = tarjeta2.Alto;

            return !(x1 + w1 <= x2 || x2 + w2 <= x1 || y1 + h1 <= y2 || y2 + h2 <= y1);
        }
    }
}
