using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Microsoft.Extensions.Http;

namespace OmniMonitor.Client.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public ApiAuthenticationStateProvider(ILocalStorageService localStorage, IHttpClientFactory httpClientFactory)
        {
            _localStorage = localStorage;
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetHttpClient()
        {
            return _httpClientFactory.CreateClient("API");
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpClient = GetHttpClient();

            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");

                if (string.IsNullOrWhiteSpace(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var jsonToken = _tokenHandler.ReadToken(token) as JwtSecurityToken;

                if (jsonToken == null || jsonToken.ValidTo < DateTime.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    httpClient.DefaultRequestHeaders.Authorization = null;
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Map JWT short claim names to Blazor's expected long names
                var claims = new List<Claim>();
                foreach (var claim in jsonToken.Claims)
                {
                    var newClaim = claim;
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
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
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
