using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using OmniMonitor.Server.Context;
using OmniMonitor.Server.Models;
using OmniMonitor.Shared.Dtos;

using static MudBlazor.CategoryTypes;

namespace OmniMonitor.Server.Services
{
    /// <summary>
    /// Interfaz para el servicio de Dashboards
    /// </summary>
    public interface IDashboardService
    {
        Task<DashboardResponse> CreateDashboardAsync(CreateDashboardRequest request, string username);
        Task<DashboardResponse?> GetDashboardByIdAsync(int idDashboard, string username);
        Task<DashboardResponse?> GetDashboardByIdAsyncSinToken(int idDashboard);
        Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username, string? query);
        Task<bool> ValidateCardIdsAsync(List<int> cardIds);
        Task<bool> ValidateLayoutAsync(DashboardLayout layout);
        Task<bool> DeleteDashboardAsync(int idDashboard, string username);
        Task<bool> UpdateDashboardConfigAsync(int idDashboard, string username, string nuevoJsonDiseno);
        Task<bool> AddDashboardCardAsync(int idDashboard, string username, string jsonConfig,DashboardCard nuevaCard);
        Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, string jsonConfig, List<DashboardCard> orderedCards);
        Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idCard, int tipoCard);
        Task<DashboardResponse?> UpdateDashboardInfoAsync(int idDashboard, string username, string? nuevoNombre, string? nuevaDescripcion);
        Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, System.Int32 IdVisualizacion, CreateVisualizacionRequest updatedCard);
        Task<List<DashboardSummaryResponse>> SearchDashboardsByTextAsync(string query);
        Task<ShareResponseDto> CreateShareLinkAsync(int dashboardId, ShareRequestDto request, string username);
        Task<List<ShareResponseDto>> GetAllByDashboardAsync(int dashboardId, string username);
        Task<ShareResponseDto?> GetBySlugAsync(string slug);
        Task<ValidateSharePasswordResponseDto> ValidatePasswordAsync(string slug, string password);
        Task<ShareResponseDto?> UpdateShareLinkAsync(string slug, ShareRequestDto request, string username);
        Task<bool> DeleteShareLinkAsync(string slug, string username);
    }   

    /// <summary>
    /// Implementación del servicio de Dashboards
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<SharedLink> _passwordHasher;

        public DashboardService(ApplicationDbContext context, IPasswordHasher<SharedLink> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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

            // Validar cardIds por tipo si se proporcionan
            if (request.Layout?.Tarjetas != null && request.Layout.Tarjetas.Any())
            {
                var visualIds = request.Layout.Tarjetas.Where(t => t.TipoCard == 1).Select(t => t.CardId).ToList();
                if (visualIds.Any())
                {
                    if (!await ValidateCardIdsAsync(visualIds))
                    {
                        throw new ArgumentException("Uno o más IdVisualizacion no existen en el sistema.");
                    }
                }

                var kpiIds = request.Layout.Tarjetas.Where(t => t.TipoCard == 2).Select(t => t.CardId).ToList();
                if (kpiIds.Any())
                {
                    var existingKpiIds = await _context.Kpi
                        .Where(k => kpiIds.Contains(k.Id))
                        .Select(k => k.Id)
                        .ToListAsync();
                    if (existingKpiIds.Count != kpiIds.Count)
                    {
                        throw new ArgumentException("Uno o más KPI no existen en el sistema.");
                    }
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
                        IdVisualizacion = tarjeta.t.TipoCard == 1 ? tarjeta.t.CardId : (int?)null,
                        KpiId = tarjeta.t.TipoCard == 2 ? tarjeta.t.CardId : (int?)null,
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
                .Include(d => d.GrupoVisualizaciones)
                    .ThenInclude(gv => gv.Kpi)
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

            

            // Mapear las tarjetas
            response.Tarjetas = dashboard.GrupoVisualizaciones
                .OrderBy(gv => gv.Orden)
                .Select(gv => new DashboardCardResponse
                {
                    IdGrupoVisualizacion = gv.IdGrupoVisualizacion,
                    CardId = gv.TipoCard == 1 ? gv.IdVisualizacion.GetValueOrDefault() : gv.KpiId.GetValueOrDefault(),
                    TipoCard = gv.TipoCard,
                    PropsConfiguracion = gv.PropsConfiguracion,
                    FechaAgregado = gv.FechaAgregado,
                    Visualizacion = (gv.TipoCard == 1 && gv.Visualizacion != null) ? new VisualizacionInfo
                    {
                        IdVisualizacion = gv.Visualizacion.IdVisualizacion,
                        Nombre = gv.Visualizacion.Nombre,
                        FechaDesde = gv.Visualizacion.FechaDesde,
                        FechaHasta = gv.Visualizacion.FechaHasta,
                        JsonDesign = gv.Visualizacion.JsonDesign
                    } : null,
                    Kpi = (gv.TipoCard == 2 && gv.Kpi != null) ? new KpiInfo
                    {
                        Id = gv.Kpi.Id,
                        Name = gv.Kpi.Name,
                        Unit = gv.Kpi.Unit
                    } : null
                }).ToList();

            return response;
        }

        public async Task<DashboardResponse?> GetDashboardByIdAsyncSinToken(int idDashboard)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                    .ThenInclude(gv => gv.Visualizacion)
                .Include(d => d.GrupoVisualizaciones)
                    .ThenInclude(gv => gv.Kpi)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard);

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

            // Mapear las tarjetas
            response.Tarjetas = dashboard.GrupoVisualizaciones
                .OrderBy(gv => gv.Orden)
                .Select(gv => new DashboardCardResponse
                {
                    IdGrupoVisualizacion = gv.IdGrupoVisualizacion,
                    CardId = gv.TipoCard == 1 ? gv.IdVisualizacion.GetValueOrDefault() : gv.KpiId.GetValueOrDefault(),
                    TipoCard = gv.TipoCard,
                    PropsConfiguracion = gv.PropsConfiguracion,
                    FechaAgregado = gv.FechaAgregado,
                    Visualizacion = (gv.TipoCard == 1 && gv.Visualizacion != null) ? new VisualizacionInfo
                    {
                        IdVisualizacion = gv.Visualizacion.IdVisualizacion,
                        Nombre = gv.Visualizacion.Nombre,
                        FechaDesde = gv.Visualizacion.FechaDesde,
                        FechaHasta = gv.Visualizacion.FechaHasta,
                        JsonDesign = gv.Visualizacion.JsonDesign
                    } : null,
                    Kpi = (gv.TipoCard == 2 && gv.Kpi != null) ? new KpiInfo
                    {
                        Id = gv.Kpi.Id,
                        Name = gv.Kpi.Name,
                        Unit = gv.Kpi.Unit
                    } : null
                }).ToList();

            return response;
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario
        /// </summary>
        public async Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username, string? query)
        {
            var dashboardsQuery = _context.Dashboards
                .Where(d => d.Username == username);

            if (!string.IsNullOrWhiteSpace(query))
            {
                var loweredQuery = query.ToLowerInvariant();
                dashboardsQuery = dashboardsQuery.Where(d =>
                    (d.Nombre != null && d.Nombre.ToLower().Contains(loweredQuery)) ||
                    (d.Descripcion != null && d.Descripcion.ToLower().Contains(loweredQuery)));
            }

            return await dashboardsQuery
                .Select(d => new DashboardSummaryResponse
                {
                    IdDashboard = d.IdDashboard,
                    Username = d.Username,
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
        public async Task<bool> AddDashboardCardAsync(int idDashboard, string username, string JsonDiseno, DashboardCard nuevaCard)
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
                bool kpiExiste = await _context.Kpi.AnyAsync(k => k.Id == nuevaCard.CardId);
                if (!kpiExiste)
                    throw new ArgumentException($"No existe un KPI con Id {nuevaCard.CardId}");
            }

            //if (JsonDiseno == null){
            //    throw new ArgumentException("El JSON de configuración no puede estar vacío.");
            //}

            // Chequear si la tarjeta ya existe en el dashboard (por TipoCard + CardId)
            bool cardExists = dashboard.GrupoVisualizaciones.Any(gv =>
                gv.TipoCard == nuevaCard.TipoCard &&
                ((nuevaCard.TipoCard == 1 && gv.IdVisualizacion == nuevaCard.CardId) ||
                 (nuevaCard.TipoCard == 2 && gv.KpiId == nuevaCard.CardId)));
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
                IdVisualizacion = nuevaCard.TipoCard == 1 ? nuevaCard.CardId : (int?)null,
                KpiId = nuevaCard.TipoCard == 2 ? nuevaCard.CardId : (int?)null,
                TipoCard = nuevaCard.TipoCard,
                PropsConfiguracion = nuevaCard.Props.HasValue ? JsonSerializer.Serialize(nuevaCard.Props.Value) : null,
                FechaAgregado = DateTime.UtcNow,
                Orden = orden
            };

            //dashboard.JsonDiseno = JsonDiseno;
            _context.Dashboards.Update(dashboard);

            _context.GrupoVisualizaciones.Add(grupoVisualizacion);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Reordena las tarjetas (GrupoVisualizaciones) de un dashboard según el orden de la lista recibida
        /// </summary>
        public async Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, string JsonDiseno, List<DashboardCard> orderedCards)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            if (JsonDiseno == null){
                throw new ArgumentException("El JSON de configuración no puede estar vacío.");
            }

            for (int i = 0; i < orderedCards.Count; i++)
            {
                var card = orderedCards[i];
                var grupo = dashboard.GrupoVisualizaciones.FirstOrDefault(gv =>
                    gv.GrupoVisualizacionId == idDashboard &&
                    gv.TipoCard == card.TipoCard &&
                    ((card.TipoCard == 1 && gv.IdVisualizacion == card.CardId) ||
                     (card.TipoCard == 2 && gv.KpiId == card.CardId)));
                if (grupo != null)
                {
                    grupo.Orden = i + 1;
                }
            }
            dashboard.JsonDiseno = JsonDiseno;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Elimina una tarjeta (GrupoVisualizacion) de un dashboard y actualiza el orden de las restantes
        /// </summary>
        public async Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idCard, int tipoCard)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.GrupoVisualizaciones)
                .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);
            if (dashboard == null)
                return false;

            var cardToRemove = dashboard.GrupoVisualizaciones.FirstOrDefault(gv =>
                gv.TipoCard == tipoCard &&
                ((tipoCard == 1 && gv.IdVisualizacion == idCard) || (tipoCard == 2 && gv.KpiId == idCard)));
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
        public async Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, System.Int32 visualizacionId, CreateVisualizacionRequest updatedCard)
        {
            // Implementación de otro método si es necesario

            var visualizacion = await _context.Visualizaciones
                    .Include(v => v.GrupoDatasets)
                    .FirstOrDefaultAsync(v => v.IdVisualizacion == visualizacionId && v.Username == username);

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

            if (updatedCard.Datasets != null && updatedCard.Datasets.Any())
                {
                    var newDatasetIds = updatedCard.Datasets.Select(d => d.DatasetId).ToList();

                    var existingGrupoDatasets = visualizacion.GrupoDatasets.ToList();
                    var existingDatasetIds = existingGrupoDatasets.Select(gd => gd.DatasetId).ToList();

                    var datasetsToRemove = existingGrupoDatasets
                        .Where(gd => !newDatasetIds.Contains(gd.DatasetId))
                        .ToList();

                    foreach (var grupoToRemove in datasetsToRemove)
                    {
                        visualizacion.GrupoDatasets.Remove(grupoToRemove);
                        _context.GrupoDatasets.Remove(grupoToRemove);
                    }

                    foreach (var datasetConfig in updatedCard.Datasets)
                    {
                        var existingGrupo = existingGrupoDatasets
                            .FirstOrDefault(gd => gd.DatasetId == datasetConfig.DatasetId);

                        if (existingGrupo != null)
                        {
                            existingGrupo.JsonDesign = datasetConfig.JsonDiseño;
                            _context.GrupoDatasets.Update(existingGrupo);
                        }
                        else
                        {
                            var nuevoGrupo = new GrupoDataset
                            {
                                DatasetId = datasetConfig.DatasetId,
                                JsonDesign = datasetConfig.JsonDiseño
                            };
                            visualizacion.GrupoDatasets.Add(nuevoGrupo);
                        }
                    }
                }


            if (jsonConfig != null)
            {
                //agrega complejidad extra y no es necesario por el momento
                //grupovisu.PropsConfiguracion = jsonConfig;
            }

            _context.Visualizaciones.Update(visualizacion);
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

        public async Task<ShareResponseDto> CreateShareLinkAsync(int dashboardId, ShareRequestDto request, string username)
        {
            var dashboard = await _context.Dashboards
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdDashboard == dashboardId && d.Username == username);

            if (dashboard == null)
            {
                throw new KeyNotFoundException("Dashboard no encontrado o no pertenece al usuario.");
            }

            var sharedLink = new SharedLink
            {
                DashboardId = dashboardId,
                Slug = Guid.NewGuid().ToString("N").Substring(0, 10),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                Status = ShareStatus.Active,
                Visibility = (request.Visibility?.ToLower() == "private") ? ShareVisibility.Private : ShareVisibility.Public
            };

            if (sharedLink.Visibility == ShareVisibility.Private && !string.IsNullOrWhiteSpace(request.Password))
            {
                sharedLink.PasswordHash = _passwordHasher.HashPassword(sharedLink, request.Password);
            }

            _context.SharedLinks.Add(sharedLink);
            await _context.SaveChangesAsync();

            return new ShareResponseDto
            {
                Slug = sharedLink.Slug,
                Status = sharedLink.Status.ToString(),
                Visibility = sharedLink.Visibility.ToString(),
                ExpiresAt = sharedLink.ExpiresAt,
                CreatedAt = sharedLink.CreatedAt,
                dashBoardId = sharedLink.DashboardId
            };
        }

        public async Task<List<ShareResponseDto>> GetAllByDashboardAsync(int dashboardId, string username)
        {
            return await _context.SharedLinks
                .Where(s => s.DashboardId == dashboardId && s.Dashboard.Username == username)
                .Select(s => new ShareResponseDto
                {
                    Slug = s.Slug,
                    Status = s.Status.ToString(),
                    Visibility = s.Visibility.ToString(),
                    ExpiresAt = s.ExpiresAt,
                    CreatedAt = s.CreatedAt,
                    dashBoardId = s.DashboardId
                })
                .ToListAsync();
        }

        public async Task<ShareResponseDto?> GetBySlugAsync(string slug)
        {
            var link = await _context.SharedLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug == slug);

            // Valida si el enlace es accesible
            if (link == null || link.Status == ShareStatus.Hidden || (link.ExpiresAt.HasValue && link.ExpiresAt < DateTime.UtcNow))
            {
                return null;
            }

            return new ShareResponseDto
            {
                Slug = link.Slug,
                Status = link.Status.ToString(),
                Visibility = link.Visibility.ToString(),
                ExpiresAt = link.ExpiresAt,
                CreatedAt = link.CreatedAt,
                dashBoardId = link.DashboardId
            };
        }

        public async Task<ValidateSharePasswordResponseDto> ValidatePasswordAsync(string slug, string password)
        {
            var link = await _context.SharedLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Slug == slug);

            if (link == null || link.Visibility != ShareVisibility.Private || string.IsNullOrWhiteSpace(link.PasswordHash))
            {
                return new ValidateSharePasswordResponseDto { IsValid = false, DashboardId = null };
            }

            var result = _passwordHasher.VerifyHashedPassword(link, link.PasswordHash, password);

            if (result == PasswordVerificationResult.Success)
            {
                return new ValidateSharePasswordResponseDto { IsValid = true, DashboardId = link.DashboardId };
            }

            return new ValidateSharePasswordResponseDto { IsValid = false, DashboardId = null };
        }

        public async Task<ShareResponseDto?> UpdateShareLinkAsync(string slug, ShareRequestDto request, string username)
        {
            var linkToUpdate = await _context.SharedLinks
                .Include(s => s.Dashboard)
                .FirstOrDefaultAsync(s => s.Slug == slug && s.Dashboard.Username == username);

            if (linkToUpdate == null)
            {
                return null;
            }

            linkToUpdate.Visibility = (request.Visibility?.ToLower() == "private") ? ShareVisibility.Private : ShareVisibility.Public;
            linkToUpdate.ExpiresAt = request.ExpiresAt;

            if (linkToUpdate.Visibility == ShareVisibility.Private && !string.IsNullOrWhiteSpace(request.Password))
            {
                linkToUpdate.PasswordHash = _passwordHasher.HashPassword(linkToUpdate, request.Password);
            }
            else if (linkToUpdate.Visibility == ShareVisibility.Public)
            {
                linkToUpdate.PasswordHash = null;
            }

            await _context.SaveChangesAsync();

            return new ShareResponseDto
            {
                Slug = linkToUpdate.Slug,
                Status = linkToUpdate.Status.ToString(),
                Visibility = linkToUpdate.Visibility.ToString(),
                ExpiresAt = linkToUpdate.ExpiresAt,
                CreatedAt = linkToUpdate.CreatedAt,
                dashBoardId = linkToUpdate.DashboardId
            };
        }

        public async Task<bool> DeleteShareLinkAsync(string slug, string username)
        {
            var linkToDelete = await _context.SharedLinks
                .FirstOrDefaultAsync(s => s.Slug == slug && s.Dashboard.Username == username);

            if (linkToDelete == null)
            {
                return false;
            }

            _context.SharedLinks.Remove(linkToDelete);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
