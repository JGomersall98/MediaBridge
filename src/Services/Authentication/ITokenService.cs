using MediaBridge.Database.DB_Models;

namespace MediaBridge.Services.Authentication
{
    public interface ITokenService
    {
        string GenerateToken(User user);
    }
}
