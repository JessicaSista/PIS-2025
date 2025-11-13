using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniMonitor.Client.Extensions
{
    public static class HttpContentExtensions
    {
        private static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task<T?> ReadFromJsonWithEnumSupportAsync<T>(this HttpContent content, CancellationToken cancellationToken = default)
        {
            return await content.ReadFromJsonAsync<T>(DefaultJsonOptions, cancellationToken);
        }
    }
}