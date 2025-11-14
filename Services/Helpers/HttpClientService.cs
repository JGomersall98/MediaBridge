using System.Net.Http;
using System.Text.Json;
using MediaBridge.Services.Dashboard;
using Microsoft.AspNetCore.Http;

namespace MediaBridge.Services.Helpers
{
    public interface IHttpClientService
    {
        Task<string> GetStringAsync(string url);
    }
    public class HttpClientService : IHttpClientService
    {
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly ILogger<HttpClient> _logger;

        public HttpClientService(System.Net.Http.HttpClient httpClient, ILogger<HttpClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<string> GetStringAsync(string url)
        {
            try
            {
                _logger.LogInformation("GET string request to: {Url}", url);

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();

                    return jsonContent;
                }

                _logger.LogWarning("GET request failed. Status: {StatusCode}, Url: {Url}", response.StatusCode, url);

                return "GET request failed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GET string request to {Url}", url);
                throw;
            }
        }
    }
}