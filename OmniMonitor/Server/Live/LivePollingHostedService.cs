using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniMonitor.Server.Context;
using OmniMonitor.Server.Configuration;
using OmniMonitor.Server.Hubs;
using OmniMonitor.Server.Services;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Server.Live
{
    public class LivePollingHostedService : BackgroundService
    {
        private readonly ILogger<LivePollingHostedService> _logger;
        private readonly ILiveSubscriptionRegistry _registry;
        private readonly LiveOptions _options;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<TelemetryHub> _telemetryHub;
        private readonly IHubContext<KpiHub> _kpiHub;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _vizLoops = new();
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _kpiLoops = new();

        public LivePollingHostedService(
            ILogger<LivePollingHostedService> logger,
            ILiveSubscriptionRegistry registry,
            IServiceScopeFactory scopeFactory,
            IHubContext<TelemetryHub> telemetryHub,
            IHubContext<KpiHub> kpiHub,
            IOptions<LiveOptions> options)
        {
            _logger = logger;
            _registry = registry;
            _scopeFactory = scopeFactory;
            _telemetryHub = telemetryHub;
            _kpiHub = kpiHub;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LivePollingHostedService started");
            var reader = _registry.Changes;
            await foreach (var change in reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    switch (change.Kind)
                    {
                        case SubscriptionKind.Visualization:
                            if (change.Start)
                            {
                                StartVisualizationLoop(change.Id);
                            }
                            else
                            {
                                StopVisualizationLoop(change.Id);
                            }
                            break;
                        case SubscriptionKind.Kpi:
                            if (change.Start)
                            {
                                StartKpiLoop(change.Id);
                            }
                            else
                            {
                                StopKpiLoop(change.Id);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling subscription change {Kind} {Id} {Start}", change.Kind, change.Id, change.Start);
                }
            }
        }

        private void StartVisualizationLoop(int visualizationId)
        {
            if (_vizLoops.ContainsKey(visualizationId)) return;

            var cts = new CancellationTokenSource();
            if (_vizLoops.TryAdd(visualizationId, cts))
            {
                _ = Task.Run(() => VisualizationLoopAsync(visualizationId, cts.Token));
                _logger.LogInformation("Started visualization loop for {Id}", visualizationId);
            }
        }

        private void StopVisualizationLoop(int visualizationId)
        {
            if (_vizLoops.TryRemove(visualizationId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Stopped visualization loop for {Id}", visualizationId);
            }
        }

        private void StartKpiLoop(int kpiId)
        {
            if (_kpiLoops.ContainsKey(kpiId)) return;
            var cts = new CancellationTokenSource();
            if (_kpiLoops.TryAdd(kpiId, cts))
            {
                _ = Task.Run(() => KpiLoopAsync(kpiId, cts.Token));
                _logger.LogInformation("Started KPI loop for {Id}", kpiId);
            }
        }

        private void StopKpiLoop(int kpiId)
        {
            if (_kpiLoops.TryRemove(kpiId, out var cts))
            {
                cts.Cancel();
                _logger.LogInformation("Stopped KPI loop for {Id}", kpiId);
            }
        }

        // IM visualization delta poller
        private async Task VisualizationLoopAsync(int visualizationId, CancellationToken ct)
        {
            // Per-dataset last timestamp map
            var lastTimestamps = new Dictionary<int, DateTime>(); // key: DatasetIM.Id
            // Per (datasetId, deviceId) last broadcasted sample to suppress duplicates
            var lastBroadcast = new Dictionary<(int DatasetId, int DeviceId), (DateTime Time, double? Value, string? Raw)>();

            int backoff = _options.Backoff.BaseSeconds;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var imService = scope.ServiceProvider.GetRequiredService<ISondaIMService>();

                    var vis = await db.Visualizaciones
                        .Include(v => v.GrupoDatasets)
                        .FirstOrDefaultAsync(v => v.IdVisualizacion == visualizationId, ct);
                    if (vis == null || !vis.LiveEnabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct); // idle if missing/disabled
                        continue;
                    }

                    string owner = vis.Username;
                    var datasetIds = vis.GrupoDatasets.Select(g => g.DatasetId).ToList();
                    if (datasetIds.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), ct);
                        continue;
                    }

                    // Map general dataset ids to IM datasets
                    var imDatasets = await db.DatasetsIM
                        .Include(d => d.DatasetDevices)
                        .Where(d => d.Username == owner && datasetIds.Contains(d.DatasetId))
                        .ToListAsync(ct);

                    var now = DateTime.UtcNow;

                    foreach (var dsim in imDatasets)
                    {
                        if (string.IsNullOrWhiteSpace(dsim.SensorName))
                            continue;
                        if (dsim.DatasetDevices == null || dsim.DatasetDevices.Count == 0)
                            continue;

                        // determine window
                        if (!lastTimestamps.TryGetValue(dsim.Id, out var lastTs))
                        {
                            lastTs = now.AddSeconds(-_options.ImChartIntervalSeconds);
                        }
                        var from = lastTs.AddTicks(1);
                        var to = now;
                        if (from >= to) continue;

                        foreach (var dev in dsim.DatasetDevices)
                        {
                            var points = await imService.GetSensorDataByDate(dev.Id_device, dsim.SensorName, from, to, owner);
                            if (points == null || points.Count == 0) continue;

                            // Broadcast only new points
                            foreach (var p in points.OrderBy(x => x.Time))
                            {
                                // Try parse numeric value
                                double? value = null;
                                if (double.TryParse(p.Data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
                                {
                                    value = num;
                                }

                                // Server-side de-duplication: skip if same time/value already sent for this dataset/device
                                var key = (dsim.DatasetId, dev.Id_device);
                                if (lastBroadcast.TryGetValue(key, out var last))
                                {
                                    bool sameOrOlderTime = p.Time <= last.Time;
                                    bool sameValue = value.HasValue ? (last.Value.HasValue && Math.Abs(last.Value.Value - value.Value) < 1e-12)
                                                                     : string.Equals(p.Data, last.Raw, StringComparison.Ordinal);
                                    if (sameOrOlderTime && sameValue)
                                    {
                                        continue;
                                    }
                                }

                                var payload = new
                                {
                                    visualizationId,
                                    datasetId = dsim.DatasetId,
                                    deviceId = dev.Id_device,
                                    time = p.Time,
                                    value,
                                    raw = p.Data
                                };

                                await _telemetryHub.Clients.Group($"visualization:{visualizationId}")
                                    .SendAsync("telemetryPoint", payload, ct);

                                if (p.Time > lastTs)
                                {
                                    lastTs = p.Time;
                                }

                                // Update last broadcasted sample
                                lastBroadcast[key] = (p.Time, value, p.Data);
                            }
                        }

                        lastTimestamps[dsim.Id] = lastTs;
                    }

                    backoff = _options.Backoff.BaseSeconds; // reset on success
                    await Task.Delay(TimeSpan.FromSeconds(_options.ImChartIntervalSeconds), ct);
                }
                catch (OperationCanceledException)
                {
                    // normal on stop
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Visualization loop error for {Id}. Applying backoff.", visualizationId);
                    backoff = NextBackoffSeconds(backoff);
                    try { await Task.Delay(TimeSpan.FromSeconds(WithJitter(backoff)), ct); } catch { }
                }
            }
        }

        // KPI poller (recalc and broadcast on change). Sentinel optimization can be added later.
        private async Task KpiLoopAsync(int kpiId, CancellationToken ct)
        {
            object? lastValue = null;
            int backoff = _options.Backoff.BaseSeconds;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var kpiService = scope.ServiceProvider.GetRequiredService<IKpiService>();

                    var kpi = await db.Kpi.FirstOrDefaultAsync(k => k.Id == kpiId, ct);
                    if (kpi == null || !kpi.LiveEnabled)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15), ct);
                        continue;
                    }

                    // Reuse existing KPI logic
                    var response = await kpiService.CalculateKpiValueAsync(kpiId, kpi.Username);

                    var newValue = response?.Value;
                    bool changed = (newValue == null && lastValue != null) ||
                                   (newValue != null && !newValue.Equals(lastValue));

                    if (changed)
                    {
                        await _kpiHub.Clients.Group($"kpi:{kpiId}").SendAsync("kpiUpdate", response, ct);
                        lastValue = newValue;
                    }

                    // Interval by module
                    backoff = _options.Backoff.BaseSeconds; // reset on success
                    int seconds = string.Equals(kpi.SourceModule, "IM", StringComparison.OrdinalIgnoreCase)
                        ? _options.ImKpiIntervalSeconds
                        : _options.EmKpiIntervalSeconds;
                    await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
                }
                catch (OperationCanceledException)
                {
                    // normal on stop
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "KPI loop error for {Id}. Applying backoff.", kpiId);
                    backoff = NextBackoffSeconds(backoff);
                    try { await Task.Delay(TimeSpan.FromSeconds(WithJitter(backoff)), ct); } catch { }
                }
            }
        }

        private int NextBackoffSeconds(int current)
        {
            var next = (int)Math.Ceiling(current * _options.Backoff.Multiplier);
            if (next < _options.Backoff.BaseSeconds) next = _options.Backoff.BaseSeconds;
            if (next > _options.Backoff.MaxSeconds) next = _options.Backoff.MaxSeconds;
            return next;
        }

        private int WithJitter(int baseSeconds)
        {
            var ratio = _options.Backoff.JitterRatio;
            if (ratio <= 0) return baseSeconds;
            // Random factor in [1 - ratio, 1 + ratio]
            var delta = (Random.Shared.NextDouble() * 2 * ratio) - ratio;
            var result = baseSeconds * (1.0 + delta);
            return Math.Max(1, (int)Math.Round(result));
        }
    }
}
