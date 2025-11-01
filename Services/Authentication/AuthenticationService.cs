using MediaBridge.Database;
using MediaBridge.Models.Authentication;

namespace MediaBridge.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly MediaBridgeDbContext _db;
        public AuthenticationService(MediaBridgeDbContext db)
        {
            _db = db;
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {




            return null;
        }
    }
}
