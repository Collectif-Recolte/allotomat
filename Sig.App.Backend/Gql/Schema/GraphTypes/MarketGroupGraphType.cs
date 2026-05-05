using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using NodaTime.Extensions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
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
            [Description("If specified, only that match text is returned.")] string searchText = "",
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

        [Description("The list of transactions for this market-group by date and cash-register.")]
        public async Task<IEnumerable<ITransactionGraphType>> Transactions(DateTime startDate, DateTime endDate, Id[] cashRegisters, IAppUserContext ctx)
        {
            if (startDate > endDate)
            {
                return new List<ITransactionGraphType>();
            }

            var transactions = await ctx.DataLoader.LoadMarketGroupTransactions(Id.LongIdentifierForType<MarketGroup>()).GetResultAsync();

            var startInstant = startDate.ToInstant();
            var endInstant = endDate.ToInstant();

            return transactions.Where(x => IsTransactionBetweenDate(x, startInstant, endInstant) && IsTransactionInCashRegister(x, cashRegisters));
        }

        private bool IsTransactionBetweenDate(ITransactionGraphType x, NodaTime.Instant startInstant, NodaTime.Instant endInstant)
        {
            var createdAtInstant = x.CreatedAt().ToInstant();

            return startInstant <= createdAtInstant && createdAtInstant < endInstant;
        }

        private bool IsTransactionInCashRegister(ITransactionGraphType x, Id[] cashRegisters)
        {
            if (cashRegisters.Length > 0)
            {
                var cashRegisterIds = cashRegisters.Select(x => x.LongIdentifierForType<CashRegister>());
                if (x is RefundTransactionGraphType rtgt)
                {
                    if (cashRegisterIds.Any(id => id == rtgt.CashRegisterId))
                    {
                        return true;
                    }
                }
                if (x is PaymentTransactionGraphType ptgt)
                {
                    if (cashRegisterIds.Any(id => id == ptgt.CashRegisterId))
                    {
                        return true;
                    }
                }
                return false;
            }
            return true;
        }
    }
}
