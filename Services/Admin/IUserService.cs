using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public interface IUserService
    {
        Task<AddUserResponse> AddUser(string username, string email);
        GetUserResponse GetUsers();
    }
}
