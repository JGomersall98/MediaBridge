namespace MediaBridge.Database.DB_Models
{
    public class CachedData
    {
        public int Id { get; set; }
        public required string CacheKey { get; set; }
        public required string JsonData { get; set; }
        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
    }
}
