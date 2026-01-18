using MediaBridge.Configuration;
using MediaBridge.Database.DB_Models;
using MediaBridge.Services.Helpers;
using Microsoft.Extensions.Options;

namespace MediaBridge.Services.Media.ExternalServices.Radarr
{
    public interface IRadarrHttp
    {
        Task<HttpResponseString> RadarrHttpPost(string endpoint, string payload);
        Task<HttpResponseString> RadarrHttpGet(string endpoint, int id);
    }

    public class RadarrHttp : IRadarrHttp
    {
        private readonly IHttpClientService _httpClientService;
        private readonly RadarrOptions _options;

        public RadarrHttp(IHttpClientService httpClientService, IOptions<RadarrOptions> options)
        {
            _httpClientService = httpClientService;
            _options = options.Value;
        }

        // Public Methods
        public async Task<HttpResponseString> RadarrHttpPost(string endpoint, string payload)
        {
            var url = BuildUrl(endpoint);
            return await _httpClientService.PostAsync(url, payload);
        }

        public async Task<HttpResponseString> RadarrHttpGet(string endpoint, int id)
        {
            // The endpoint should already have the ID replaced, so we just build the URL
            var url = BuildUrl(endpoint);
            return await _httpClientService.GetAsync(url);
        }

        // Private Methods
        private string BuildUrl(string endpointTemplate)
        {
            // Start with the endpoint template
            var url = endpointTemplate;

            // Replace the API key placeholder
            url = url.Replace("{ApiKey}", _options.ApiKey);

            // Combine base URL with the endpoint
            return $"{_options.BaseUrl.TrimEnd('/')}{url}";
        }
    }
}
