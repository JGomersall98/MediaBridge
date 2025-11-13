using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Helpers
{
    public class Caching : ICaching
    {
        private readonly MediaBridgeDbContext _db;
        public Caching(MediaBridgeDbContext db)
        {
            _db = db;
        }

        public async Task<CachedData?> GetCachedDataAsync(string cacheKey)
        {
            return await _db.CachedData
                .Where(c => c.CacheKey == cacheKey && c.ExpiresAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task CacheDataAsync(string cacheKey, string jsonData, int hours)
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
                ExpiresAt = DateTime.UtcNow.AddHours(hours)
            };

            _db.CachedData.Add(cacheEntry);
            await _db.SaveChangesAsync();
        }
    }
}
