using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class CreateMarket : IRequestHandler<CreateMarket.Input, CreateMarket.Payload>
    {
        private readonly ILogger<CreateMarket> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public CreateMarket(ILogger<CreateMarket> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var market = new Market()
            {
                Name = request.Name
            };

            if (request.RefundTransactionPassword.IsSet())
            {
                market.SetRefundTransactionPassword(request.RefundTransactionPassword.Value.Trim());
            }

            var managers = new List<AppUser>();

            db.Markets.Add(market);

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateMarketManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketManagerOf))
                    throw new UserAlreadyManagerException();

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, market.Id.ToString()));

                if (isNew)
                {
                    await mailer.Send(new MarketManagerInviteEmail(manager.Email)
                    {
                        InviteToken = await userManager.GenerateUserTokenAsync(manager, TokenProviders.EmailInvites, TokenPurposes.MerchantInvite),
                        MarketName = market.Name
                    });
                }
                else
                {
                    await mailer.Send(new NewMarketAssignedEmail(manager.Email, market.GetIdentifier().ToString(), market.Name));
                }

                managers.Add(manager);
                logger.LogInformation($"Market manager {manager.Email} added to market {market.Name} ({market.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"New market created {market.Name} ({market.Id})");

            return new Payload
            {
                Market = new MarketGraphType(market),
                Managers = managers.Select(x => new UserGraphType(x))
            };
        }

        private async Task<(AppUser user, bool isNew)> GetOrCreateMarketManager(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null)
            {
                switch (user.Type)
                {
                    case UserType.Merchant:
                        return (user, false);
                    default:
                        throw new ExistingUserNotMerchantException();
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.Merchant,
                    Profile = new UserProfile()
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogDebug($"New market manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string Name { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
            public Maybe<NonNull<string>> RefundTransactionPassword { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGraphType Market { get; set; }
            public IEnumerable<UserGraphType> Managers { get; set; }
        }

        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotMerchantException : RequestValidationException { }
    }
}
