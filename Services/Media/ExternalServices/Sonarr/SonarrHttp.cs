using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media.ExternalServices.Sonarr
{
    public interface ISonarrHttp
    {
        Task<HttpResponseString> SonarrHttpPost(string configKey, string payload);
        Task<HttpResponseString> SonarrHttpGet(string configKey, int tvdbId);
    }
    public class SonarrHttp : ISonarrHttp
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _sonarrApiKey;

        public SonarrHttp(IGetConfig config, IHttpClientService httpClientService)
        {
            _config = config;
            _httpClientService = httpClientService;
        }

        // Public Methods
        public async Task<HttpResponseString> SonarrHttpPost(string configKey, string payload)
        {
            // Build Sonarr URL
            string url = await BuildSonarrUrl(configKey);

            // Make POST request to Sonarr
            HttpResponseString response = await _httpClientService.PostAsync(url, payload);

            return response;
        }
        public async Task<HttpResponseString> SonarrHttpGet(string configKey, int id)
        {
            // Build Sonarr URL
            string url = await BuildSonarrUrl(configKey);

            // Replace {tvdbId} placeholder with actual id
            url = url.Replace("{id}", id.ToString());

            // Make GET request to Sonarr
            HttpResponseString response = await _httpClientService.GetAsync(url);

            return response;
        }



        // Private Methods
        private async Task<String> BuildSonarrUrl(string configKey)
        {
            // Get Sonarr URL from config
            string configUrl = await _config.GetConfigValueAsync(configKey);

            // Ensure API key is set
            await SetSonarrApiKeyAsync();

            // Replace {apiKey} placeholder with actual API key
            return configUrl!.Replace("{apiKey}", _sonarrApiKey!);
        }
        private async Task SetSonarrApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_sonarrApiKey))
            {
                var apiKey = await _config.GetConfigValueAsync("sonarr_api_key");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("sonarr_api_key not found in configuration.");
                }
                _sonarrApiKey = apiKey;
            }
        }
    }
}
