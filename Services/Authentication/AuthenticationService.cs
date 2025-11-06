using System.Text;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Admin;
using MediaBridge.Models.Authentication;
using MediaBridge.Services.Admin;
using Microsoft.EntityFrameworkCore;

namespace MediaBridge.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ITokenService _tokenService;
        private readonly MediaBridgeDbContext _db;
        public AuthenticationService(ITokenService tokenService, MediaBridgeDbContext db)
        {
            _tokenService = tokenService;
            _db = db;
        }

        public LoginResponse LoginAsync(string username, string password)
        {
            LoginResponse response = new LoginResponse();

            User? potentialUser = _db.Users
                .Where(u => u.Username.ToLower() == username.ToLower())
                .Where(u => u.IsDeleted == false)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefault();

            if (potentialUser == null)
            {
                response.Reason = "No user found with that username";
                response.IsSuccess = false;
                return response;
            }

            bool IsPasswordMatch = VerifyPassword(password, potentialUser.Salt, potentialUser.PasswordHash);

            response.Token = _tokenService.GenerateToken(potentialUser);
            response.IsSuccess = true;

            return response;
        }

        private bool VerifyPassword(string password, string salt, string hashedPasssword)
        {
            byte[] byteSalt = Convert.FromBase64String(salt);
            string pwd = Convert.ToBase64String(PasswordHelper.HashPassword(password, byteSalt));

            if (hashedPasssword == pwd)
            {
                return true;
            }
            return false;
        }
    }
}
