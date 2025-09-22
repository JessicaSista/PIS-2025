using Blazored.LocalStorage;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace OmniMonitor.Client.Auth
{
    /// <summary>
    /// This DelegatingHandler intercepts outgoing HTTP requests and attaches the
    /// JWT token from local storage to the Authorization header.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;

        public AuthHeaderHandler(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Get the token from local storage
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);

            // If the token exists, add it to the request's Authorization header
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Continue sending the request to the server
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
