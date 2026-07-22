using System;
using System.Security.Cryptography;
using System.Text;

namespace Sig.App.Backend.Services.Kiosk
{
    public static class KioskHelper
    {
        private const string PasswordChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";

        public static string GenerateAccessToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static string GeneratePassword()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);
            var result = new StringBuilder(8);
            foreach (var b in bytes)
            {
                result.Append(PasswordChars[b % PasswordChars.Length]);
            }
            return result.ToString();
        }

        public static bool PasswordMatches(string stored, string provided)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(provided))
            {
                return false;
            }

            return string.Equals(stored, provided, StringComparison.OrdinalIgnoreCase);
        }
    }
}
