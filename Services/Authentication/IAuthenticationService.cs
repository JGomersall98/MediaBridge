using MediaBridge.Models.Authentication;

namespace MediaBridge.Services.Authentication
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> LoginAsync(string username, string password);
    }
}
