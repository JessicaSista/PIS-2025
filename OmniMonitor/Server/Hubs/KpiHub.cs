using System;
using System.Collections.Concurrent;
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
    public class KpiHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<KpiHub> _logger;
        private readonly ILiveSubscriptionRegistry _registry;
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte>> _connKpis = new();

        public KpiHub(ApplicationDbContext db, ILogger<KpiHub> logger, ILiveSubscriptionRegistry registry)
        {
            _db = db;
            _logger = logger;
            _registry = registry;
        }

        private static string KpiGroupName(int kpiId) => $"kpi:{kpiId}";

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
            _logger.LogInformation("KpiHub connected: {ConnectionId}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("KpiHub disconnected: {ConnectionId}. Error: {Error}", Context.ConnectionId, exception?.Message);
            if (_connKpis.TryRemove(Context.ConnectionId, out var set))
            {
                foreach (var kv in set)
                {
                    _ = _registry.UnregisterKpiAsync(kv.Key);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task JoinKpi(int kpiId)
        {
            if (kpiId <= 0)
            {
                throw new HubException("Invalid kpi id");
            }

            string username = GetUsernameOrThrow();
            var kpi = await _db.Kpi.FirstOrDefaultAsync(k => k.Id == kpiId);
            if (kpi == null)
            {
                throw new HubException("NotFound: KPI does not exist");
            }

            if (!string.Equals(kpi.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                throw new HubException("Forbidden: you do not have access to this KPI");
            }

            if (!kpi.LiveEnabled)
            {
                throw new HubException("Live is disabled for this KPI");
            }

            string group = KpiGroupName(kpiId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            var set = _connKpis.GetOrAdd(Context.ConnectionId, _ => new ConcurrentDictionary<int, byte>());
            if (set.TryAdd(kpiId, 0))
            {
                await _registry.RegisterKpiAsync(kpiId);
            }
            _logger.LogInformation("User {User} joined group {Group}", username, group);
        }

        public async Task LeaveKpi(int kpiId)
        {
            if (kpiId <= 0)
            {
                return;
            }

            string group = KpiGroupName(kpiId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            if (_connKpis.TryGetValue(Context.ConnectionId, out var set) && set.TryRemove(kpiId, out _))
            {
                await _registry.UnregisterKpiAsync(kpiId);
            }
            _logger.LogInformation("Connection {Conn} left group {Group}", Context.ConnectionId, group);
        }
    }
}
