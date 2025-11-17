using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace OmniMonitor.Server.Live
{
    public enum SubscriptionKind { Visualization, Kpi }

    public readonly struct SubscriptionChange
    {
        public SubscriptionKind Kind { get; }
        public int Id { get; }
        public bool Start { get; }
        public SubscriptionChange(SubscriptionKind kind, int id, bool start)
        {
            Kind = kind;
            Id = id;
            Start = start;
        }
    }

    public interface ILiveSubscriptionRegistry
    {
        Task RegisterVisualizationAsync(int visualizationId);
        Task UnregisterVisualizationAsync(int visualizationId);
        Task RegisterKpiAsync(int kpiId);
        Task UnregisterKpiAsync(int kpiId);
        ChannelReader<SubscriptionChange> Changes { get; }
    }

    public class LiveSubscriptionRegistry : ILiveSubscriptionRegistry
    {
        private readonly ConcurrentDictionary<(SubscriptionKind Kind, int Id), int> _counts = new();
        private readonly Channel<SubscriptionChange> _channel = Channel.CreateUnbounded<SubscriptionChange>();

        public ChannelReader<SubscriptionChange> Changes => _channel.Reader;

        private Task RegisterAsync(SubscriptionKind kind, int id)
        {
            var key = (kind, id);
            int newCount = _counts.AddOrUpdate(key, 1, (_, c) => c + 1);
            if (newCount == 1)
            {
                _channel.Writer.TryWrite(new SubscriptionChange(kind, id, true));
            }
            return Task.CompletedTask;
        }

        private Task UnregisterAsync(SubscriptionKind kind, int id)
        {
            var key = (kind, id);
            if (_counts.TryGetValue(key, out int count))
            {
                if (count <= 1)
                {
                    _counts.TryRemove(key, out _);
                    _channel.Writer.TryWrite(new SubscriptionChange(kind, id, false));
                }
                else
                {
                    _counts[key] = count - 1;
                }
            }
            return Task.CompletedTask;
        }

        public Task RegisterVisualizationAsync(int visualizationId) => RegisterAsync(SubscriptionKind.Visualization, visualizationId);
        public Task UnregisterVisualizationAsync(int visualizationId) => UnregisterAsync(SubscriptionKind.Visualization, visualizationId);
        public Task RegisterKpiAsync(int kpiId) => RegisterAsync(SubscriptionKind.Kpi, kpiId);
        public Task UnregisterKpiAsync(int kpiId) => UnregisterAsync(SubscriptionKind.Kpi, kpiId);
    }
}
