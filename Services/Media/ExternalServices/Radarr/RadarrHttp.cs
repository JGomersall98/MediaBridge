using MediaBridge.Database.DB_Models;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media.ExternalServices.Radarr
{
    public interface IRadarrHttp
    {
        Task<HttpResponseString> RadarrHttpPost(string configKey, string payload);
        Task<HttpResponseString> RadarrHttpGet(string configKey, int id);
    }
    public class RadarrHttp : IRadarrHttp
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _radarrApiKey;

        public RadarrHttp(IGetConfig config, IHttpClientService httpClientService)
        {
            _config = config;
            _httpClientService = httpClientService;
        }

        // Public Methods
        public async Task<HttpResponseString> RadarrHttpPost(string configKey, string payload)
        {
            // Build Radarr URL
            string url = await BuildRadarrUrl(configKey);

            // Make POST request to Radarr
            HttpResponseString response = await _httpClientService.PostAsync(url, payload);

            return response;
        }
        public async Task<HttpResponseString> RadarrHttpGet(string configKey, int id)
        {
            // Build Radarr URL
            string url = await BuildRadarrUrl(configKey);

            // Replace {id} placeholder with actual id
            url = url.Replace("{id}", id.ToString());

            // Make GET request to Radarr
            HttpResponseString response = await _httpClientService.GetAsync(url);

            return response;
        }



        // Private Methods
        private async Task<String> BuildRadarrUrl(string configKey)
        {
            // Get Radarr URL from config
            string configUrl = await _config.GetConfigValueAsync(configKey);

            // Ensure API key is set
            await SetRadarrApiKeyAsync();

            // Replace {apiKey} placeholder with actual API key
            return configUrl!.Replace("{apiKey}", _radarrApiKey!);
        }
        private async Task SetRadarrApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_radarrApiKey))
            {
                var apiKey = await _config.GetConfigValueAsync("radarr_api_key");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("radarr_api_key not found in configuration.");
                }
                _radarrApiKey = apiKey;
            }
        }
    }
}
