using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using NodaTime.Extensions;
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
        public bool IsDisabled => market.IsDisabled;
        public bool IsArchived => market.IsArchived;

        public MarketGraphType(Market market)
        {
            this.market = market;
        }

        public IDataLoaderResult<IEnumerable<ProjectGraphType>> Projects(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarketProjects(Id.LongIdentifierForType<Market>());
        }

        public IDataLoaderResult<IEnumerable<OrganizationGraphType>> Organizations(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarketOrganizations(Id.LongIdentifierForType<Market>());
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

        [Description("The list of transactions for this market by date and cash-register.")]
        public async Task<IEnumerable<ITransactionGraphType>> Transactions(DateTime startDate, DateTime endDate, Id[] cashRegisters, IAppUserContext ctx)
        {
            if (startDate > endDate)
            {
                return new List<ITransactionGraphType>();
            }

            var transactions = await ctx.DataLoader.LoadMarketTransactions(Id.LongIdentifierForType<Market>()).GetResultAsync();

            var startInstant = startDate.ToInstant();
            var endInstant = endDate.ToInstant();
            return transactions.Where(x => TransactionGraphTypeHelper.IsTransactionBetweenDate(x, startInstant, endInstant) && TransactionGraphTypeHelper.IsTransactionInCashRegister(x, cashRegisters));
        }

        [Description("The list of cash-register for this market.")]
        public async Task<IEnumerable<CashRegisterGraphType>> CashRegisters(IAppUserContext ctx, bool includeArchived = true)
        {
            var cashRegisters = await ctx.DataLoader.LoadMarketCashRegisters(Id.LongIdentifierForType<Market>()).GetResultAsync();

            if (includeArchived)
            {
                return cashRegisters;
            }
            return cashRegisters.Where(x => !x.IsArchived);
        }

    }
}
