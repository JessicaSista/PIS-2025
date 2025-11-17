using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Live;

namespace OmniMonitor.Server.Hubs
{
    [Authorize]
    public class TelemetryHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TelemetryHub> _logger;
        private readonly ILiveSubscriptionRegistry _registry;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> _connVisualizations = new();

        public TelemetryHub(ApplicationDbContext db, ILogger<TelemetryHub> logger, ILiveSubscriptionRegistry registry)
        {
            _db = db;
            _logger = logger;
            _registry = registry;
        }

        private static string VisualizationGroupName(int visualizationId) => $"visualization:{visualizationId}";

        private string GetUsernameOrThrow()
        {
            string? name = Context?.User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new HubException("Unauthorized: missing identity");
            }
            return name;
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("TelemetryHub connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("TelemetryHub disconnected: {ConnectionId}. Error: {Error}", Context.ConnectionId, exception?.Message);
            if (_connVisualizations.TryRemove(Context.ConnectionId, out var set))
            {
                foreach (var kv in set)
                {
                    _ = _registry.UnregisterVisualizationAsync(kv.Key);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinVisualization(int visualizationId)
        {
            if (visualizationId <= 0)
            {
                throw new HubException("Invalid visualization id");
            }

            string username = GetUsernameOrThrow();
            var vis = await _db.Visualizaciones.FirstOrDefaultAsync(v => v.IdVisualizacion == visualizationId);
            if (vis == null)
            {
                throw new HubException("NotFound: visualization does not exist");
            }

            if (!string.Equals(vis.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException("Forbidden: you do not have access to this visualization");
            }

            if (!vis.LiveEnabled)
            {
                throw new HubException("Live is disabled for this visualization");
            }

            string group = VisualizationGroupName(visualizationId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            var set = _connVisualizations.GetOrAdd(Context.ConnectionId, _ => new ConcurrentDictionary<int, byte>());
            if (set.TryAdd(visualizationId, 0))
            {
                await _registry.RegisterVisualizationAsync(visualizationId);
            }
            _logger.LogInformation("User {User} joined group {Group}", username, group);
        }

        public async Task LeaveVisualization(int visualizationId)
        {
            if (visualizationId <= 0)
            {
                return;
            }

            string group = VisualizationGroupName(visualizationId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            if (_connVisualizations.TryGetValue(Context.ConnectionId, out var set) && set.TryRemove(visualizationId, out _))
            {
                await _registry.UnregisterVisualizationAsync(visualizationId);
            }
            _logger.LogInformation("Connection {Conn} left group {Group}", Context.ConnectionId, group);
        }
    }
}
