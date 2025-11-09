using System.Threading.Tasks;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models;
using MediaBridge.Models.Admin.AddUser;
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
        public GetUserResponse GetUsers()
        {
            GetUserResponse response = new GetUserResponse();

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
                    Role = highestRole
                };
                userResponses.Add(userResponse);
            }
            response.UserResponse = userResponses;
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
    }
}
public class PasswordResponse
{
    public string Password { get; set; } = default!;
    public string Salt { get; set; } = default!;
    public string Hash { get; set; } = default!;
}
