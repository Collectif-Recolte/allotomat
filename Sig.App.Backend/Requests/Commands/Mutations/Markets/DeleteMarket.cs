﻿using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Gql.Interfaces;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sig.App.Backend.Constants;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.DbModel.Entities.Transactions;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class DeleteMarket : AsyncRequestHandler<DeleteMarket.Input>
    {
        private readonly ILogger<DeleteMarket> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DeleteMarket(ILogger<DeleteMarket> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets
                .Include(x => x.Projects)
                .FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null) throw new MarketNotFoundException();

            var marketManagers = await mediator.Send(new GetMarketManagers.Query
            {
                MarketId = marketId
            });

            if (marketManagers != null) {
                foreach (var manager in marketManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, marketId.ToString()));
                }
            }

            var transactions = await db.Transactions.ToListAsync();
            transactions = transactions.Where(x => x.GetType() == typeof(PaymentTransaction) && (x as PaymentTransaction).MarketId == marketId).ToList();

            if (transactions.Count() > 0)
            {
                db.Transactions.RemoveRange(transactions);
            }

            db.ProjectMarkets.RemoveRange(market.Projects);
            
            db.Markets.Remove(market);

            await db.SaveChangesAsync();
            logger.LogInformation($"Market deleted ({marketId}, {market.Name})");
        }
        
        [MutationInput]
        public class Input : IRequest, IHaveMarketId
        {
            public Id MarketId { get; set; }
        }

        public class MarketNotFoundException : RequestValidationException { }
    }
}
