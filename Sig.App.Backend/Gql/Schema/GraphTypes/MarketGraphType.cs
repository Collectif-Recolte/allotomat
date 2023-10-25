using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Requests.Queries.Markets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class MarketGraphType
    {
        private readonly Market market;

        public Id Id => market.GetIdentifier();
        public NonNull<string> Name => market.Name;

        public MarketGraphType(Market market)
        {
            this.market = market;
        }

        public IDataLoaderResult<IEnumerable<ProjectGraphType>> Projects(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarketProjects(Id.LongIdentifierForType<Market>());
        }

        [Description("The list of markets managers for this project.")]
        public async Task<IEnumerable<UserGraphType>> Managers([Inject] IMediator mediator)
        {
            var marketManagers = await mediator.Send(new GetMarketManagers.Query
            {
                MarketId = Id.LongIdentifierForType<Market>()
            });

            return marketManagers.Select(x => new UserGraphType(x));
        }

        [Description("The list of transactions for this market by date.")]
        public async Task<IEnumerable<PaymentTransactionGraphType>> Transactions(DateTime startDate, DateTime endDate, IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadMarketTransactions(Id.LongIdentifierForType<Market>()).GetResultAsync();
            return transactions.Where(x => x.CreatedAt().DayOfYear >= startDate.DayOfYear && x.CreatedAt().Year >= startDate.Year && x.CreatedAt().DayOfYear <= endDate.DayOfYear && x.CreatedAt().Year <= endDate.Year);
        }
    }
}
