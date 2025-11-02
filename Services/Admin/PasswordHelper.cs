using System.Security.Cryptography;
using MediaBridge.Models.Admin;

namespace MediaBridge.Services.Admin
{
    public static class PasswordHelper
    {
        public static PasswordResponse Generate()
        {
            var password = PasswordGenerator();
            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            return new PasswordResponse
            {
                Password = password,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(hash)
            };
        }

        public static string PasswordGenerator()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var stringChars = new char[8];
            var randomBytes = new byte[stringChars.Length];
            RandomNumberGenerator.Fill(randomBytes);

            for (int i = 0; i < stringChars.Length; i++)
            {
                // Use randomBytes[i] to select a character from chars
                stringChars[i] = chars[randomBytes[i] % chars.Length];
            }

            return new String(stringChars);
        }
        public static byte[] GenerateSalt()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
            return salt;
        }
        public static byte[] HashPassword(string password, byte[] salt, int iterations = 100_000, int keySize = 32)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keySize);
        }
    }
}
