using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace OmniMonitor.Client.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public ApiAuthenticationStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Anonymous user
                }

                var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken == null || jsonToken.ValidTo < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Expired or invalid token
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
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Error state
            }
        }

        public async Task NotifyUserAuthentication(string token)
        {
            await _localStorage.SetItemAsync("authToken", token);
            var authState = await GetAuthenticationStateAsync();
            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task NotifyUserLogout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }
    }
}

