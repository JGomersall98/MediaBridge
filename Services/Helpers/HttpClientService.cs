using System.Net.Http;
using System.Text.Json;
using MediaBridge.Services.Dashboard;
using Microsoft.AspNetCore.Http;

namespace MediaBridge.Services.Helpers
{
    public interface IHttpClientService
    {
        Task<string> GetStringAsync(string url);
        Task<HttpResponseString> PostStringAsync(string url, string jsonBody);
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
        public async Task<HttpResponseString> PostStringAsync(string url, string jsonBody)
        {
            HttpResponseString postResponse = new HttpResponseString();
            try
            {
                _logger.LogInformation("POST string request to: {Url}", url);
         
                var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);
                postResponse.IsSuccess = response.IsSuccessStatusCode;
 
                postResponse.Response = await response.Content.ReadAsStringAsync();
                return postResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POST string request to {Url}", url);
                throw;
            }
        }
    }
    public class HttpResponseString
    {
        public string Response { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }
}