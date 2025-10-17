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
        Task<bool> DeleteDashboardAsync(int idDashboard, string username);
        Task<bool> UpdateDashboardConfigAsync(int idDashboard, string username, string nuevoJsonDiseno);
        Task<bool> AddDashboardCardAsync(int idDashboard, string username, DashboardCard nuevaCard);
        Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, List<DashboardCard> orderedCards);
        Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idGrupoVisualizacion);
        Task<DashboardResponse?> UpdateDashboardInfoAsync(int idDashboard, string username, string? nuevoNombre, string? nuevaDescripcion);
        Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, string nombre, CreateVisualizacionRequest updatedCard);
        Task<List<DashboardSummaryResponse>> SearchDashboardsByTextAsync(string query);
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

            }

            // Crear el dashboard
            var nuevoDashboard = new DashboardDto
            {
                Username = username,
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                JsonDiseno = request.Layout?.Configuracion != null ? JsonSerializer.Serialize(request.Layout.Configuracion) : null,
                FechaCreacion = DateTime.UtcNow,
                FechaModificacion = DateTime.UtcNow
            };

            _context.Dashboards.Add(nuevoDashboard);
            await _context.SaveChangesAsync();

            // Agregar las tarjetas si se proporcionan
            if (request.Layout?.Tarjetas != null && request.Layout.Tarjetas.Any())
            {
                foreach (var tarjeta in request.Layout.Tarjetas.Select((t, idx) => new { t, idx }))
                {
                    var grupoVisualizacion = new GrupoVisualizacion
                    {
                        GrupoVisualizacionId = nuevoDashboard.IdDashboard,
                        IdVisualizacion = tarjeta.t.CardId,
                        PropsConfiguracion = tarjeta.t.Props.HasValue ? JsonSerializer.Serialize(tarjeta.t.Props.Value) : null,
                        FechaAgregado = DateTime.UtcNow,
                        TipoCard = tarjeta.t.TipoCard,
                        Orden = tarjeta.idx + 1
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
            /*if (!string.IsNullOrEmpty(dashboard.JsonDiseno))
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
            }*/

            // Mapear las tarjetas
            response.Tarjetas = dashboard.GrupoVisualizaciones
                .OrderBy(gv => gv.Orden)
                .Select(gv => new DashboardCardResponse
                {
                    IdGrupoVisualizacion = gv.IdGrupoVisualizacion,
                    CardId = gv.IdVisualizacion,
                    TipoCard = gv.TipoCard,
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
            /*for (int i = 0; i < tarjetas.Count; i++)
            {
                for (int j = i + 1; j < tarjetas.Count; j++)
                {
                    if (TarjetasSeSuperponen(tarjetas[i], tarjetas[j]))
                    {
                        return false;
                    }
                }
            }*/

            return true;
        }

        /// <summary>
        /// Verifica si dos tarjetas se superponen
        /// </summary>
        /*private bool TarjetasSeSuperponen(DashboardCard tarjeta1, DashboardCard tarjeta2)
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
        }*/

        /// <summary>
        /// Elimina un dashboard y todos sus GrupoVisualizaciones asociados (no elimina visualizaciones/KPIs)
        /// </summary>
        public async Task<bool> DeleteDashboardAsync(int idDashboard, string username)
        {
            // Buscar el dashboard del usuario
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);

            if (dashboard == null)
                return false;

            // Eliminar los GrupoVisualizaciones asociados
            _context.GrupoVisualizaciones.RemoveRange(dashboard.GrupoVisualizaciones);
            // Eliminar el dashboard
            _context.Dashboards.Remove(dashboard);

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Actualiza el JSON de configuración (JsonDiseno) de un dashboard
        /// </summary>
        public async Task<bool> UpdateDashboardConfigAsync(int idDashboard, string username, string nuevoJsonDiseno)
        {
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            dashboard.JsonDiseno = nuevoJsonDiseno;
            dashboard.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Agrega una tarjeta (DashboardCard) a un dashboard existente
        /// </summary>
        public async Task<bool> AddDashboardCardAsync(int idDashboard, string username, DashboardCard nuevaCard)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            // Validar según el tipo de tarjeta
            if (nuevaCard.TipoCard == 1)
            {
                // Validar que el CardId exista en Visualizaciones
                bool visualizacionExiste = await _context.Visualizaciones.AnyAsync(v => v.IdVisualizacion == nuevaCard.CardId);
                if (!visualizacionExiste)
                    throw new ArgumentException($"No existe una visualización con Id {nuevaCard.CardId}");
            }
            else if (nuevaCard.TipoCard == 2)
            {
                // TODO: Validar que el KPI exista cuando se implemente la entidad correspondiente
                // bool kpiExiste = await _context.KPIs.AnyAsync(k => k.IdKPI == nuevaCard.CardId);
                // if (!kpiExiste) return false;
            }

            // Chequear si la tarjeta ya existe en el dashboard (por IdVisualizacion y TipoCard)
            bool cardExists = dashboard.GrupoVisualizaciones.Any(gv =>
                gv.IdVisualizacion == nuevaCard.CardId &&
                gv.TipoCard == nuevaCard.TipoCard);
            if (cardExists)
                throw new ArgumentException("Tarjeta duplicada: ya existe una tarjeta con ese IdVisualizacion y TipoCard en el dashboard.");

            // Calcular el orden para la nueva tarjeta
            int orden = _context.GrupoVisualizaciones
                .Where(gv => gv.GrupoVisualizacionId == idDashboard)
                .Select(gv => (int?)gv.Orden)
                .Max() ?? 0;
            orden++;

            var grupoVisualizacion = new GrupoVisualizacion
            {
                GrupoVisualizacionId = idDashboard,
                IdVisualizacion = nuevaCard.CardId,
                TipoCard = nuevaCard.TipoCard,
                PropsConfiguracion = nuevaCard.Props.HasValue ? JsonSerializer.Serialize(nuevaCard.Props.Value) : null,
                FechaAgregado = DateTime.UtcNow,
                Orden = orden
            };
            _context.GrupoVisualizaciones.Add(grupoVisualizacion);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Reordena las tarjetas (GrupoVisualizaciones) de un dashboard según el orden de la lista recibida
        /// </summary>
        public async Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, List<DashboardCard> orderedCards)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            for (int i = 0; i < orderedCards.Count; i++)
            {
                var card = orderedCards[i];
                var grupo = dashboard.GrupoVisualizaciones.FirstOrDefault(gv =>
                    gv.GrupoVisualizacionId == idDashboard &&
                    gv.IdVisualizacion == card.CardId &&
                    gv.TipoCard == card.TipoCard);
                if (grupo != null)
                {
                    grupo.Orden = i + 1;
                }
            }
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Elimina una tarjeta (GrupoVisualizacion) de un dashboard y actualiza el orden de las restantes
        /// </summary>
        public async Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idGrupoVisualizacion)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            var cardToRemove = dashboard.GrupoVisualizaciones.FirstOrDefault(gv => gv.IdGrupoVisualizacion == idGrupoVisualizacion);
            if (cardToRemove == null)
                return false;

            int ordenEliminado = cardToRemove.Orden;
            _context.GrupoVisualizaciones.Remove(cardToRemove);

            // Actualizar el orden de las tarjetas que estaban después
            var cardsToUpdate = dashboard.GrupoVisualizaciones.Where(gv => gv.Orden > ordenEliminado).ToList();
            foreach (var card in cardsToUpdate)
            {
                card.Orden--;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Actualiza el nombre y/o la descripción de un dashboard
        /// </summary>
        public async Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, string nombre, CreateVisualizacionRequest updatedCard)
        {
            // Implementación de otro método si es necesario

            var visualizacion = await _context.Visualizaciones
                .FirstOrDefaultAsync(v => v.Nombre == nombre && v.Username == username);

            if (visualizacion == null)
            {
                return false;
            }

            // Actualizar configuración de la card en el grupo visualización
            var grupovisu = await _context.GrupoVisualizaciones
                .FirstOrDefaultAsync(gv => gv.GrupoVisualizacionId == idDashboard && gv.IdVisualizacion == visualizacion.IdVisualizacion);

            if (grupovisu == null)
            {
                return false;
            }

            // Actualizar los datos de la visualización
            visualizacion.Nombre = updatedCard.Nombre;
            visualizacion.FechaDesde = updatedCard.FechaDesde;
            visualizacion.FechaHasta = updatedCard.FechaHasta;
            visualizacion.JsonDesign = updatedCard.JsonDiseñoGeneral;

            if (jsonConfig != null)
            {
                grupovisu.PropsConfiguracion = jsonConfig;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<DashboardResponse?> UpdateDashboardInfoAsync(int idDashboard, string username, string? nuevoNombre, string? nuevaDescripcion)
        {
            var dashboard = await _context.Dashboards
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);


            if (dashboard == null)
                return null;


            bool hasChanges = false;


            if (!string.IsNullOrWhiteSpace(nuevoNombre) && !string.Equals(dashboard.Nombre, nuevoNombre, StringComparison.Ordinal))
            {
                // Validar nombre único por usuario
                var exists = await _context.Dashboards
                    .AnyAsync(d => d.Username == username && d.Nombre == nuevoNombre && d.IdDashboard != idDashboard);
                if (exists)
                {
                    throw new ArgumentException($"Ya existe un dashboard con el nombre '{nuevoNombre}' para el usuario '{username}'.");
                }


                dashboard.Nombre = nuevoNombre.Trim();
                hasChanges = true;
            }


            if (nuevaDescripcion != null && !string.Equals(dashboard.Descripcion, nuevaDescripcion, StringComparison.Ordinal))
            {
                dashboard.Descripcion = string.IsNullOrWhiteSpace(nuevaDescripcion) ? null : nuevaDescripcion.Trim();
                hasChanges = true;
            }


            if (!hasChanges)
            {
                // No hay cambios que aplicar; devolver el dashboard actual
                return await GetDashboardByIdAsync(idDashboard, username);
            }


            dashboard.FechaModificacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();


            return await GetDashboardByIdAsync(idDashboard, username);
        }

        /// <summary>
        /// Busca dashboards por un fragmento de texto en el nombre o descripción
        /// </summary>
        public async Task<List<DashboardSummaryResponse>> SearchDashboardsByTextAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<DashboardSummaryResponse>();

            var lowerQuery = query.ToLower();
            var dashboards = await _context.Dashboards
                .Where(d => d.Nombre.ToLower().Contains(lowerQuery) || (d.Descripcion != null && d.Descripcion.ToLower().Contains(lowerQuery)))
                .Select(d => new DashboardSummaryResponse
                {
                    IdDashboard = d.IdDashboard,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion,
                    Username = d.Username,
                    FechaCreacion = d.FechaCreacion,
                    FechaModificacion = d.FechaModificacion,
                    CantidadTarjetas = d.GrupoVisualizaciones.Count
                })
                .ToListAsync();

            return dashboards;
        }
    }
}
