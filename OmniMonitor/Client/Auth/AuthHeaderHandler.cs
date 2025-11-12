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
        // Instancia para leer el JWT, similar a tu ApiAuthenticationStateProvider
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

        // Método auxiliar para forzar el logout y la redirección
        private async Task ForceLogoutAndRedirect()
        {
            var apiProvider = _authStateProvider as ApiAuthenticationStateProvider;

            if (apiProvider != null)
            {
                await apiProvider.NotifyUserLogout();
            }

            // Redirigir al usuario
            _navigationManager.NavigateTo("/login", replace: true);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken", cancellationToken);

            // --- 1. Chequeo de Token Nulo o Vacío ---
            if (string.IsNullOrEmpty(token))
            {
                // Si no hay token, simplemente pasamos la petición.
                return await base.SendAsync(request, cancellationToken);
            }

            // --- 2. PRE-CHEQUEO CLIENTE (Ahorro de Llamada al Backend) ---
            try
            {
                var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken != null && jsonToken.ValidTo < DateTime.UtcNow)
                {
                    // Token expirado localmente. Evitamos la llamada al backend.
                    await ForceLogoutAndRedirect();

                    // Devolvemos el 401 localmente para finalizar el proceso.
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
            catch (ArgumentException)
            {
                // Si el token no puede ser leído (formato inválido, etc.), lo tratamos como expirado.
                await ForceLogoutAndRedirect();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            // --- 3. Lógica de Petición Saliente (Token válido) ---
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 4. Continuar enviando la petición y esperar la respuesta
            var response = await base.SendAsync(request, cancellationToken);

            // --- 5. POST-CHEQUEO SERVIDOR (Manejo de 401 por Clock Skew o Revocación) ---

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // El servidor es la autoridad final. Si dice 401, lo honramos.
                await ForceLogoutAndRedirect();

                // Devolvemos el 401 para detener el procesamiento de éxito en el componente de origen.
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            return response;
        }
    }
}