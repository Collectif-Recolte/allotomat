using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class AuthenticateKiosk : IRequestHandler<AuthenticateKiosk.Input, AuthenticateKiosk.Payload>
    {
        private readonly ILogger<AuthenticateKiosk> logger;
        private readonly AppDbContext db;
        private readonly KioskJwtService kioskJwtService;

        public AuthenticateKiosk(ILogger<AuthenticateKiosk> logger, AppDbContext db, KioskJwtService kioskJwtService)
        {
            this.logger = logger;
            this.db = db;
            this.kioskJwtService = kioskJwtService;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[Mutation] AuthenticateKiosk");

            var resolved = await KioskCashRegisterResolver.Resolve(db, request.Token, cancellationToken);

            if (!resolved.CanBeUsed(out var reason))
            {
                logger.LogWarning("[Mutation] AuthenticateKiosk - KioskAccessInvalidException - {Reason}", reason);
                throw new KioskAccessInvalidException();
            }

            if (!KioskHelper.PasswordMatches(resolved.CashRegister.KioskPassword, request.Password))
            {
                logger.LogWarning("[Mutation] AuthenticateKiosk - KioskAuthenticationFailedException");
                throw new KioskAuthenticationFailedException();
            }

            var (accessToken, expiresAtUtc) = kioskJwtService.IssueToken(resolved.CashRegister.KioskAccessToken);

            return new Payload
            {
                AccessToken = accessToken,
                ExpiresAt = expiresAtUtc.ToString("o")
            };
        }

        public class Input : IRequest<Payload>
        {
            public string Token { get; set; }
            public string Password { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public string AccessToken { get; set; }
            public string ExpiresAt { get; set; }
        }

        public class KioskAuthenticationFailedException : RequestValidationException { }
    }
}
