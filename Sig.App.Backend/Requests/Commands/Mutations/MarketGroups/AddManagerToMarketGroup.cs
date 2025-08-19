using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
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

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class AddManagerToMarketGroup : IRequestHandler<AddManagerToMarketGroup.Input, AddManagerToMarketGroup.Payload>
    {
        private readonly ILogger<AddManagerToMarketGroup> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public AddManagerToMarketGroup(ILogger<AddManagerToMarketGroup> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddManagerToMarketGroup({request.MarketGroupId}, {request.ManagerEmails})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] AddManagerToMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }
            var managers = new List<AppUser>();

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateMarketGroupManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketGroupManagerOf))
                {
                    logger.LogWarning("[Mutation] AddManagerToMarketGroup - UserAlreadyManagerException");
                    throw new UserAlreadyManagerException();
                }

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.MarketGroupManagerOf, marketGroup.Id.ToString()));

                if (isNew)
                {
                    await mailer.Send(new MarketGroupManagerInviteEmail(manager.Email)
                    {
                        InviteToken = await userManager.GenerateUserTokenAsync(manager, TokenProviders.EmailInvites, TokenPurposes.MarketGroupManagerInvite),
                        MarketGroupName = marketGroup.Name
                    });
                }
                else
                {
                    await mailer.Send(new NewMarketGroupAssignedEmail(manager.Email, marketGroup.GetIdentifier().ToString(), marketGroup.Name));
                }

                managers.Add(manager);
                logger.LogInformation($"[Mutation] AddManagerToMarketGroup - Market group manager {manager.Email} added to market group {marketGroup.Name} ({marketGroup.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload
            {
                Managers = managers.Select(x => new UserGraphType(x))
            };
        }

        private async Task<(AppUser user, bool isNew)> GetOrCreateMarketGroupManager(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null)
            {
                switch (user.Type)
                {
                    case UserType.MarketGroupManager:
                        return (user, false);
                    default:
                        logger.LogWarning("[Mutation] AddManagerToMarketGroup - ExistingUserNotMarketGroupManagerException");
                        throw new ExistingUserNotMarketGroupManagerException();
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.MarketGroupManager,
                    Profile = new UserProfile(),
                    EmailOptIn = new UserEmailOptIn()
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogInformation($"[Mutation] AddManagerToMarketGroup - New market group manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotMarketGroupManagerException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveMarketGroupId, IRequest<Payload>
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