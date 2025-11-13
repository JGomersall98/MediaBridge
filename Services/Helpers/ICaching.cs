using MediaBridge.Database.DB_Models;

namespace MediaBridge.Services.Helpers
{
    public interface ICaching
    {
        Task<CachedData?> GetCachedDataAsync(string cacheKey);
        Task CacheDataAsync(string cacheKey, string jsonData, int hours);
    }
}
