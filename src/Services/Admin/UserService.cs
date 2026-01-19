using System.Threading.Tasks;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
using MediaBridge.Models.Admin.EditUser;
using MediaBridge.Models.Admin.GetUser;
using MediaBridge.Models.Admin.ResetPassword;
using MediaBridge.Services.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Admin
{
    public class UserService : IUserService
    {
        private readonly MediaBridgeDbContext _db;
        private readonly IAuthenticationService _authenticationService;
        public UserService(MediaBridgeDbContext db, IAuthenticationService authenticationService)
        {
            _db = db;
            _authenticationService = authenticationService;
        }

        public async Task<AddUserResponse> AddUser(AddUserRequest newUser)
        {
            string username = newUser.UserName;
            string email = newUser.Email;

            AddUserResponse response = new AddUserResponse();
            string reason;

            if(_db.Users.Where(u => u.Username.ToLower() == username.ToLower()).Any()
                || _db.Users.Where(u => u.Email.ToLower() == email.ToLower()).Any())
            {
                response.Reason = "Username or email already exists";
                response.IsSuccess = false;
                return response;
            }

            // Username
            bool validUsername = CheckUsername(username, out reason);
            if (!validUsername)
            {
                response.IsSuccess = false;
                response.Reason = reason;
                return response;
            }

            // Email
            bool validEmail = CheckEmail(email, out reason);
            if (!validEmail)
            {
                response.IsSuccess = false;
                response.Reason = reason;
                return response;
            }

            // Password
            PasswordResponse passwordResponse = PasswordHelper.Generate();

            // DB Entry
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordResponse.Hash,
                Salt = passwordResponse.Salt,
                EmailVerified = false,
                IsDeleted = false,
                UserRoles = new List<UserRole>()
            };

            // Add Default User Role
            var userRoleEntity = await _db.Roles
                .FirstOrDefaultAsync(r => r.RoleValue == "User");

            user.UserRoles.Add(new UserRole
            {
                User = user,
                Role = userRoleEntity
            });

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            response.IsSuccess = true;

            // Temp, Ideally this will be sent via ses to the email.
            // Acting as an email verification also.
            response.Password = passwordResponse.Password;

            return response;
        }
        public GetUserListResponse GetUserList()
        {
            GetUserListResponse response = new GetUserListResponse();

            List<User> users = _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToList();

            if(users.Count == 0)
            {
                response.IsSuccess = false;
                response.Reason = "No users";
                return response;
            }

            string highestRole = "";

            List<UserResponse> userResponses = new List<UserResponse>();
            foreach (User user in users)
            {
                highestRole = GetHighestRole(user.UserRoles.ToList()).ToString();
                UserResponse userResponse = new UserResponse
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Role = highestRole,
                    LastLogin = GetLastLogin(user),
                    IsActive = IsUserActive(user)
                };
                userResponses.Add(userResponse);
            }
            response.UserResponse = userResponses;
            response.IsSuccess = true;
            return response;
        }
        public GetUserResponse GetUser(int id)
        {
            GetUserResponse response = new GetUserResponse();

            User user = _db.Users
                .Where(u => u.Id == id)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefault();

            if (user == null)
            {
                response.IsSuccess = false;
                response.Reason = "No user found";
                return response;
            }

            List<GetUserRoles> roleList = new List<GetUserRoles>();
            foreach (var role in user.UserRoles)
            {
                var roleObject = role.Role;
                if (roleObject == null)
                {
                    continue;
                }
                GetUserRoles getUserRoles = new GetUserRoles();
                getUserRoles.Name = roleObject.RoleValue;
                roleList.Add(getUserRoles);
            }

            UserInfo userInfo = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Created = user.Created.ToString(),
                LastLogin = user.LastLogin.ToString(),
                LastPasswordChange = user.LastPasswordChange.ToString(),
                RoleList = roleList,
                IsActive = IsUserActive(user)
            };

            response.UserInfo = userInfo;
            response.IsSuccess = true;
            return response;
        }
        public StandardResponse EditUser(int id, EditUserRequest editUserRequest)
        {
            StandardResponse response = new StandardResponse();

            if (editUserRequest.Username != null)
            {
                User duplicateUsername = _db.Users
                    .Where(u => u.Username.ToLower() == editUserRequest.Username.ToLower())
                    .Where(u => u.Id != id)
                    .FirstOrDefault();

                if (duplicateUsername != null)
                {
                    response.IsSuccess = false;
                    response.Reason = "Username already in use";
                    return response;
                }
            }

            if (editUserRequest.Email != null)
            {
                User duplicateEmail = _db.Users
                    .Where(u => u.Email.ToLower() == editUserRequest.Email.ToLower())
                    .Where(u => u.Id != id)
                    .FirstOrDefault();

                if (duplicateEmail != null)
                {
                    response.IsSuccess = false;
                    response.Reason = "Email already in use";
                    return response;
                }
            }

            User user = _db.Users
                .Where(u => u.Id == id)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefault();

            if (user == null)
            {
                response.IsSuccess = false;
                response.Reason = "No user found";
                return response;
            }

            UpdateUserBasicInfo(user, editUserRequest);

            if (editUserRequest.RoleUpdate != null)
            {
                UpdateUserRoles(user, editUserRequest.RoleUpdate);
            }

            _db.SaveChanges();
            response.IsSuccess = true;
            return response;
        }
        public StandardResponse ResetPassword(ResetPasswordRequest resetPasswordRequest)
        {
            StandardResponse response = new StandardResponse();

            string newPassword = resetPasswordRequest.NewPassword;
            string newPasswordConfirmed = resetPasswordRequest.ConfirmPassword;

            if (newPassword != newPasswordConfirmed)
            {
                response.IsSuccess = false;
                response.Reason = "New passwords do not match";
                return response;
            }

            User user = _db.Users
                .Where(u => u.Id == resetPasswordRequest.UserId)
                .FirstOrDefault();

            if (user == null)
            {
                response.IsSuccess = false;
                response.Reason = "User not found";
                return response;
            }

            bool IsPasswordMatch = _authenticationService.VerifyPassword(resetPasswordRequest.CurrentPassword, user.Salt, user.PasswordHash);

            if (IsPasswordMatch == false)
            {
                response.IsSuccess = false;
                response.Reason = "Password is incorrect";
                return response;
            }

            byte[] salt = PasswordHelper.GenerateSalt();
            string saltBase64 = Convert.ToBase64String(salt);
            string passwordHash = Convert.ToBase64String(PasswordHelper.HashPassword(newPassword, salt));

            user.Salt = saltBase64;
            user.PasswordHash = passwordHash;
            user.LastPasswordChange = DateTime.Now;

            _db.SaveChanges();

            response.IsSuccess = true;

            return response;
        }
        public StandardResponse DeleteUser(int  userId)
        {
            StandardResponse response = new StandardResponse();

            User user = _db.Users
                .Where (u => u.Id == userId)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefault();

            if (user == null)
            {
                response.IsSuccess = false;
                response.Reason = "Cannot find user to delete";
                return response;
            }
            _db.RemoveRange(user.UserRoles);
            _db.Remove(user);
            _db.SaveChanges();

            response.IsSuccess = true;
            return response;
        }
        private bool CheckUsername(string username, out string reason)
        {
            
            if (string.IsNullOrEmpty(username))
            {
                reason = "username is null or empty";
                return false;
            }
            if ((username.Length) < 4 || (username.Length) > 14)
            {
                reason = "username length is less than 4 or more than 14 characters";
                return false;
            }
            reason = string.Empty;
            return true;
        }
        private bool CheckEmail(string email, out string reason)
        {
            if (string.IsNullOrEmpty(email))
            {
                reason = "email is null or empty";
                return false;
            }

            // Ensure exactly one '@' symbol
            string[] emailFirstSplit = email.Split('@');
            if (emailFirstSplit.Length != 2)
            {
                reason = "Email must contain exactly one '@' symbol";
                return false;
            }

            string emailUsername = emailFirstSplit[0];
            string domainPart = emailFirstSplit[1];

            // Ensure at least one '.' in the domain part
            string[] emailSecondSplit = domainPart.Split('.');
            if (emailSecondSplit.Length < 2)
            {
                reason = "Domain part of email must contain a '.'";
                return false;
            }

            string emailDomain = emailSecondSplit[0];
            string emailTLD = string.Join(".", emailSecondSplit.Skip(1));
            if (string.IsNullOrEmpty(emailUsername) || string.IsNullOrEmpty(emailDomain) || string.IsNullOrEmpty(emailTLD))
            {
                reason = "Invalid email, missing parts";
                return false;
            }
            reason = string.Empty;
            return true;
        }      
        private string GetHighestRole(List<UserRole> roles)
        {
            string admin = "Admin";
            string maintainer = "Maintainer";
            string user = "User";

            string highestRole = "";
            string roleValue = string.Empty;

            foreach (UserRole role in roles)
            {
                if (highestRole == admin)
                {
                    break;
                }
                roleValue = role.Role.RoleValue;

                if (highestRole == maintainer)
                {
                    if(roleValue == user)
                    {
                        continue;
                    }
                }
                highestRole = roleValue;
            }
            return highestRole;
        }
        private string GetLastLogin(User user)
        {
            if (user.LastLogin == null)
            {
                return "Never logged in";
            }

            var timeDifference = DateTime.Now - user.LastLogin.Value;
            var totalMinutes = (int)timeDifference.TotalMinutes;
            var totalHours = (int)timeDifference.TotalHours;
            var totalDays = (int)timeDifference.TotalDays;

            if (totalMinutes < 1)
            {
                return "Just now";
            }
            else if (totalMinutes < 60)
            {
                return totalMinutes == 1 ? "1 minute ago" : $"{totalMinutes} minutes ago";
            }
            else if (totalHours < 24)
            {
                return totalHours == 1 ? "1 hour ago" : $"{totalHours} hours ago";
            }
            else if (totalDays < 7)
            {
                return totalDays == 1 ? "1 day ago" : $"{totalDays} days ago";
            }
            else if (totalDays < 30)
            {
                var weeks = totalDays / 7;
                return weeks == 1 ? "1 week ago" : $"{weeks} weeks ago";
            }
            else if (totalDays < 365)
            {
                var months = totalDays / 30;
                return months == 1 ? "1 month ago" : $"{months} months ago";
            }
            else
            {
                var years = totalDays / 365;
                return years == 1 ? "1 year ago" : $"{years} years ago";
            }
        }
        private bool IsUserActive(User user)
        {
            if (user.LastLogin == null)
            {
                return false;
            }

            var daysSinceLastLogin = (DateTime.Now - user.LastLogin.Value).TotalDays;
            return daysSinceLastLogin < 90;
        }
        private void UpdateUserBasicInfo(User user, EditUserRequest editUserRequest)
        {
            if (editUserRequest.Username != null && editUserRequest.Username.ToLower() != user.Username.ToLower())
            {
                user.Username = editUserRequest.Username;
            }

            if (editUserRequest.Email != null && editUserRequest.Email.ToLower() != user.Email.ToLower())
            {
                user.Email = editUserRequest.Email;
            }
        }

        private void UpdateUserRoles(User user, List<RoleUpdate> roleUpdates)
        {
            var currentRoles = ExtractRoleValues(user.UserRoles.ToList());
            var requestedRoles = ExtractRoleValues(roleUpdates);

            var rolesToAdd = GetRolesToAdd(currentRoles, requestedRoles);
            var rolesToRemove = GetRolesToRemove(currentRoles, requestedRoles);

            // Remove roles first - we need to explicitly delete the UserRole entities
            RemoveRolesFromUser(user, rolesToRemove);
            
            // Add new roles
            AddRolesToUser(user, rolesToAdd);
        }

        private void AddRolesToUser(User user, List<string> rolesToAdd)
        {
            foreach (string roleToAdd in rolesToAdd)
            {
                var roleEntity = _db.Roles.FirstOrDefault(r => r.RoleValue == roleToAdd);
                if (roleEntity != null)
                {
                    var newUserRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleEntity.Id,
                        User = user,
                        Role = roleEntity
                    };
                user.UserRoles.Add(newUserRole);
                }
            }
        }

        private void RemoveRolesFromUser(User user, List<string> rolesToRemove)
        {
            // Create a list of UserRole entities to remove
            var userRolesToRemove = new List<UserRole>();
            
            foreach (string roleToRemove in rolesToRemove)
            {
                var userRoleToRemove = user.UserRoles
                .FirstOrDefault(ur => ur.Role != null && ur.Role.RoleValue == roleToRemove);
                if (userRoleToRemove != null)
                {
                    userRolesToRemove.Add(userRoleToRemove);
                }
            }

            // Remove from both the collection and mark for deletion
            foreach (var userRoleToRemove in userRolesToRemove)
            {
                user.UserRoles.Remove(userRoleToRemove);
                _db.User_Roles.Remove(userRoleToRemove);
            }
        }
        private List<string> ExtractRoleValues(List<UserRole> userRoles)
        {
            return userRoles
                .Where(ur => ur.Role?.RoleValue != null)
                .Select(ur => ur.Role.RoleValue)
                .ToList();
        }

        private List<string> ExtractRoleValues(List<RoleUpdate> roleUpdates)
        {
            return roleUpdates
                .Where(ru => ru?.RoleValue != null)
                .Select(ru => ru.RoleValue)
                .ToList();
        }

        private List<string> GetRolesToAdd(List<string> userRoles, List<string> editRoles)
        {
            return editRoles
                .Where(role => !userRoles.Contains(role))
                .ToList();
        }
        private List<string> GetRolesToRemove(List<string> userRoles, List<string> editRoles)
        {
            return userRoles
                .Where(role => !editRoles.Contains(role))
                .ToList();
        }
    }
}
public class PasswordResponse
{
    public string Password { get; set; } = default!;
    public string Salt { get; set; } = default!;
    public string Hash { get; set; } = default!;
}
