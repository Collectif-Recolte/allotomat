using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class CreateMarketGroup : IRequestHandler<CreateMarketGroup.Input, CreateMarketGroup.Payload>
    {
        private readonly ILogger<CreateMarketGroup> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public CreateMarketGroup(ILogger<CreateMarketGroup> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateMarketGroup({request.Name}, {request.ManagerEmails})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null)
            {
                logger.LogWarning("[Mutation] CreateMarketGroup - ProjectNotFoundException");
                throw new ProjectNotFoundException();
            }

            var marketGroup = new MarketGroup()
            {
                Project = project,
                Name = request.Name.Trim()
            };

            var managers = new List<AppUser>();

            db.MarketGroups.Add(marketGroup);

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateMarketGroupManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketGroupManagerOf))
                {
                    logger.LogWarning($"[Mutation] CreateMarketGroup - UserAlreadyManagerException ({email})");
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
                logger.LogInformation($"[Mutation] CreateMarketGroup - Market group manager {manager.Email} added to market group {marketGroup.Name} ({marketGroup.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateMarketGroup - New market group created {marketGroup.Name} ({marketGroup.Id})");

            return new Payload
            {
                MarketGroup = new MarketGroupGraphType(marketGroup),
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
                        logger.LogWarning($"[Mutation] CreateMarketGroup - ExistingUserNotMerchantGroupException ({email})");
                        throw new ExistingUserNotMerchantGroupException();
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

                try {
                    var result = await userManager.CreateAsync(user);
                    result.AssertSuccess();
                }
                catch (Exception error)
                {
                    var test1 = 1;
                }

                logger.LogInformation($"[Mutation] CreateMarketGroup - New market group manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public string Name { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGroupGraphType MarketGroup { get; set; }
            public IEnumerable<UserGraphType> Managers { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotMerchantGroupException : RequestValidationException { }
    }
}
