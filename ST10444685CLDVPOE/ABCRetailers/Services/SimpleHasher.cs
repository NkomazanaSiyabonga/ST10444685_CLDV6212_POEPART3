using System;
using System.Security.Cryptography;
using System.Text;

namespace ABCRetailers.Services
{
    public interface ISimpleHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class SimpleHasher : ISimpleHasher
    {
        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                // Convert password to bytes
                var bytes = Encoding.UTF8.GetBytes(password);
                // Hash the bytes
                var hash = sha256.ComputeHash(bytes);
                // Convert to base64 string
                return Convert.ToBase64String(hash);
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            // Hash the input password and compare
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }
    }
}