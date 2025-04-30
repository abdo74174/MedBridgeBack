using System.Security.Cryptography;
using System.Text;

namespace MedBridge.Services
{
    public static class PasswordHasher
    {
        public static (byte[] hashedPassword, byte[] salt) CreatePasswordHash(string password)
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using var hmac = new HMACSHA256(saltBytes);
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return (hashBytes, saltBytes);
        }

        public static bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
        {
            using var hmac = new HMACSHA256(storedSalt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(storedHash);
        }
    }
}