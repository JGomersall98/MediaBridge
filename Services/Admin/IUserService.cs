using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
using MediaBridge.Models.Admin.GetUser;
using MediaBridge.Models.Admin.ResetPassword;

namespace MediaBridge.Services.Admin
{
    public interface IUserService
    {
        Task<AddUserResponse> AddUser(AddUserRequest newUser);
        GetUserResponse GetUsers();
        StandardResponse ResetPassword(ResetPasswordRequest resetPasswordRequest);
    }
}
