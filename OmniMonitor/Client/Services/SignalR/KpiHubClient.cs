using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Client.Services.SignalR
{
    public class KpiHubClient : IAsyncDisposable
    {
        private readonly NavigationManager _nav;
        private readonly ILocalStorageService _localStorage;
        private HubConnection? _connection;
        private readonly ConcurrentDictionary<int, byte> _joinedKpis = new();

        public event Action<KpiResponse>? KpiUpdated;
        public event Action<ConnectionState>? ConnectionStateChanged;

        public KpiHubClient(NavigationManager nav, ILocalStorageService localStorage)
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
                .WithUrl(baseUrl + "/hubs/kpi", options =>
                {
                    options.AccessTokenProvider = async () => await _localStorage.GetItemAsync<string>("authToken") ?? string.Empty;
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<KpiResponse>("kpiUpdate", resp =>
            {
                KpiUpdated?.Invoke(resp);
            });

            _connection.Reconnecting += (ex) =>
            {
                ConnectionStateChanged?.Invoke(ConnectionState.Reconnecting);
                return Task.CompletedTask;
            };
            _connection.Reconnected += async (id) =>
            {
                ConnectionStateChanged?.Invoke(ConnectionState.Connected);
                // Rejoin KPIs after reconnect
                try
                {
                    foreach (var kv in _joinedKpis.Keys)
                    {
                        try { await _connection.InvokeAsync("JoinKpi", kv); } catch { }
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

        public async Task JoinKpiAsync(int kpiId)
        {
            _joinedKpis.TryAdd(kpiId, 0);
            var conn = await EnsureConnectionAsync();
            await conn.InvokeAsync("JoinKpi", kpiId);
        }

        public async Task LeaveKpiAsync(int kpiId)
        {
            if (_connection is { State: HubConnectionState.Connected })
            {
                await _connection.InvokeAsync("LeaveKpi", kpiId);
            }
            _joinedKpis.TryRemove(kpiId, out _);
            await ShutdownIfIdleAsync();
        }

        public async Task ShutdownIfIdleAsync()
        {
            if (_joinedKpis.IsEmpty && _connection is { } conn)
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
