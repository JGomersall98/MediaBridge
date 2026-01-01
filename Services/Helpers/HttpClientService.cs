using System.Net.Http;
using System.Text.Json;
using MediaBridge.Services.Dashboard;
using Microsoft.AspNetCore.Http;

namespace MediaBridge.Services.Helpers
{
    public interface IHttpClientService
    {
        Task<string> GetStringAsync(string url);
        Task<string> GetStringAsync(string url, Dictionary<string, string> headers);
        Task<HttpResponseString> PostStringAsync(string url, string jsonBody);
        Task<HttpResponseString> PostFormUrlEncodedAsync(string url, Dictionary<string, string> formData);
        Task<string> DeleteStringAsync(string url);
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
        public async Task<string> GetStringAsync(string url, Dictionary<string, string> headers)
        {
            try
            {
                _logger.LogInformation("GET string request to: {Url} with headers", url);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                var response = await _httpClient.SendAsync(request);
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
        public async Task<HttpResponseString> PostFormUrlEncodedAsync(string url, Dictionary<string, string> formData)
        {
            HttpResponseString postResponse = new HttpResponseString();
            try
            {
                _logger.LogInformation("POST form-urlencoded request to: {Url}", url);
                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(url, content);

                // Extract SID cookie from response headers if exists
                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    foreach (var cookie in cookies)
                    {
                        if (cookie.StartsWith("SID="))
                        {
                            // Extract the SID value (everything after "SID=" until the first semicolon)
                            var sidValue = cookie.Substring(4).Split(';')[0];
                            postResponse.SidCookie = sidValue;
                            _logger.LogInformation("Extracted SID cookie: {SidValue}", sidValue);
                            break;
                        }
                    }
                }

                postResponse.IsSuccess = response.IsSuccessStatusCode;
                postResponse.Response = await response.Content.ReadAsStringAsync();
                return postResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POST form-urlencoded request to {Url}", url);
                throw;
            }
        }
        public async Task<string> DeleteStringAsync(string url)
        {
            try
            {
                _logger.LogInformation("DELETE string request to: {Url}", url);
                var response = await _httpClient.DeleteAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return jsonContent;
                }
                _logger.LogWarning("DELETE request failed. Status: {StatusCode}, Url: {Url}", response.StatusCode, url);
                return "DELETE request failed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DELETE string request to {Url}", url);
                throw;
            }
        }
    }
    public class HttpResponseString
    {
        public string Response { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? SidCookie { get; set; }
    }
}