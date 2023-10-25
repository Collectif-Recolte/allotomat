using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Sig.App.Backend.Services.Crypto
{
    public class HmacSignatureService : ISignatureService
    {
        private readonly IClock clock;
        private readonly Random rng = new Random();
        private readonly HashAlgorithm algo;
        
        public HmacSignatureService(IClock clock, IOptions<HmacSignatureOptions> options)
        {
            this.clock = clock;

            var secret = options.Value.Secret;
            algo = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        }

        public string SignUrl(string url, TimeSpan validFor)
        {
            var nonce = rng.Next();
            var until = (clock.GetCurrentInstant() + Duration.FromTimeSpan(validFor)).ToUnixTimeSeconds();

            if (url.IndexOf('?') >= 0)
                url += '&';
            else
                url += '?';

            url += $"_n={nonce}&_u={until}";

            var signature = GetSignature(url);

            url += $"&_s={signature}";

            return url;
        }

        public bool VerifySignedUrl(string url)
        {
            var queryString = url
                .Substring(url.IndexOf('?') + 1)
                .Split('&')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0], x => x.Length > 1 ? x[1] : null);

            if (!queryString.ContainsKey("_n") || !queryString.ContainsKey("_u") || !queryString.ContainsKey("_s")) return false;
            if (!long.TryParse(queryString["_u"], out var untilValue)) return false;

            var until = Instant.FromUnixTimeSeconds(untilValue);
            if (clock.GetCurrentInstant() > until) return false;

            var actualSignature = queryString["_s"];
            
            var urlWithoutSignature = url.Substring(0, url.IndexOf("&_s=", StringComparison.Ordinal));
            var expectedSignature = GetSignature(urlWithoutSignature);

            return expectedSignature == actualSignature;
        }

        private string GetSignature(string value)
        {
            var hash = algo.ComputeHash(Encoding.UTF8.GetBytes(value));
            var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return signature;
        }
    }
}