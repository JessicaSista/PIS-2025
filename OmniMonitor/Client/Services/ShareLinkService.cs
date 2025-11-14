using System.Net.Http.Json;
using Blazored.LocalStorage;
using OmniMonitor.Shared.Dtos;

namespace OmniMonitor.Client.Services
{
    public class ShareLinkService
    {
        private readonly HttpClient _http;
        private readonly ILocalStorageService _localStorage;

        public ShareLinkService(HttpClient http, ILocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        public async Task<List<ShareResponseDto>> GetShares(int dashboardId)
        {
            var url = $"api/Dashboard/getShares/{dashboardId}/share";
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error fetching share links: {response.ReasonPhrase}. {error}");
            }
            var data = await response.Content.ReadFromJsonAsync<List<ShareResponseDto>>();
            return data ?? new List<ShareResponseDto>();
        }

        public async Task<ShareResponseDto> CreateShare(int dashboardId, ShareRequestDto request)
        {
            var url = $"api/Dashboard/createShare/{dashboardId}/share";
            var response = await _http.PostAsJsonAsync(url, request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error creating share link: {response.ReasonPhrase}. {error}");
            }
            var data = await response.Content.ReadFromJsonAsync<ShareResponseDto>();
            if (data == null) throw new HttpRequestException("Empty response when creating share link");
            return data;
        }

        public async Task<ShareResponseDto?> UpdateShare(string slug, ShareRequestDto request)
        {
            var url = $"api/Dashboard/UpdateShare/{slug}";
            var response = await _http.PutAsJsonAsync(url, request);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error updating share link: {response.ReasonPhrase}. {error}");
            }
            return await response.Content.ReadFromJsonAsync<ShareResponseDto>();
        }

        public async Task DeleteShare(string slug)
        {
            var url = $"api/Dashboard/DeleteShare/{slug}";
            var response = await _http.DeleteAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error deleting share link: {response.ReasonPhrase}. {error}");
            }
        }
    }
}
