using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public interface IAddUserService
    {
        Task<AddUserResponse> AddUser(string username, string email);
    }
}
