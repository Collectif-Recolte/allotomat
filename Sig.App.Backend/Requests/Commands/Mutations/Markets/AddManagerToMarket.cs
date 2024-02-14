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
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
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
    public class AddManagerToMarket : IRequestHandler<AddManagerToMarket.Input, AddManagerToMarket.Payload>
    {
        private readonly ILogger<AddManagerToMarket> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public AddManagerToMarket(ILogger<AddManagerToMarket> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddManagerToMarket({request.MarketId}, {request.ManagerEmails})");
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] AddManagerToMarket - MarketNotFoundException");
                throw new MarketNotFoundException();
            }
            var managers = new List<AppUser>();

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateMarketManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketManagerOf))
                {
                    logger.LogWarning("[Mutation] AddManagerToMarket - UserAlreadyManagerException");
                    throw new UserAlreadyManagerException();
                }

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

            return new Payload
            {
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
                        logger.LogWarning("[Mutation] AddManagerToMarket - ExistingUserNotMarketManagerException");
                        throw new ExistingUserNotMarketManagerException();
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

        public class MarketNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotMarketManagerException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveMarketId, IRequest<Payload>
        {
            public IEnumerable<string> ManagerEmails { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IEnumerable<UserGraphType> Managers { get; set; }
        }
    }
}