using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Sig.App.Backend.Extensions
{
    public static class StringExtensions
    {
        public static string ToBase64(this string value)
        {
            var bytes = Encoding.Default.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public static string Base64Decode(this string value)
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.Default.GetString(bytes);
        }

        public static string NormalizeEmailAddress(this string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress)) return null;

            return emailAddress.Trim().ToUpperInvariant();
        }

        public static string Slugify(this string text, int maxLength = 9999)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var str = text.RemoveAccents().ToLower();

            // Remove invalid chars           
            str = Regex.Replace(str, @"[^a-z0-9\s-_]", "");
            
            // Replace underscores and space with hyphens
            str = Regex.Replace(str, @"[\s-_]+", "-").Trim('-'); 

            // Cut and trim 
            str = str.Substring(0, str.Length <= maxLength ? str.Length : maxLength).Trim('-');

            return str;
        }

        public static string RemoveAccents(this string txt)
        {
            if (string.IsNullOrWhiteSpace(txt))
                return string.Empty;

            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
            return Encoding.ASCII.GetString(bytes);
        }
    }
}