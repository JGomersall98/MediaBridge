using MediaBridge.Database;
using MediaBridge.Services.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Helpers
{
    public interface IGetConfig
    {
        Task<string?> GetConfigValueAsync(string key);
    }
    public class GetConfig : IGetConfig
    {
        private readonly MediaBridgeDbContext _db;

        public GetConfig(MediaBridgeDbContext db)
        {
            _db = db;

        }
        public async Task<string?> GetConfigValueAsync(string key)
        {
            var config = await _db.Configs.FirstOrDefaultAsync(c => c.Key == key);
            return config?.Value;
        }
    }
}
