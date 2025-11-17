using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json.Serialization;

namespace OmniMonitor.Client.Services.SignalR
{
    public class TelemetryPointMessage
    {
        [JsonPropertyName("visualizationId")] public int VisualizationId { get; set; }
        [JsonPropertyName("datasetId")] public int DatasetId { get; set; }
        [JsonPropertyName("deviceId")] public int DeviceId { get; set; }
        [JsonPropertyName("time")] public DateTime Time { get; set; }
        [JsonPropertyName("value")] public double? Value { get; set; }
        [JsonPropertyName("raw")] public string? Raw { get; set; }
    }

    public enum ConnectionState
    {
        Connected,
        Reconnecting,
        Disconnected
    }

    public class TelemetryHubClient : IAsyncDisposable
    {
        private readonly NavigationManager _nav;
        private readonly ILocalStorageService _localStorage;
        private HubConnection? _connection;
        private readonly ConcurrentDictionary<int, byte> _joinedVisualizations = new();

        public event Action<TelemetryPointMessage>? TelemetryReceived;
        public event Action<ConnectionState>? ConnectionStateChanged;

        public TelemetryHubClient(NavigationManager nav, ILocalStorageService localStorage)
        {
            _nav = nav;
            _localStorage = localStorage;
        }

        private async Task<HubConnection> EnsureConnectionAsync()
        {
            if (_connection is { State: HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting })
            {
                return _connection;
            }

            string baseUrl = _nav.BaseUri.TrimEnd('/');

            _connection = new HubConnectionBuilder()
                .WithUrl(baseUrl + "/hubs/telemetry", options =>
                {
                    options.AccessTokenProvider = async () => await _localStorage.GetItemAsync<string>("authToken") ?? string.Empty;
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<TelemetryPointMessage>("telemetryPoint", msg =>
            {
                TelemetryReceived?.Invoke(msg);
            });

            _connection.Reconnecting += (ex) =>
            {
                ConnectionStateChanged?.Invoke(ConnectionState.Reconnecting);
                return Task.CompletedTask;
            };
            _connection.Reconnected += async (id) =>
            {
                ConnectionStateChanged?.Invoke(ConnectionState.Connected);
                // Rejoin any visualizations we had joined before reconnect
                try
                {
                    foreach (var kv in _joinedVisualizations.Keys)
                    {
                        try { await _connection.InvokeAsync("JoinVisualization", kv); } catch { }
                    }
                }
                catch { }
            };
            _connection.Closed += (ex) =>
            {
                ConnectionStateChanged?.Invoke(ConnectionState.Disconnected);
                return Task.CompletedTask;
            };

            await _connection.StartAsync();
            ConnectionStateChanged?.Invoke(ConnectionState.Connected);
            return _connection;
        }

        public async Task JoinVisualizationAsync(int visualizationId)
        {
            _joinedVisualizations.TryAdd(visualizationId, 0);
            var conn = await EnsureConnectionAsync();
            await conn.InvokeAsync("JoinVisualization", visualizationId);
        }

        public async Task LeaveVisualizationAsync(int visualizationId)
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                await _connection.InvokeAsync("LeaveVisualization", visualizationId);
            }
            _joinedVisualizations.TryRemove(visualizationId, out _);
            await ShutdownIfIdleAsync();
        }

        public async Task ShutdownIfIdleAsync()
        {
            if (_joinedVisualizations.IsEmpty && _connection is { } conn)
            {
                try { await conn.StopAsync(); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
    }
}
