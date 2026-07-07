using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.System;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class CreateMarket : IRequestHandler<CreateMarket.Input, CreateMarket.Payload>
    {
        private readonly ILogger<CreateMarket> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;
        private readonly IMediator mediator;
        private readonly ICurrentUserAccessor currentUserAccessor;

        public CreateMarket(ILogger<CreateMarket> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer, IMediator mediator, ICurrentUserAccessor currentUserAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
            this.mediator = mediator;
            this.currentUserAccessor = currentUserAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateMarket({request.Name}, {request.ManagerEmails})");

            var marketGroup = await ResolveMarketGroupForAssociationAsync(request, cancellationToken);
            var managers = await PrepareManagersAsync(request.ManagerEmails);

            var market = new Market()
            {
                Name = request.Name.Trim()
            };

            db.Markets.Add(market);

            foreach (var (manager, isNew) in managers)
            {
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

                logger.LogInformation($"[Mutation] CreateMarket - Market manager {manager.Email} added to market {market.Name} ({market.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            if (marketGroup != null)
            {
                marketGroup.Markets.Add(new MarketGroupMarket()
                {
                    Market = market,
                    MarketGroup = marketGroup
                });

                market.Projects = new List<ProjectMarket>
                {
                    new ProjectMarket()
                    {
                        Market = market,
                        ProjectId = request.ProjectId.Value.LongIdentifierForType<Project>()
                    }
                };

                await mediator.Send(new CreateCashRegister.Input() { MarketGroupId = request.MarketGroupId.Value, MarketId = market.GetIdentifier(), Name = marketGroup.Name });
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateMarket - New market created {market.Name} ({market.Id})");

            return new Payload
            {
                Market = new MarketGraphType(market),
                Managers = managers.Select(x => new UserGraphType(x.manager))
            };
        }

        private async Task<MarketGroup> ResolveMarketGroupForAssociationAsync(Input request, CancellationToken cancellationToken)
        {
            var hasProject = request.ProjectId.HasValue();
            var hasMarketGroup = request.MarketGroupId.HasValue();

            if (!hasProject && !hasMarketGroup)
            {
                if (!currentUserAccessor.IsUserType(UserType.PCAAdmin))
                {
                    logger.LogWarning("[Mutation] CreateMarket - IncompleteProgramAssociationException");
                    throw new IncompleteProgramAssociationException();
                }

                return null;
            }

            if (hasProject != hasMarketGroup)
            {
                logger.LogWarning("[Mutation] CreateMarket - IncompleteProgramAssociationException");
                throw new IncompleteProgramAssociationException();
            }

            var projectId = request.ProjectId.Value.LongIdentifierForType<Project>();
            var projectExists = await db.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                logger.LogWarning("[Mutation] CreateMarket - ProjectNotFoundException");
                throw new ProjectNotFoundException();
            }

            var marketGroupId = request.MarketGroupId.Value.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);
            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] CreateMarket - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            if (marketGroup.ProjectId != projectId)
            {
                logger.LogWarning("[Mutation] CreateMarket - MarketGroupNotInProjectException");
                throw new MarketGroupNotInProjectException();
            }

            return marketGroup;
        }

        private async Task<List<(AppUser manager, bool isNew)>> PrepareManagersAsync(IEnumerable<string> managerEmails)
        {
            var pending = new List<(string email, AppUser existing)>();

            foreach (var email in managerEmails)
            {
                var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

                if (user != null)
                {
                    if (user.Type != UserType.Merchant)
                    {
                        logger.LogWarning($"[Mutation] CreateMarket - ExistingUserNotMerchantException ({email})");
                        throw new ExistingUserNotMerchantException();
                    }

                    var existingClaims = await userManager.GetClaimsAsync(user);
                    if (existingClaims.Any(c => c.Type == AppClaimTypes.MarketManagerOf))
                    {
                        logger.LogWarning($"[Mutation] CreateMarket - UserAlreadyManagerException ({email})");
                        throw new UserAlreadyManagerException();
                    }
                }

                pending.Add((email, user));
            }

            var managers = new List<(AppUser manager, bool isNew)>();
            foreach (var (email, existing) in pending)
            {
                managers.Add(existing != null
                    ? (existing, false)
                    : (await GetOrCreateMarketManager(email), true));
            }

            return managers;
        }

        private async Task<AppUser> GetOrCreateMarketManager(string email)
        {
            var user = new AppUser(email)
            {
                Type = UserType.Merchant,
                Profile = new UserProfile()
            };

            var result = await userManager.CreateAsync(user);
            result.AssertSuccess();

            logger.LogInformation($"[Mutation] CreateMarket - New market manager created {user.Email} ({user.Id}). Sending email invitation.");

            return user;
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string Name { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
            public Maybe<Id> ProjectId { get; set; }
            public Maybe<Id> MarketGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGraphType Market { get; set; }
            public IEnumerable<UserGraphType> Managers { get; set; }
        }

        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotMerchantException : RequestValidationException { }
        public class ProjectNotFoundException : RequestValidationException { }
        public class MarketGroupNotFoundException : RequestValidationException { }
        public class IncompleteProgramAssociationException : RequestValidationException { }
        public class MarketGroupNotInProjectException : RequestValidationException { }
    }
}
