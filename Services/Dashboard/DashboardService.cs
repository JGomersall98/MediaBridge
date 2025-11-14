using System.Text.Json;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Dashboard;
using MediaBridge.Services.Helpers;
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
                string apiUrl = await _config.GetConfigValueAsync(MOVIES_ENDPOINT_KEY);
                var freshData = await FetchMediaFromApiAsync(apiUrl);
                if (freshData != null)
                {
                    await _caching.CacheDataAsync(MOVIES_CACHE_KEY, JsonSerializer.Serialize(freshData, _jsonOptions), CACHE_DURATION_HOURS);
                    return BuildDashboardMoviesResponse(freshData, false);
                }

                response.IsSuccess = false;
                response.Reason = "Failed to fetch movie data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top movies");
                response.IsSuccess = false;
                response.Reason = "Internal server error";
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

                string apiUrl = await _config.GetConfigValueAsync(TVSHOWS_ENDPOINT_KEY);

                var freshData = await FetchMediaFromApiAsync(apiUrl);
                if (freshData != null)
                {
                    string jsonData = JsonSerializer.Serialize(freshData, _jsonOptions);
                    await _caching.CacheDataAsync(TVSHOWS_CACHE_KEY, jsonData, CACHE_DURATION_HOURS);

                    return BuildDashboardTvShowsResponse(freshData, false);
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
                response.Reason = "Internal server error";
                return response;
            }
        }
        public async Task<bool> RefreshMovieCacheAsync()
        {
            try
            {
                string apiUrl = await _config.GetConfigValueAsync(MOVIES_ENDPOINT_KEY);
                var freshData = await FetchMediaFromApiAsync(apiUrl);
                if (freshData != null)
                {
                    string jsonData = JsonSerializer.Serialize(freshData, _jsonOptions);
                    await _caching.CacheDataAsync(MOVIES_CACHE_KEY, jsonData, CACHE_DURATION_HOURS);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing movie cache");
                return false;
            }
        }

        public async Task<bool> RefreshTvShowCacheAsync()
        {
            var apiUrl = await _config.GetConfigValueAsync(TVSHOWS_ENDPOINT_KEY);
            try
            {
                var freshData = await FetchMediaFromApiAsync(apiUrl);
                if (freshData != null)
                {
                    string jsonData = JsonSerializer.Serialize(freshData, _jsonOptions);
                    await _caching.CacheDataAsync(TVSHOWS_CACHE_KEY, jsonData, CACHE_DURATION_HOURS);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing TV show cache");
                return false;
            }
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

        private async Task<MdbListApiResponse?> FetchMediaFromApiAsync(string? apiUrl)
        {
            var apiKey = await _config.GetConfigValueAsync("mdblist_api_key");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing MDBList configuration");
                return null;
            }

            var fullUrl = $"{apiUrl}{apiKey}";

            var httpResponse = await _httpClientService.GetStringAsync(fullUrl);
            MdbListApiResponse response = JsonSerializer.Deserialize<MdbListApiResponse>(httpResponse, _jsonOptions);
            return response;
        }
    }
}