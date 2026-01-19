using MediaBridge.Configuration;
using MediaBridge.Services.Helpers;
using Microsoft.Extensions.Options;

namespace MediaBridge.Services.Media.ExternalServices.Sonarr
{
    public interface ISonarrHttp
    {
        Task<HttpResponseString> SonarrHttpPost(string endpoint, string payload);
        Task<HttpResponseString> SonarrHttpGet(string endpoint, int tvdbId);
    }

    public class SonarrHttp : ISonarrHttp
    {
        private readonly IHttpClientService _httpClientService;
        private readonly SonarrOptions _options;

        public SonarrHttp(IHttpClientService httpClientService, IOptions<SonarrOptions> options)
        {
            _httpClientService = httpClientService;
            _options = options.Value;
        }

        public async Task<HttpResponseString> SonarrHttpPost(string endpoint, string payload)
        {
            var url = BuildUrl(endpoint);
            return await _httpClientService.PostAsync(url, payload);
        }

        public async Task<HttpResponseString> SonarrHttpGet(string endpoint, int id)
        {
            var url = BuildUrl(endpoint);
            return await _httpClientService.GetAsync(url);
        }

        private string BuildUrl(string endpoint)
        {
            // Replace the API key placeholder
            endpoint = endpoint.Replace("{ApiKey}", _options.ApiKey);
              
            // Combine base URL with the endpoint
            return _options.BaseUrl + endpoint;
        }
    }
}
