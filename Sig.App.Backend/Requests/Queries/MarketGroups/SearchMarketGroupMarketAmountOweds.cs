using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Requests.Queries.Markets
{
    public class SearchProjectMarketAmountOweds : IRequestHandler<SearchProjectMarketAmountOweds.Query, MarketAmountOwedPagination<MarketAmountOwedGraphType>>
    {
        private readonly AppDbContext db;

        public SearchProjectMarketAmountOweds(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<TransactionLog> query = db.TransactionLogs.Where(x => x.MarketId != null && x.ProjectId == request.ProjectId && x.CreatedAtUtc >= request.StartDate && x.CreatedAtUtc <= request.EndDate);
            var transactions = query.ToList().GroupBy(x => x.MarketId);

            var markets = db.Markets.Where(x => transactions.Select(x => x.Key).Contains(x.Id)).ToList();

            return await MarketAmountOwedPagination.For(transactions.Select(x => {
                var transactionByCashRegister = x.Select(x => x).Where(x => x.CashRegisterId.HasValue).GroupBy(x => x.CashRegisterId.Value);
                var amountByCashRegister = transactionByCashRegister.Select(x =>
                {
                    var cashRegister = db.CashRegisters.FirstOrDefault(y => y.Id == x.Key);

                    return new CashRegisterAmountOwedGraphType()
                    {
                        CashRegister = new CashRegisterGraphType(cashRegister),
                        Amount = x.Sum(t => t.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog ? t.TotalAmount : -t.TotalAmount)
                    };
                });

                return new MarketAmountOwedGraphType { Market = new MarketGraphType(markets.First(y => y.Id == x.Key)), Amount = x.Sum(t => t.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog ? t.TotalAmount : -t.TotalAmount), AmountByCashRegister = amountByCashRegister };
            }), request.Page);
        }

        public class Query : IRequest<MarketAmountOwedPagination<MarketAmountOwedGraphType>>
        {
            public Page Page { get; set; }
            public long ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}
