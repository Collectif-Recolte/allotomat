using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Requests.Queries.MarketGroups;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Sig.App.Backend.Requests.Queries.Markets.SearchMarkets;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class MarketGroupGraphType
    {
        private readonly MarketGroup marketGroup;

        public Id Id => marketGroup.GetIdentifier();
        public NonNull<string> Name => marketGroup.Name;
        public bool IsArchived => marketGroup.IsArchived;

        public MarketGroupGraphType(MarketGroup marketGroup)
        {
            this.marketGroup = marketGroup;
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(marketGroup.ProjectId);
        }

        public IDataLoaderResult<IEnumerable<MarketGraphType>> Markets(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadGroupMarketMarkets(Id.LongIdentifierForType<MarketGroup>());
        }

        public IDataLoaderResult<IEnumerable<CashRegisterGraphType>> CashRegisters(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarketGroupCashRegisters(Id.LongIdentifierForType<MarketGroup>());
        }

        [Description("The list of market groups managers for this project.")]
        public async Task<IEnumerable<UserGraphType>> Managers([Inject] IMediator mediator)
        {
            var marketManagers = await mediator.Send(new GetMarketGroupManagers.Query
            {
                MarketGroupId = Id.LongIdentifierForType<MarketGroup>()
            });

            return marketManagers.Select(x => new UserGraphType(x));
        }

        public async Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> MarketsAmountOwed([Inject] IMediator mediator, int page, int limit, DateTime startDate, DateTime endDate)
        {
            var results = await mediator.Send(new SearchMarketGroupMarketAmountOweds.Query
            {
                MarketGroupId = marketGroup.Id,
                Page = new Page(page, limit),
                StartDate = startDate,
                EndDate = endDate
            });

            return results;
        }

        public async Task<Pagination<MarketGraphType>> MarketsSearch([Inject] IMediator mediator, int page, int limit,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            [Description("If specified, only that match text is returned.")] string? searchText = "",
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Sort<MarketSort> sort = null
            )
        {
            var results = await mediator.Send(new SearchMarkets.Query
            {
                MarketGroupId = marketGroup.Id,
                Page = new Page(page, limit),
                SearchText = searchText,
                Sort = sort
            });

            return results.Map(x =>
            {
                return new MarketGraphType(x);
            });
        }
    }
}
