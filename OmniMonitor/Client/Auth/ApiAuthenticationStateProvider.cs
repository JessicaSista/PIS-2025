using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;

using Blazored.LocalStorage;

using Microsoft.AspNetCore.Components.Authorization;

// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using System.Linq;
namespace OmniMonitor.Client.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly HttpClient _httpClient;

        public ApiAuthenticationStateProvider(ILocalStorageService localStorage, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                string? token = await _localStorage.GetItemAsync<string>("authToken");

                // Si no hay token
                if (string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Anonymous user
                }

                // Si hay token lo parseamos
                // var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;
                // Si el jsonToken es null o ha expirado
                // if (jsonToken == null || jsonToken.ValidTo < DateTime.UtcNow)
                if (_tokenHandler.ReadToken(token) is not JwtSecurityToken jsonToken || jsonToken.ValidTo < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    _httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // The JWT token uses short claim type names (e.g., "nameid"),
                // but Blazor's authorization components expect the standard long names
                // (e.g., "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").
                // We manually map them here to ensure compatibility.
                var claims = new List<Claim>();
                foreach (Claim claim in jsonToken.Claims)
                {
                    Claim newClaim = claim;

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
                        default:
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
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Error state
            }
        }

        public async Task NotifyUserAuthentication(string token)
        {
            await _localStorage.SetItemAsync("authToken", token);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            AuthenticationState authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task NotifyUserLogout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            Task<AuthenticationState> authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }
    }
}