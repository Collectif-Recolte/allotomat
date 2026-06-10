using System;
using System.Security.Cryptography;

namespace Sig.App.Backend.Helpers
{
    public static class KioskAccessTokenHelper
    {
        public static string Generate()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
