using MediaBridge.Database;
using MediaBridge.Database.DB_Models;
using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public class AddUserService : IUserService
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

            // Check to see if username or email already exist in db
            // if they exist return false.

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
    }
}
