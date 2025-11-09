using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
using MediaBridge.Models.Admin.EditUser;
using MediaBridge.Models.Admin.GetUser;
using MediaBridge.Models.Admin.ResetPassword;

namespace MediaBridge.Services.Admin
{
    public interface IUserService
    {
        Task<AddUserResponse> AddUser(AddUserRequest newUser);
        GetUserListResponse GetUserList();
        StandardResponse ResetPassword(ResetPasswordRequest resetPasswordRequest);
        StandardResponse DeleteUser(int userId);
        GetUserResponse GetUser(int id);
        StandardResponse EditUser(int id, EditUserRequest editUserRequest);
    }
}
