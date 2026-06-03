using System;
using System.Security.Cryptography;
using System.Text;

namespace BruteForceApp
{
    /// <summary>
    /// Handles password creation and SHA256 hashing with a static salt.
    /// </summary>
    public class PasswordManager
    {
        // Static constant salt used for all hashing operations
        public const string SALT = "BruteForce_Static_Salt_2024";

        private static readonly string[] CharacterPool = {
            "abcdefghijklmnopqrstuvwxyz",
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
            "0123456789"
        };

        public static readonly string AllCharacters =
            CharacterPool[0] + CharacterPool[1] + CharacterPool[2];

        private readonly Random _random = new Random();

        public string GeneratedPassword { get; private set; } = string.Empty;
        public string HashedPassword { get; private set; } = string.Empty;

        /// <summary>
        /// Generates a random password with length in [4, 6).
        /// </summary>
        public void CreatePassword()
        {
            // Length is randomly chosen between 4 (inclusive) and 6 (exclusive)
            int length = _random.Next(4, 6);
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
                sb.Append(AllCharacters[_random.Next(AllCharacters.Length)]);

            GeneratedPassword = sb.ToString();
            HashedPassword = HashPassword(GeneratedPassword);
        }

        /// <summary>
        /// Hashes a candidate string using SHA256 with the static salt.
        /// </summary>
        public static string HashPassword(string input)
        {
            string salted = SALT + input;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salted));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
