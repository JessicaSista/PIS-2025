using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Para IServiceProvider, aunque usaremos IHttpClientFactory
using System.Net.Http;
using Microsoft.Extensions.Http; // Para IHttpClientFactory

namespace OmniMonitor.Client.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        // 💡 CAMBIO: Usamos la Factoría para crear clientes, rompiendo el ciclo de dependencia.
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        // El HttpClient ya no es un campo privado directo, se obtiene bajo demanda.

        public ApiAuthenticationStateProvider(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory)
        {
            _localStorage = localStorage;
            _httpClientFactory = httpClientFactory;
        }

        // Método auxiliar para obtener el HttpClient, usando el cliente nombrado "API"
        private HttpClient GetHttpClient()
        {
            // Usamos la factoría para crear una instancia del cliente "API".
            // Esto evita que el DI intente resolver el cliente durante la inicialización del proveedor.
            return _httpClientFactory.CreateClient("API");
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpClient = GetHttpClient();

            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");


                // Si no hay token
                if (string.IsNullOrWhiteSpace(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Anonymous user
                }

                // Si hay token lo parseamos
                var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;


                // Si el jsonToken es null o ha expirado
                if (jsonToken == null || jsonToken.ValidTo < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // The JWT token uses short claim type names (e.g., "nameid"),
                // but Blazor's authorization components expect the standard long names
                // (e.g., "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                // We manually map them here to ensure compatibility.
                var claims = new List<Claim>();
                foreach (var claim in jsonToken.Claims)
                {
                    var newClaim = claim;
                    // Map the short claim types to the standard ClaimTypes constants
                    switch (claim.Type)
                    {
                        case "nameid":
                            newClaim = new Claim(ClaimTypes.NameIdentifier, claim.Value);
                            break;
                        case "unique_name":
                            newClaim = new Claim(ClaimTypes.Name, claim.Value);
                            break;
                        case "role":
                            newClaim = new Claim(ClaimTypes.Role, claim.Value);
                            break;
                    }
                    claims.Add(newClaim);
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                return new AuthenticationState(user);
            }
            catch
            {
                await _localStorage.RemoveItemAsync("authToken");
                httpClient.DefaultRequestHeaders.Authorization = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Error state
            }
        }

        public async Task NotifyUserAuthentication(string token)
        {
            var httpClient = GetHttpClient();
            await _localStorage.SetItemAsync("authToken", token);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task NotifyUserLogout()
        {
            var httpClient = GetHttpClient();
            await _localStorage.RemoveItemAsync("authToken");
            httpClient.DefaultRequestHeaders.Authorization = null;
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }
    }
}