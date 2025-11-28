using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Services.Helpers;
using Microsoft.AspNetCore.WebUtilities;

namespace MediaBridge.Services.Media
{
    public class MediaService : IMediaService
    {
        private readonly IGetConfig _config;
        private readonly IHttpClientService _httpClientService;
        private string? _radarrApiKey;
        private string? _sonarrApiKey;
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
        public async Task<StandardResponse> DownloadMedia(int tmbId, int userId, string username, int[]? seasonsRequested, string mediaType)
        {
            StandardResponse response = new StandardResponse();

            RSGetMediaResponse movieDetails = await GetMediaDetails(tmbId, mediaType);

            HttpResponseString requestResponse = new HttpResponseString();

            if (mediaType == "movie")
            {
                requestResponse = await SendMovieRequest(movieDetails);
            }
            else if (mediaType == "show")
            {
                if (seasonsRequested == null)
                {
                    response.Reason = "No Seasons Requested.";
                    response.IsSuccess = false;
                    return response;
                }
                requestResponse = await SendShowRequest(movieDetails, seasonsRequested);
            }

            if (!requestResponse.IsSuccess)
            {
                response = await ErrorResponse(userId, username, tmbId, mediaType, movieDetails.Title!, requestResponse.Response);
                return response;
            }

            response.IsSuccess = true;

            await LogMediaRequest(userId, username, tmbId, mediaType, movieDetails.Title!, true, null);

            return response;
        }      
        private async Task<StandardResponse> ErrorResponse(int userId, string username, int tmbId, string mediaType, string mediaTitle, string responseString)
        {
            StandardResponse response = new StandardResponse();
            MediaErrorResponse error = JsonSerializer.Deserialize<List<MediaErrorResponse>>(responseString, _jsonOptions)!.FirstOrDefault();
            response.IsSuccess = false;
            response.Reason = $"Failed sending {mediaType} request: {error?.ErrorMessage ?? "Unknown error"}";

            await LogMediaRequest(userId, username, tmbId, mediaType, mediaTitle, false, error?.ErrorMessage);

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
                Console.WriteLine($"Failed to log media request: {ex.Message}");
            }
        }
        private async Task<RSGetMediaResponse> GetMediaDetails(int tmbId, string media)
        {
            string apiUrl = "";
            if (media == "movie")
            {
                apiUrl = await BuildApiUrl("radarr_get_movie_endpoint", "movie", tmbId);
            }
            else if (media == "show")
            {
                apiUrl = await BuildApiUrl("sonarr_get_show_endpoint", "show", tmbId);
            }
            string response = await _httpClientService.GetStringAsync(apiUrl);

            List<RSGetMediaResponse>? movieDetailList = JsonSerializer.Deserialize<List<RSGetMediaResponse>>(response, _jsonOptions);
            RSGetMediaResponse movieDetail = movieDetailList!.FirstOrDefault();
            return movieDetail!;
        }
        private async Task<string> BuildApiUrl(string urlConfigValue, string media, int mediaId)
        {
            string configUrl = await _config.GetConfigValueAsync(urlConfigValue);
            string apiKey = "";
            if (media == "movie")
            {
                await SetRadarrApiKeyAsync();
                apiKey = _radarrApiKey;
            }
            else if (media == "show")
            {
                await SetSonarrApiKeyAsync();
                apiKey = _sonarrApiKey;
            }
            string apiUrl = configUrl!.Replace("{id}", mediaId.ToString()) + apiKey;
            return apiUrl;
        }
        private async Task<HttpResponseString> SendMovieRequest(RSGetMediaResponse movieDetails)
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

            HttpResponseString response = await _httpClientService.PostStringAsync(configUrl + _radarrApiKey, payload);
            return response;
        }
        private async Task<HttpResponseString> SendShowRequest(RSGetMediaResponse movieDetails, int[] seasonsRequested)
        {
            string configUrl = await _config.GetConfigValueAsync("sonarr_post_show_endpoint");

            await SetSonarrApiKeyAsync();

            List<Season> seasonList = new List<Season>();

            foreach (var seasonItem in seasonsRequested)
            {
                Season season = new Season()
                {
                    SeasonNumber = seasonItem,
                    Monitored = true
                };
                seasonList.Add(season);
            }

            string payload = JsonSerializer.Serialize(new SonarrAddShowRequest
            {
                Title = movieDetails.Title,
                TvdbId = movieDetails.TvdbId,
                TitleSlug = movieDetails.TitleSlug,
                Seasons = seasonList
            });

            HttpResponseString response = await _httpClientService.PostStringAsync(configUrl + _sonarrApiKey, payload);
            return response;
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
    public class RSGetMediaResponse
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        [JsonPropertyName("tmdbId")]
        public int? TmdbId { get; set; } // Movie
        [JsonPropertyName("tvdbId")]
        public int? TvdbId { get; set; } // Tv Show
        [JsonPropertyName("imdbId")]
        public string? ImdbId { get; set; }
        [JsonPropertyName("titleSlug")]
        public string? TitleSlug { get; set; }
    }
    public class RadarrAddMovieRequest
    {
        public string? Title { get; set; }
        public int? TmdbId { get; set; }
        public string? ImdbId { get; set; }
        public string? TitleSlug { get; set; }
        public int QualityProfileId { get; set; } = 1;
        public string? RootFolderPath { get; set; } = "/mnt/server/Movies";
        public bool Monitored { get; set; } = true;
        public string? MinimumAvailability { get; set; } = "released";
        public AddOptions? AddOptions { get; set; } = new AddOptions { SearchForMovie = true };
    }
    public class SonarrAddShowRequest
    {
        public string? Title { get; set; }
        public string? TitleSlug { get; set; }
        public int? TvdbId { get; set; }
        public int QualityProfileId { get; set; } = 1;
        public string? RootFolderPath { get; set; } = "/mnt/server/'TV Shows'";
        public bool Monitored { get; set; } = true;
        public List<Season>? Seasons { get; set; }
        public AddOptions? AddOptions { get; set; } = new AddOptions { SearchForMissingEpisodes = true, 
                                                                        SearchForCutoffUnmetEpisodes = true };
    }
    public class AddOptions
    {
        public bool? SearchForMovie { get; set; } = true;
        public bool? SearchForMissingEpisodes { get; set; } = true;
        public bool? SearchForCutoffUnmetEpisodes { get; set; } = true;
    }
    public class Season
    {
        public int? SeasonNumber { get; set; }
        public bool? Monitored { get; set; }
    }
    public class MediaErrorResponse
    {
        public string? ErrorMessage { get; set; }
    }
}