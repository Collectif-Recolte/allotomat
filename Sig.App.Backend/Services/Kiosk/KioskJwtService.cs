using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sig.App.Backend.DbModel;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Kiosk
{
    public class KioskJwtService
    {
        public const string KioskSlugClaim = "kiosk_slug";
        private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(365 * 3);

        private readonly KioskJwtOptions options;
        private readonly JwtSecurityTokenHandler tokenHandler = new();

        public KioskJwtService(IOptions<KioskJwtOptions> options)
        {
            this.options = options.Value;
        }

        public (string AccessToken, DateTime ExpiresAtUtc) IssueToken(string kioskSlug)
        {
            if (string.IsNullOrWhiteSpace(options.SigningKey))
            {
                throw new InvalidOperationException("Kiosk JWT signing key is not configured.");
            }

            var expiresAtUtc = DateTime.UtcNow.Add(TokenLifetime);
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new[] { new Claim(KioskSlugClaim, kioskSlug) },
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return (tokenHandler.WriteToken(token), expiresAtUtc);
        }

        public async Task<ResolvedKioskCashRegister> ResolveFromAuthToken(
            AppDbContext db,
            string authToken,
            CancellationToken cancellationToken)
        {
            var kioskSlug = ValidateAndGetSlug(authToken);
            var resolved = await KioskCashRegisterResolver.Resolve(db, kioskSlug, cancellationToken);

            if (!resolved.TokenFound || !resolved.IsOperational || resolved.MarketIsDisabled)
            {
                throw new KioskAccessInvalidException();
            }

            return resolved;
        }

        private string ValidateAndGetSlug(string authToken)
        {
            if (string.IsNullOrWhiteSpace(authToken))
            {
                throw new KioskAccessInvalidException();
            }

            if (string.IsNullOrWhiteSpace(options.SigningKey))
            {
                throw new InvalidOperationException("Kiosk JWT signing key is not configured.");
            }

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                tokenHandler.ValidateToken(authToken, validationParameters, out var validatedToken);
                var jwt = (JwtSecurityToken)validatedToken;
                var slug = jwt.Claims.FirstOrDefault(x => x.Type == KioskSlugClaim)?.Value;

                if (string.IsNullOrWhiteSpace(slug))
                {
                    throw new KioskAccessInvalidException();
                }

                return slug;
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                throw new KioskAccessInvalidException();
            }
        }
    }
}
