using System.Text.Json;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly MediaBridgeDbContext _db;
        private readonly HttpClient _httpClient;
        private readonly ILogger<DashboardService> _logger;
        private const int CACHE_DURATION_HOURS = 12;
        private const string MOVIES_CACHE_KEY = "top_movies";
        private const string TVSHOWS_CACHE_KEY = "top_tvshows";

        public DashboardService(MediaBridgeDbContext db, HttpClient httpClient, ILogger<DashboardService> logger)
        {
            _db = db;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<DashboardMoviesResponse> GetTopMoviesAsync()
        {
            var response = new DashboardMoviesResponse();

            try
            {
                var cachedData = await GetCachedDataAsync(MOVIES_CACHE_KEY);

                if (cachedData != null)
                {
                    var movieData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, GetJsonOptions());

                    response.Movies = movieData?.Movies ?? new List<MediaItem>();
                    response.LastUpdated = cachedData.CachedAt;
                    response.FromCache = true;
                    response.IsSuccess = true;
                    return response;
                }

                var freshData = await FetchMoviesFromApiAsync();
                if (freshData != null)
                {
                    await CacheDataAsync(MOVIES_CACHE_KEY, JsonSerializer.Serialize(freshData, GetJsonOptions()));

                    response.Movies = freshData.Movies;
                    response.LastUpdated = DateTime.UtcNow;
                    response.FromCache = false;
                    response.IsSuccess = true;
                    return response;
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
            var response = new DashboardTvShowsResponse();

            try
            {
                var cachedData = await GetCachedDataAsync(TVSHOWS_CACHE_KEY);

                if (cachedData != null)
                {
                    var tvData = JsonSerializer.Deserialize<MdbListApiResponse>(cachedData.JsonData, GetJsonOptions());

                    response.Shows = tvData?.Shows ?? new List<MediaItem>();
                    response.LastUpdated = cachedData.CachedAt;
                    response.FromCache = true;
                    response.IsSuccess = true;
                    return response;
                }

                var freshData = await FetchTvShowsFromApiAsync();
                if (freshData != null)
                {
                    await CacheDataAsync(TVSHOWS_CACHE_KEY, JsonSerializer.Serialize(freshData, GetJsonOptions()));

                    response.Shows = freshData.Shows;
                    response.LastUpdated = DateTime.UtcNow;
                    response.FromCache = false;
                    response.IsSuccess = true;
                    return response;
                }

                response.IsSuccess = false;
                response.Reason = "Failed to fetch TV show data";
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top TV shows");
                response.IsSuccess = false;
                response.Reason = "Internal server error";
                return response;
            }
        }

        public async Task<bool> RefreshMovieCacheAsync()
        {
            try
            {
                var freshData = await FetchMoviesFromApiAsync();
                if (freshData != null)
                {
                    await CacheDataAsync(MOVIES_CACHE_KEY, JsonSerializer.Serialize(freshData, GetJsonOptions()));
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
            try
            {
                var freshData = await FetchTvShowsFromApiAsync();
                if (freshData != null)
                {
                    await CacheDataAsync(TVSHOWS_CACHE_KEY, JsonSerializer.Serialize(freshData, GetJsonOptions()));
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

        private async Task<CachedData?> GetCachedDataAsync(string cacheKey)
        {
            return await _db.CachedData
                .Where(c => c.CacheKey == cacheKey && c.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        private async Task CacheDataAsync(string cacheKey, string jsonData)
        {
            var existingCache = await _db.CachedData
                .Where(c => c.CacheKey == cacheKey)
                .FirstOrDefaultAsync();

            if (existingCache != null)
            {
                _db.CachedData.Remove(existingCache);
            }

            var cacheEntry = new CachedData
            {
                CacheKey = cacheKey,
                JsonData = jsonData,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(CACHE_DURATION_HOURS)
            };

            _db.CachedData.Add(cacheEntry);
            await _db.SaveChangesAsync();
        }

        private async Task<MdbListApiResponse?> FetchMoviesFromApiAsync()
        {
            var apiUrl = await GetConfigValueAsync("mdblist_movies_endpoint");
            var apiKey = await GetConfigValueAsync("mdblist_api_key");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing MDBList configuration");
                return null;
            }

            var fullUrl = $"{apiUrl}?limit=30&offset=0&append_to_response=genre,poster&apikey={apiKey}";

            var httpResponse = await _httpClient.GetAsync(fullUrl);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch movies from MDBList API. Status: {StatusCode}", httpResponse.StatusCode);
                return null;
            }

            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MdbListApiResponse>(jsonContent, GetJsonOptions());
        }

        private async Task<MdbListApiResponse?> FetchTvShowsFromApiAsync()
        {
            var apiUrl = await GetConfigValueAsync("mdblist_tvshows_endpoint");
            var apiKey = await GetConfigValueAsync("mdblist_api_key");

            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Missing MDBList configuration");
                return null;
            }

            var fullUrl = $"{apiUrl}?limit=30&offset=0&append_to_response=genre,poster&apikey={apiKey}";

            var httpResponse = await _httpClient.GetAsync(fullUrl);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch TV shows from MDBList API. Status: {StatusCode}", httpResponse.StatusCode);
                return null;
            }

            var jsonContent = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<MdbListApiResponse>(jsonContent, GetJsonOptions());
        }

        private async Task<string?> GetConfigValueAsync(string key)
        {
            var config = await _db.Configs.FirstOrDefaultAsync(c => c.Key == key);
            return config?.Value;
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
    }
    public class MdbListApiResponse
    {
        public List<MediaItem> Movies { get; set; } = new List<MediaItem>();
        public List<MediaItem> Shows { get; set; } = new List<MediaItem>();
    }
}