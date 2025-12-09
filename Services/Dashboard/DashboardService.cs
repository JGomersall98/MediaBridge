using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Dashboard;
using MediaBridge.Services.Helpers;
using MediaBridge.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly MediaBridgeDbContext _db;
        private readonly IHttpClientService _httpClientService;
        private readonly ILogger<DashboardService> _logger;
        private readonly ICaching _caching;
        private readonly IGetConfig _config;
        private const int CACHE_DURATION_HOURS = 12;
        private const string MOVIES_CACHE_KEY = "top_movies";
        private const string TVSHOWS_CACHE_KEY = "top_tvshows";
        private const string MOVIES_ENDPOINT_KEY = "mdblist_movies_endpoint";
        private const string TVSHOWS_ENDPOINT_KEY = "mdblist_tvshows_endpoint";
        private const string MEDIA_TMBDID_INFO = "media_tmbd_id_endpoint";
        private readonly JsonSerializerOptions _jsonOptions;

        public DashboardService(MediaBridgeDbContext db, ILogger<DashboardService> logger, 
            ICaching caching, IHttpClientService httpClientService, IGetConfig config )
        {
            _db = db;
            _logger = logger;
            _caching = caching;
            _httpClientService = httpClientService;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<DashboardMoviesResponse> GetTopMoviesAsync()
        {
            var response = new DashboardMoviesResponse();

            try
            {
                var cachedData = await _caching.GetCachedDataAsync(MOVIES_CACHE_KEY);

                if (cachedData != null)
                {
                    return BuildDashboardMoviesResponse(cachedData, true);
                }

                response.IsSuccess = false;
                response.Reason = "Failed to fetch movie data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
                response.IsSuccess = false;
                response.Reason = "Movie cache failed";
                return response;
            }
        }
        public async Task<DashboardTvShowsResponse> GetTopTvShowsAsync()
        {
            try
            {
                var cachedData = await _caching.GetCachedDataAsync(TVSHOWS_CACHE_KEY);

                if (cachedData != null)
                {
                    return BuildDashboardTvShowsResponse(cachedData, true);
                }

                var response = new DashboardTvShowsResponse();
                response.IsSuccess = false;
                response.Reason = "Failed to fetch TV show data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top TV shows");
                var response = new DashboardTvShowsResponse();
                response.IsSuccess = false;
                response.Reason = "Tv show cache failed";
                return response;
            }
        }


        public async Task<MdbListApiResponse?> GetMoreTmdIdForMediaAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            if (mediaType == "movie")
            {
                List<MediaMovieInfo> mediaMovieInfos = await GetTmdIdForMovieAsync(mdbListApiResponse, mediaType);
            }
            else if (mediaType == "show")
            {
                List<MediaMovieInfo> mediaTvShowInfos = await GetTmdIdForTvShowAsync(mdbListApiResponse, mediaType);
            }
            else
            {
                _logger.LogError("Invalid media type specified for Tmdb ID retrieval.");
                return mdbListApiResponse;
            }

            return mdbListApiResponse;
        }
        private async Task<List<MediaMovieInfo>> GetTmdIdForMovieAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            // Get all the imdb from the movie items
            List<string> imdbIds = mdbListApiResponse.Movies.Select(m => m.ImdbId!).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            // Prepare the request payload
            var requestData = new { ids = imdbIds };

            // Serialize the request payload to JSON
            string jsonBody = JsonSerializer.Serialize(requestData, _jsonOptions);

            string movieUrl = await BuildTmbdIdInfoEndpoint(mediaType);

            // Make the POST request using the correct IHttpClientService method
            var httpResponse = await _httpClientService.PostStringAsync(movieUrl, jsonBody);

            // deserialize the response
            List<MediaMovieInfo> mediaMovieInfos = JsonSerializer.Deserialize<List<MediaMovieInfo>>(httpResponse.Response, _jsonOptions);

            if (mediaMovieInfos == null)
            {
                mediaMovieInfos = new List<MediaMovieInfo>();
                return mediaMovieInfos;
            }

            // add the tmbid into the original movie items
            foreach (var movie in mdbListApiResponse.Movies)
            {
                var info = mediaMovieInfos.FirstOrDefault(m => m.InfoIds!.ImdbId == movie.ImdbId);
                if (info != null)
                {
                    movie.TmdbId = info.InfoIds!.TmdbId;
                }
            }
            Console.WriteLine("Completed fetching Tmdb IDs for movies.");
            return mediaMovieInfos;
        }
        private async Task<List<MediaMovieInfo>> GetTmdIdForTvShowAsync(MdbListApiResponse mdbListApiResponse, string mediaType)
        {
            // Get all the imdb from the tv show items
            List<string> imdbIds = mdbListApiResponse.Shows.Select(m => m.ImdbId!).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            
            // Prepare the request payload
            var requestData = new { ids = imdbIds };
            
            // Serialize the request payload to JSON
            string jsonBody = JsonSerializer.Serialize(requestData, _jsonOptions);
            string tvUrl = await BuildTmbdIdInfoEndpoint(mediaType);
            
            // Make the POST request using the correct IHttpClientService method
            var httpResponse = await _httpClientService.PostStringAsync(tvUrl, jsonBody);
            
            // deserialize the response
            List<MediaMovieInfo> mediaMovieInfos = JsonSerializer.Deserialize<List<MediaMovieInfo>>(httpResponse.Response, _jsonOptions);
            
            if (mediaMovieInfos == null)
            {
                mediaMovieInfos = new List<MediaMovieInfo>();
                return mediaMovieInfos;
            }

            // add the tmbid into the original tv show items
            foreach (var show in mdbListApiResponse.Shows)
            {
                var info = mediaMovieInfos.FirstOrDefault(m => m.InfoIds!.ImdbId == show.ImdbId);
                if (info != null)
                {
                    show.TmdbId = info.InfoIds!.TmdbId;
                }
            }

            Console.WriteLine("Completed fetching Tmdb IDs for TV shows.");
            return mediaMovieInfos;
        }
        private async Task<string> BuildTmbdIdInfoEndpoint(string mediaType)
        {
            string apiUrl = await _config.GetConfigValueAsync(MEDIA_TMBDID_INFO);

            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogError("MDBList media TMBD ID endpoint is not configured.");
                return string.Empty;
            }

            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");

            if(mediaType == "movie")
            {
                apiUrl = apiUrl.Replace("{mediaType}", mediaType) + apiKey;
            }
            else if(mediaType == "show")
            {
                apiUrl = apiUrl.Replace("{mediaType}", mediaType) + apiKey;
            }
            return apiUrl;
        }
        private DashboardTvShowsResponse BuildDashboardTvShowsResponse(object data, bool isCachedResponse)
        {
            DashboardTvShowsResponse response = new DashboardTvShowsResponse();

            if (isCachedResponse && data is CachedData cachedData)
            {
                var tvData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, _jsonOptions);
                if (tvData != null)
                {
                    response.Shows = tvData.Shows;
                    response.LastUpdated = cachedData.CachedAt;
                }
            }
            else if (!isCachedResponse && data is MdbListApiResponse freshData)
            {
                response.Shows = freshData.Shows;
                response.LastUpdated = DateTime.UtcNow;
            }

            response.FromCache = isCachedResponse;
            response.IsSuccess = true;
            return response;
        }
        private DashboardMoviesResponse BuildDashboardMoviesResponse(object data, bool isCachedResponse)
        {
            DashboardMoviesResponse response = new DashboardMoviesResponse();

            if (isCachedResponse && data is CachedData cachedData)
            {
                var movieData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, _jsonOptions);
                if (movieData != null)
                {
                    response.Movies = movieData.Movies;
                    response.LastUpdated = cachedData.CachedAt;
                }
            }
            else if (!isCachedResponse && data is MdbListApiResponse freshData)
            {
                response.Movies = freshData.Movies;
                response.LastUpdated = DateTime.UtcNow;
            }

            response.FromCache = isCachedResponse;
            response.IsSuccess = true;
            return response;
        }
        private async Task<MdbListApiResponse?> FetchMediaFromApiAsync(string? apiUrl, string mediaType)
        {
            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing MDBList configuration");
                return null;
            }

            var fullUrl = $"{apiUrl}{apiKey}";

            var httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            var deserializedResponse = JsonSerializer.Deserialize<MdbListApiResponse>(httpResponse, _jsonOptions);
            if (deserializedResponse == null)
            {
                _logger.LogError("Failed to deserialize MdbListApiResponse from API response.");
                return null;
            }
            MdbListApiResponse response = await GetMoreTmdIdForMediaAsync(deserializedResponse, mediaType);
            return response;
        }



        // Refresh cache methods called by a scheduled job
        public async Task RefreshCaches()
        {
            await RefreshTopMoviesCacheAsync();

            await RefreshTopShowsCacheAsync();

        }
        private async Task RefreshTopMoviesCacheAsync()
        {
            try
            {
                string apiUrl = await _config.GetConfigValueAsync(MOVIES_ENDPOINT_KEY);
                var freshData = await FetchMediaFromApiAsync(apiUrl, "movie");
                if (freshData != null)
                {
                    await _caching.CacheDataAsync(MOVIES_CACHE_KEY, JsonSerializer.Serialize(freshData, _jsonOptions), CACHE_DURATION_HOURS);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
            }
        }
        private async Task RefreshTopShowsCacheAsync()
        {
            try
            {
                string apiUrl = await _config.GetConfigValueAsync(TVSHOWS_ENDPOINT_KEY);

                var freshData = await FetchMediaFromApiAsync(apiUrl, "show");
                if (freshData != null)
                {
                    string jsonData = JsonSerializer.Serialize(freshData, _jsonOptions);
                    await _caching.CacheDataAsync(TVSHOWS_CACHE_KEY, jsonData, CACHE_DURATION_HOURS);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top TV shows");
            }
        }
    }
    public class GetImdbDataResposne
    {
        [JsonPropertyName("ids")]
        public List<GetImdbDataResposneIds> Ids { get; set; }

    }
    public class GetImdbDataResposneIds
    {
        [JsonPropertyName("imdb")]
        public string ImdbId { get; set; }
        [JsonPropertyName("tmdb")]
        public int TmdbId { get; set; }
    }
}