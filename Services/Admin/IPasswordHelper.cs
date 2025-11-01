using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public interface IPasswordHelper
    {
        PasswordResponse Generate();
        bool VerifyPassword(string password, string storedHash, string storedSalt);
        string GenerateRandomPassword();
        byte[] GenerateSalt();
        byte[] HashPassword(string password, byte[] salt, int iterations = 100_000, int keySize = 32);
    }
}
