using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly MediaBridgeDbContext _db;
        public AdminService(MediaBridgeDbContext db)
        {
            _db = db;
        }
        public async Task<ConfigResponse> GetConfigAsync()
        {
            ConfigResponse response = new ConfigResponse();
            List<Config> configs = await _db.Configs.ToListAsync();

            if (configs == null || configs.Count == 0)
            {
                response.IsSuccess = false;
                response.Reason = "No configurations found.";
                return response;
            }

            response.ConfigList = configs.Select(c => new SingleConfig
            {
                Id = c.Id,
                Key = c.Key,
                Value = c.Value,
                Description = c.Description,
                DateCreated = c.Created.ToString("yyyy-MM-dd HH:mm:ss"),
                DateUpdated = c.Updated?.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            response.IsSuccess = true;
            return response;
        }
        public async Task<StandardResponse> UpdateConfigAsync(int id, string value)
        {
            StandardResponse response = new StandardResponse();
            var config = await _db.Configs.FindAsync(id);

            if (config == null)
            {
                response.IsSuccess = false;
                response.Reason = "Configuration not found.";
                return response;
            }
            config.Value = value;
            config.Updated = DateTime.UtcNow;

            _db.Configs.Update(config);
            await _db.SaveChangesAsync();

            response.IsSuccess = true;
            return response;
        }
        public async Task<StandardResponse> AddConfigAsync(string key, string value, string description)
        {
            StandardResponse response = new StandardResponse();
            var existingConfig = await _db.Configs.FirstOrDefaultAsync(c => c.Key == key);
            if (existingConfig != null)
            {
                response.IsSuccess = false;
                response.Reason = "Configuration with the same key already exists.";
                return response;
            }
            Config newConfig = new Config
            {
                Key = key,
                Value = value,
                Description = description,
                Created = DateTime.UtcNow
            };
            await _db.Configs.AddAsync(newConfig);
            await _db.SaveChangesAsync();
            response.IsSuccess = true;
            return response;
        }
    }
}
