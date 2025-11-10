using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;

namespace OmniMonitor.Client.Auth
{
    /// <summary>
    /// This DelegatingHandler intercepts outgoing HTTP requests to attach the JWT token.
    /// It performs a client-side expiration check to prevent unnecessary API calls 
    /// and intercepts server-side 401s for final security.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly NavigationManager _navigationManager;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();


        public AuthHeaderHandler(
            ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider,
            NavigationManager navigationManager)
        {
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
            _navigationManager = navigationManager;
        }

        // Metodo auxiliar para forzar el logout y la redireccion
        private async Task ForceLogoutAndRedirect()
        {
            var apiProvider = _authStateProvider as ApiAuthenticationStateProvider;

            if (apiProvider != null)
            {
                await apiProvider.NotifyUserLogout();
            }

            _navigationManager.NavigateTo("/login", replace: true);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);

            if (string.IsNullOrEmpty(token))
            {
                return await base.SendAsync(request, cancellationToken);
            }

            try
            {
                var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken != null && jsonToken.ValidTo < DateTime.UtcNow)
                {
                    await ForceLogoutAndRedirect();
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
            catch (ArgumentException)
            {
                await ForceLogoutAndRedirect();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);


            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ForceLogoutAndRedirect();

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return response;
        }
    }
}
