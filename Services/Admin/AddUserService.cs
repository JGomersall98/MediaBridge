using System.Security.Cryptography;
using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Admin;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MediaBridge.Services.Admin
{
    public class AddUserService : IAddUserService
    {
        private readonly MediaBridgeDbContext _db;
        public AddUserService(MediaBridgeDbContext db)
        {
            _db = db;
        }

        public async Task<AddUserResponse> AddUser(string username, string email)
        {
            AddUserResponse response = new AddUserResponse();
            string reason;

            // Username
            bool validUsername = CheckUsername(username, out reason);
            if (!validUsername)
            {
                response.Success = false;
                response.Reason = reason;
                return response;
            }

            // Email
            bool validEmail = CheckEmail(email, out reason);
            if (!validEmail)
            {
                response.Success = false;
                response.Reason = reason;
                return response;
            }

            // Password
            PasswordResponse passwordResponse = PasswordHelper.Generate();

            // DB Entry
            _db.Users.Add(new User
            {
                Username = username,
                Email = email,
                PasswordHash = passwordResponse.Hash,
                Salt = passwordResponse.Salt,
                EmailVerified = false
            });

            await _db.SaveChangesAsync();

            // Temp
            Console.WriteLine("Password is: " + passwordResponse.Password);

            response.Success = true;
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
            if(!email.Contains("@"))
            {
                reason = "No '@' symbol";
                return false;
            }
            if (!email.Contains('.'))
            {
                reason = "No '.' symbol";
                return false;
            }

            string[] emailFirstSplit = email.Split('@');
            string[] emailSecondSplit = emailFirstSplit[1].Split('.');

            string emailUsername = emailFirstSplit[0];
            string emailDomain = emailSecondSplit[1];
            string emailTLD = "";
            if (emailSecondSplit.Length > 2)
            {
                for(int i = 1; i < emailSecondSplit.Length; i++)
                {
                    emailTLD += '.' + emailSecondSplit[i];
                }
            }
            else
            {
                emailTLD = emailSecondSplit[1];
            }
                
            if (string.IsNullOrEmpty(emailUsername) || string.IsNullOrEmpty(emailDomain) || string.IsNullOrEmpty(emailTLD))
            {
                reason = "Invalid email, missing parts";
                return false;
            }
            reason = string.Empty;
            return true;
        }      
    }
}
