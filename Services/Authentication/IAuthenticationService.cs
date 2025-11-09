using MediaBridge.Models.Authentication;

namespace MediaBridge.Services.Authentication
{
    public interface IAuthenticationService
    {
        LoginResponse LoginAsync(string username, string password);
        bool VerifyPassword(string password, string salt, string hashedPasssword);
    }
}
