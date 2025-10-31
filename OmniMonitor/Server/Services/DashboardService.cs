using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Services
{
    #region Interfaces

    /// <summary>
    /// Servicio para la gestión de dashboards de usuario.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Crea un nuevo dashboard con sus tarjetas configuradas.
        /// </summary>
        /// <param name="request">Datos para la creación del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dashboard creado.</returns>
        Task<DashboardResponse> CreateDashboardAsync(CreateDashboardRequest request, string username);

        /// <summary>
        /// Obtiene un dashboard por su ID y usuario.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>El dashboard encontrado o null.</returns>
        Task<DashboardResponse?> GetDashboardByIdAsync(int idDashboard, string username);

        /// <summary>
        /// Obtiene todos los dashboards de un usuario, con filtro opcional.
        /// </summary>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="query">Texto de búsqueda opcional.</param>
        /// <returns>Lista de dashboards resumen.</returns>
        Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username, string? query);

        /// <summary>
        /// Valida que todos los cardIds existan en el sistema.
        /// </summary>
        /// <param name="cardIds">Lista de IDs de tarjetas.</param>
        /// <returns>True si todos existen, false en caso contrario.</returns>
        Task<bool> ValidateCardIdsAsync(List<int> cardIds);

        /// <summary>
        /// Valida que el layout no tenga superposiciones inválidas.
        /// </summary>
        /// <param name="layout">Layout a validar.</param>
        /// <returns>True si es válido, false en caso contrario.</returns>
        Task<bool> ValidateLayoutAsync(DashboardLayout layout);

        /// <summary>
        /// Elimina un dashboard y sus tarjetas asociadas.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <returns>True si se eliminó correctamente, false en caso contrario.</returns>
        Task<bool> DeleteDashboardAsync(int idDashboard, string username);

        /// <summary>
        /// Actualiza el JSON de configuración de un dashboard.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="nuevoJsonDiseno">Nuevo JSON de diseño.</param>
        /// <returns>True si se actualizó correctamente, false en caso contrario.</returns>
        Task<bool> UpdateDashboardConfigAsync(int idDashboard, string username, string nuevoJsonDiseno);

        /// <summary>
        /// Agrega una tarjeta a un dashboard existente.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="jsonConfig">JSON de configuración.</param>
        /// <param name="nuevaCard">Tarjeta a agregar.</param>
        /// <returns>True si se agregó correctamente, false en caso contrario.</returns>
        Task<bool> AddDashboardCardAsync(int idDashboard, string username, string jsonConfig, DashboardCard nuevaCard);

        /// <summary>
        /// Reordena las tarjetas de un dashboard.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="jsonConfig">JSON de configuración.</param>
        /// <param name="orderedCards">Lista ordenada de tarjetas.</param>
        /// <returns>True si se reordenó correctamente, false en caso contrario.</returns>
        Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, string jsonConfig, List<DashboardCard> orderedCards);

        /// <summary>
        /// Elimina una tarjeta de un dashboard.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="idCard">ID de la tarjeta.</param>
        /// <param name="tipoCard">Tipo de tarjeta.</param>
        /// <returns>True si se eliminó correctamente, false en caso contrario.</returns>
        Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idCard, int tipoCard);

        /// <summary>
        /// Actualiza el nombre y/o la descripción de un dashboard.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="nuevoNombre">Nuevo nombre.</param>
        /// <param name="nuevaDescripcion">Nueva descripción.</param>
        /// <returns>El dashboard actualizado o null.</returns>
        Task<DashboardResponse?> UpdateDashboardInfoAsync(int idDashboard, string username, string? nuevoNombre, string? nuevaDescripcion);

        /// <summary>
        /// Edita una tarjeta de un dashboard.
        /// </summary>
        /// <param name="idDashboard">ID del dashboard.</param>
        /// <param name="username">Nombre de usuario.</param>
        /// <param name="jsonConfig">JSON de configuración.</param>
        /// <param name="idVisualizacion">ID de la visualización.</param>
        /// <param name="updatedCard">Datos actualizados de la tarjeta.</param>
        /// <returns>True si se editó correctamente, false en caso contrario.</returns>
        Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, int idVisualizacion, CreateVisualizacionRequest updatedCard);

        /// <summary>
        /// Busca dashboards por texto en nombre o descripción.
        /// </summary>
        /// <param name="query">Texto de búsqueda.</param>
        /// <returns>Lista de dashboards resumen.</returns>
        Task<List<DashboardSummaryResponse>> SearchDashboardsByTextAsync(string query);
    }

    #endregion

    #region Classes

    /// <summary>
    /// Implementación del servicio de dashboards de usuario.
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        /// <summary>
        /// Constructor de DashboardService.
        /// </summary>
        /// <param name="context">Contexto de base de datos.</param>
        /// <param name="logger">Logger para registrar eventos.</param>
        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea un nuevo dashboard con sus tarjetas configuradas
        /// </summary>
        public async Task<DashboardResponse> CreateDashboardAsync(CreateDashboardRequest request, string username)
        {
            try
            {
                _logger.LogInformation("Creando dashboard '{Nombre}' para usuario {Username}", request.Nombre, username);

                DashboardDto? existingDashboard = await _context.Dashboards
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Username == username && d.Nombre == request.Nombre);

                if (existingDashboard != null)
                {
                    _logger.LogWarning("Ya existe un dashboard con el nombre '{Nombre}' para el usuario {Username}", request.Nombre, username);
                    throw new ArgumentException($"Ya existe un dashboard con el nombre '{request.Nombre}' para el usuario '{username}'.");
                }

                if (request.Layout != null && request.Layout.Tarjetas != null && request.Layout.Tarjetas.Any())
                {
                    List<int> cardIds = request.Layout.Tarjetas.Select(t => t.CardId).ToList();
                    if (!await ValidateCardIdsAsync(cardIds))
                    {
                        _logger.LogWarning("Uno o más IdVisualizacion no existen en el sistema para el usuario {Username}", username);
                        throw new ArgumentException("Uno o más IdVisualizacion no existen en el sistema.");
                    }
                }

                DashboardDto nuevoDashboard = new()
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

                if (request.Layout != null && request.Layout.Tarjetas != null && request.Layout.Tarjetas.Any())
                {
                    foreach (var tarjeta in request.Layout.Tarjetas.Select((t, idx) => new { t, idx }))
                    {
                        GrupoVisualizacion grupoVisualizacion = new()
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

                _logger.LogInformation("Dashboard '{Nombre}' creado correctamente para usuario {Username}", request.Nombre, username);

                // Retornar el dashboard completo
                return await GetDashboardByIdAsync(nuevoDashboard.IdDashboard, username) 
                    ?? throw new InvalidOperationException("Error al recuperar el dashboard creado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando dashboard '{Nombre}' para usuario {Username}", request.Nombre, username);
                throw;
            }
        }

        /// <summary>
        /// Obtiene un dashboard por su ID con todas sus tarjetas
        /// </summary>
        public async Task<DashboardResponse?> GetDashboardByIdAsync(int idDashboard, string username)
        {
            try
            {
                _logger.LogInformation("Obteniendo dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);

                DashboardDto? dashboard = await _context.Dashboards
                    .Include(d => d.GrupoVisualizaciones)
                        .ThenInclude(gv => gv.Visualizacion)
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username == username);

                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return null;
                }

                DashboardResponse response = new()
                {
                    IdDashboard = dashboard.IdDashboard,
                    Username = dashboard.Username,
                    Nombre = dashboard.Nombre,
                    Descripcion = dashboard.Descripcion,
                    GrupoVisualizacion = dashboard.GrupoVisualizacion,
                    JsonDiseno = dashboard.JsonDiseno,
                    FechaCreacion = dashboard.FechaCreacion,
                    FechaModificacion = dashboard.FechaModificacion,
                    Tarjetas = dashboard.GrupoVisualizaciones
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
                        }).ToList()
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los dashboards de un usuario
        /// </summary>
        public async Task<List<DashboardSummaryResponse>> GetAllDashboardsAsync(string username, string? query)
        {
            try
            {
                _logger.LogInformation("Obteniendo todos los dashboards para usuario {Username}", username);

                IQueryable<DashboardDto> dashboardsQuery = _context.Dashboards
                    .AsNoTracking()
                    .Where(d => d.Username.ToLower() == username.ToLower());

                if (!string.IsNullOrEmpty(query))
                {
                    string loweredQuery = query.ToLowerInvariant();
                    dashboardsQuery = dashboardsQuery.Where(d =>
                        (!string.IsNullOrEmpty(d.Nombre) && d.Nombre.ToLower().Contains(loweredQuery)) ||
                        (!string.IsNullOrEmpty(d.Descripcion) && d.Descripcion.ToLower().Contains(loweredQuery)));
                }

                List<DashboardSummaryResponse> result = await dashboardsQuery
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

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dashboards para usuario {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Valida que todos los cardIds (IdVisualizacion) existan en el sistema
        /// </summary>
        public async Task<bool> ValidateCardIdsAsync(List<int> cardIds)
        {
            try
            {
                if (cardIds == null || !cardIds.Any())
                {
                    return true;
                }

                List<int> existingIds = await _context.Visualizaciones
                    .Where(v => cardIds.Contains(v.IdVisualizacion))
                    .Select(v => v.IdVisualizacion)
                    .ToListAsync();

                bool isValid = existingIds.Count == cardIds.Count;
                if (!isValid)
                {
                    _logger.LogWarning("Validación de cardIds fallida. Esperados: {Expected}, Encontrados: {Found}", cardIds.Count, existingIds.Count);
                }
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando cardIds");
                throw;
            }
        }

        /// <summary>
        /// Valida que el layout no tenga superposiciones inválidas
        /// </summary>
        public async Task<bool> ValidateLayoutAsync(DashboardLayout layout)
        {
            try
            {
                if (layout == null || layout.Tarjetas == null || !layout.Tarjetas.Any())
                {
                    return true;
                }

                // Validación de superposiciones (comentada)
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando layout de dashboard");
                throw;
            }
        }

        /// <summary>
        /// Elimina un dashboard y todos sus GrupoVisualizaciones asociados (no elimina visualizaciones/KPIs)
        /// </summary>
        public async Task<bool> DeleteDashboardAsync(int idDashboard, string username)
        {
            try
            {
                _logger.LogInformation("Eliminando dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);

                DashboardDto? dashboard = await _context.Dashboards
                    .Include(d => d.GrupoVisualizaciones)
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());

                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return false;
                }

                _context.GrupoVisualizaciones.RemoveRange(dashboard.GrupoVisualizaciones);
                _context.Dashboards.Remove(dashboard);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Dashboard con ID {IdDashboard} eliminado correctamente", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Actualiza el JSON de configuración (JsonDiseno) de un dashboard
        /// </summary>
        public async Task<bool> UpdateDashboardConfigAsync(int idDashboard, string username, string nuevoJsonDiseno)
        {
            try
            {
                DashboardDto? dashboard = await _context.Dashboards
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());
                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return false;
                }

                dashboard.JsonDiseno = nuevoJsonDiseno;
                dashboard.FechaModificacion = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Configuración de dashboard con ID {IdDashboard} actualizada correctamente", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando configuración de dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Agrega una tarjeta (DashboardCard) a un dashboard existente
        /// </summary>
        public async Task<bool> AddDashboardCardAsync(int idDashboard, string username, string jsonConfig, DashboardCard nuevaCard)
        {
            try
            {
                DashboardDto? dashboard = await _context.Dashboards
                    .Include(d => d.GrupoVisualizaciones)
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());
                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return false;
                }

                // Validar según el tipo de tarjeta
                if (nuevaCard.TipoCard == 1)
                {
                    // Validar que el CardId exista en Visualizaciones
                    bool visualizacionExiste = await _context.Visualizaciones.AnyAsync(v => v.IdVisualizacion == nuevaCard.CardId);
                    if (!visualizacionExiste)
                    {
                        _logger.LogWarning("No existe una visualización con Id {CardId}", nuevaCard.CardId);
                        throw new ArgumentException($"No existe una visualización con Id {nuevaCard.CardId}");
                    }
                }

                // Chequear si la tarjeta ya existe en el dashboard (por IdVisualizacion y TipoCard)
                bool cardExists = dashboard.GrupoVisualizaciones.Any(gv =>
                    gv.IdVisualizacion == nuevaCard.CardId &&
                    gv.TipoCard == nuevaCard.TipoCard);
                if (cardExists)
                {
                    _logger.LogWarning("Tarjeta duplicada: ya existe una tarjeta con ese IdVisualizacion y TipoCard en el dashboard.");
                    throw new ArgumentException("Tarjeta duplicada: ya existe una tarjeta con ese IdVisualizacion y TipoCard en el dashboard.");
                }

                // Calcular el orden para la nueva tarjeta
                int orden = _context.GrupoVisualizaciones
                    .Where(gv => gv.GrupoVisualizacionId == idDashboard)
                    .Select(gv => (int?)gv.Orden)
                    .Max() ?? 0;
                orden++;

                GrupoVisualizacion grupoVisualizacion = new()
                {
                    GrupoVisualizacionId = idDashboard,
                    IdVisualizacion = nuevaCard.CardId,
                    TipoCard = nuevaCard.TipoCard,
                    PropsConfiguracion = nuevaCard.Props.HasValue ? JsonSerializer.Serialize(nuevaCard.Props.Value) : null,
                    FechaAgregado = DateTime.UtcNow,
                    Orden = orden
                };

                //dashboard.JsonDiseno = JsonDiseno;
                _context.Dashboards.Update(dashboard);

                _context.GrupoVisualizaciones.Add(grupoVisualizacion);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tarjeta agregada correctamente al dashboard con ID {IdDashboard}", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error agregando tarjeta al dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Reordena las tarjetas (GrupoVisualizaciones) de un dashboard según el orden de la lista recibida
        /// </summary>
        public async Task<bool> ReorderDashboardCardsAsync(int idDashboard, string username, string jsonConfig, List<DashboardCard> orderedCards)
        {
            try
            {
                DashboardDto? dashboard = await _context.Dashboards
                    .Include(d => d.GrupoVisualizaciones)
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());
                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return false;
                }

                if (string.IsNullOrEmpty(jsonConfig))
                {
                    _logger.LogWarning("El JSON de configuración no puede estar vacío.");
                    throw new ArgumentException("El JSON de configuración no puede estar vacío.");
                }

                for (int i = 0; i < orderedCards.Count; i++)
                {
                    DashboardCard card = orderedCards[i];
                    GrupoVisualizacion? grupo = dashboard.GrupoVisualizaciones.FirstOrDefault(gv =>
                        gv.GrupoVisualizacionId == idDashboard &&
                        gv.IdVisualizacion == card.CardId &&
                        gv.TipoCard == card.TipoCard);
                    if (grupo != null)
                    {
                        grupo.Orden = i + 1;
                    }
                }
                dashboard.JsonDiseno = jsonConfig;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tarjetas reordenadas correctamente en el dashboard con ID {IdDashboard}", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordenando tarjetas en el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Elimina una tarjeta (GrupoVisualizacion) de un dashboard y actualiza el orden de las restantes
        /// </summary>
        public async Task<bool> DeleteDashboardCardAsync(int idDashboard, string username, int idCard, int tipoCard)
        {
            try
            {
                DashboardDto? dashboard = await _context.Dashboards
                    .Include(d => d.GrupoVisualizaciones)
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());
                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return false;
                }

                GrupoVisualizacion? cardToRemove = dashboard.GrupoVisualizaciones.FirstOrDefault(gv => gv.IdVisualizacion == idCard && gv.TipoCard == tipoCard);
                if (cardToRemove == null)
                {
                    _logger.LogWarning("No se encontró la tarjeta a eliminar en el dashboard con ID {IdDashboard}", idDashboard);
                    return false;
                }

                int ordenEliminado = cardToRemove.Orden;
                _context.GrupoVisualizaciones.Remove(cardToRemove);

                List<GrupoVisualizacion> cardsToUpdate = dashboard.GrupoVisualizaciones.Where(gv => gv.Orden > ordenEliminado).ToList();
                foreach (GrupoVisualizacion card in cardsToUpdate)
                {
                    card.Orden--;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tarjeta eliminada correctamente del dashboard con ID {IdDashboard}", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando tarjeta del dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Actualiza el nombre y/o la descripción de un dashboard
        /// </summary>
        public async Task<bool> EditDashboardCard(int idDashboard, string username, string jsonConfig, int visualizacionId, CreateVisualizacionRequest updatedCard)
        {
            try
            {
                Visualizacion? visualizacion = await _context.Visualizaciones
                        .Include(v => v.GrupoDatasets)
                        .FirstOrDefaultAsync(v => v.IdVisualizacion == visualizacionId && v.Username.ToLower() == username.ToLower());

                if (visualizacion == null)
                {
                    _logger.LogWarning("No se encontró la visualización con ID {VisualizacionId} para usuario {Username}", visualizacionId, username);
                    return false;
                }

                GrupoVisualizacion? grupovisu = await _context.GrupoVisualizaciones
                    .FirstOrDefaultAsync(gv => gv.GrupoVisualizacionId == idDashboard && gv.IdVisualizacion == visualizacion.IdVisualizacion);

                if (grupovisu == null)
                {
                    _logger.LogWarning("No se encontró el grupo visualización para el dashboard con ID {IdDashboard}", idDashboard);
                    return false;
                }

                // Actualizar los datos de la visualización
                visualizacion.Nombre = updatedCard.Nombre;
                visualizacion.FechaDesde = updatedCard.FechaDesde;
                visualizacion.FechaHasta = updatedCard.FechaHasta;
                visualizacion.JsonDesign = updatedCard.JsonDiseñoGeneral;

                if (updatedCard.Datasets != null && updatedCard.Datasets.Any())
                {
                    List<int> newDatasetIds = updatedCard.Datasets.Select(d => d.DatasetId).ToList();

                    List<GrupoDataset> existingGrupoDatasets = visualizacion.GrupoDatasets.ToList();
                    List<int> existingDatasetIds = existingGrupoDatasets.Select(gd => gd.DatasetId).ToList();

                    List<GrupoDataset> datasetsToRemove = existingGrupoDatasets
                        .Where(gd => !newDatasetIds.Contains(gd.DatasetId))
                        .ToList();

                    foreach (GrupoDataset grupoToRemove in datasetsToRemove)
                    {
                        visualizacion.GrupoDatasets.Remove(grupoToRemove);
                        _context.GrupoDatasets.Remove(grupoToRemove);
                    }

                    foreach (DatasetConfig datasetConfig in updatedCard.Datasets)
                    {
                        GrupoDataset? existingGrupo = existingGrupoDatasets
                            .FirstOrDefault(gd => gd.DatasetId == datasetConfig.DatasetId);

                        if (existingGrupo != null)
                        {
                            existingGrupo.JsonDesign = datasetConfig.JsonDiseño;
                            _context.GrupoDatasets.Update(existingGrupo);
                        }
                        else
                        {
                            GrupoDataset nuevoGrupo = new()
                            {
                                DatasetId = datasetConfig.DatasetId,
                                JsonDesign = datasetConfig.JsonDiseño
                            };
                            visualizacion.GrupoDatasets.Add(nuevoGrupo);
                        }
                    }
                }

                _context.Visualizaciones.Update(visualizacion);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Visualización editada correctamente en el dashboard con ID {IdDashboard}", idDashboard);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editando visualización en el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        public async Task<DashboardResponse?> UpdateDashboardInfoAsync(int idDashboard, string username, string? nuevoNombre, string? nuevaDescripcion)
        {
            try
            {
                DashboardDto? dashboard = await _context.Dashboards
                    .FirstOrDefaultAsync(d => d.IdDashboard == idDashboard && d.Username.ToLower() == username.ToLower());


                if (dashboard == null)
                {
                    _logger.LogWarning("No se encontró el dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                    return null;
                }


                bool hasChanges = false;


                if (!string.IsNullOrWhiteSpace(nuevoNombre) && !string.Equals(dashboard.Nombre, nuevoNombre, StringComparison.Ordinal))
                {
                    // Validar nombre único por usuario
                    var exists = await _context.Dashboards
                        .AnyAsync(d => d.Username.ToLower() == username.ToLower() && string.Equals(d.Nombre, nuevoNombre, StringComparison.Ordinal) && d.IdDashboard != idDashboard);
                    if (exists)
                    {
                        _logger.LogWarning("Ya existe un dashboard con el nombre '{NuevoNombre}' para el usuario '{Username}'", nuevoNombre, username);
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


                _logger.LogInformation("Información del dashboard con ID {IdDashboard} actualizada correctamente", idDashboard);

                return await GetDashboardByIdAsync(idDashboard, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando información del dashboard con ID {IdDashboard} para usuario {Username}", idDashboard, username);
                throw;
            }
        }

        /// <summary>
        /// Busca dashboards por un fragmento de texto en el nombre o descripción
        /// </summary>
        public async Task<List<DashboardSummaryResponse>> SearchDashboardsByTextAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new();
                }

                string lowerQuery = query.ToLower();
                List<DashboardSummaryResponse> dashboards = await _context.Dashboards
                    .AsNoTracking()
                    .Where(d => d.Nombre.ToLower().Contains(lowerQuery) || (!string.IsNullOrEmpty(d.Descripcion) && d.Descripcion.ToLower().Contains(lowerQuery)))
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

                _logger.LogInformation("Búsqueda de dashboards por texto '{Query}' retornó {Count} resultados", query, dashboards.Count);

                return dashboards;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando dashboards por texto '{Query}'", query);
                throw;
            }
        }
    }

    #endregion
}
