using MediaBridge.Models;
using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public interface IAdminService
    {
        Task<ConfigResponse> GetConfigAsync();
        Task<StandardResponse> UpdateConfigAsync(int id, string value);
        Task<StandardResponse> AddConfigAsync(string key, string value, string description);
    }
}
