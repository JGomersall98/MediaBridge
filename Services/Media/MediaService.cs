using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Services.Helpers;

namespace MediaBridge.Services.Media
{
    public class MediaService : IMediaService
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _apiKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly MediaBridgeDbContext _db;
        public MediaService(IGetConfig config, IHttpClientService httpClientService, MediaBridgeDbContext db)
        {
            _config = config;
            _httpClientService = httpClientService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            _db = db;
        }

        public async Task<StandardResponse> DownloadMovie(int tmbId, int userId, string username)
        {
            StandardResponse response = new StandardResponse();

            RadarrGetMovieResponse movieDetails = await GetMovieDetails(tmbId);

            HttpResponseString requestResponse = await SendMovieRequest(movieDetails);

            if (!requestResponse.IsSuccess)
            {
                RadarrErrorResponse error = JsonSerializer.Deserialize<List<RadarrErrorResponse>>(requestResponse.Response, _jsonOptions)!.FirstOrDefault();
                response.IsSuccess = false;
                response.Reason = $"Failed sending movie request to Radarr: {error?.ErrorMessage ?? "Unknown error"}";

                await LogMediaRequest(userId, username, tmbId, "movie", movieDetails.Title!, false, error?.ErrorMessage);

                return response;
            }

            response.IsSuccess = true;

            await LogMediaRequest(userId, username, tmbId, "movie", movieDetails.Title!, true, null);

            return response;
        }
        private async Task LogMediaRequest(int userId, string username, int tmdbId, string mediaType, string mediaTitle, bool isSuccessful, string? errorMessage)
        {
            try
            {
                var logEntry = new MediaRequestLog
                {
                    UserId = userId,
                    Username = username,
                    TmdbId = tmdbId,
                    MediaType = mediaType,
                    MediaTitle = mediaTitle,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = errorMessage,
                    RequestedAt = DateTime.UtcNow
                };

                _db.MediaRequestLogs.Add(logEntry);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log to console or your logging framework if database logging fails
                Console.WriteLine($"Failed to log media request: {ex.Message}");
                // Don't throw here to avoid breaking the main functionality
            }
        }
        private async Task<RadarrGetMovieResponse> GetMovieDetails(int tmbId)
        {
            string configUrl = await _config.GetConfigValueAsync("radarr_get_movie_endpoint");
            string apiUrl = configUrl!.Replace("{id}", tmbId.ToString());

            await SetRadarrApiKeyAsync();

            string response = await _httpClientService.GetStringAsync(apiUrl + _apiKey);
            List<RadarrGetMovieResponse>? movieDetailList = JsonSerializer.Deserialize<List<RadarrGetMovieResponse>>(response, _jsonOptions);
            RadarrGetMovieResponse movieDetail = movieDetailList!.FirstOrDefault();

            return movieDetail!;
        }
        private async Task<HttpResponseString> SendMovieRequest(RadarrGetMovieResponse movieDetails)
        {
            string configUrl = await _config.GetConfigValueAsync("radarr_post_movie_endpoint");

            await SetRadarrApiKeyAsync();

            string payload = JsonSerializer.Serialize(new RadarrAddMovieRequest
            {
                Title = movieDetails.Title,
                TmdbId = movieDetails.TmdbId,
                ImdbId = movieDetails.ImdbId,
                TitleSlug = movieDetails.TitleSlug
            });

            HttpResponseString response = await _httpClientService.PostStringAsync(configUrl + _apiKey, payload);
            return response;
        }


        private async Task SetRadarrApiKeyAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                var apiKey = await _config.GetConfigValueAsync("radarr_api_key");
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("radarr_api_key not found in configuration.");
                }
                _apiKey = apiKey;
            }
        }
    }
    public class RadarrGetMovieResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("year")]
        public int? Year { get; set; }
        [JsonPropertyName("tmdbId")]
        public int? TmdbId { get; set; }
        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("titleSlug")]
        public string? TitleSlug { get; set; }
    }
    public class RadarrAddMovieRequest
    {
        public string? Title { get; set; }
        public int Year { get; set; }
        public int? TmdbId { get; set; }
        public string? ImdbId { get; set; }
        public string? TitleSlug { get; set; }
        public int QualityProfileId { get; set; } = 1;
        public string? RootFolderPath { get; set; } = "/mnt/server/Movies";
        public bool Monitored { get; set; } = true;
        public string? MinimumAvailability { get; set; } = "released";
        public AddOptions? AddOptions { get; set; } = new AddOptions { SearchForMovie = true };
    }

    public class AddOptions
    {
        public bool SearchForMovie { get; set; } = true;
    }
    public class RadarrErrorResponse
    {
        public string? ErrorMessage { get; set; }
    }
}