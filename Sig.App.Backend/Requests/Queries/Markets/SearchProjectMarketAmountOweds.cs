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
using Microsoft.EntityFrameworkCore;

namespace Sig.App.Backend.Requests.Queries.Markets
{
    public class SearchMarketGroupMarketAmountOweds : IRequestHandler<SearchMarketGroupMarketAmountOweds.Query, MarketAmountOwedPagination<MarketAmountOwedGraphType>>
    {
        private readonly AppDbContext db;

        public SearchMarketGroupMarketAmountOweds(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var marketsInMarketGroup = db.MarketGroups.Include(x => x.Markets).First(x => x.Id == request.MarketGroupId).Markets.Select(x => x.MarketId);

            IQueryable <TransactionLog> query = db.TransactionLogs.Where(x => x.MarketId != null && marketsInMarketGroup.Contains(x.MarketId.Value) && x.CreatedAtUtc >= request.StartDate && x.CreatedAtUtc <= request.EndDate);
            var transactions = query.ToList().GroupBy(x => x.MarketId);

            var markets = db.Markets.Where(x => transactions.Select(x => x.Key).Contains(x.Id)).ToList();

            return await MarketAmountOwedPagination.For(transactions.Select(x => new MarketAmountOwedGraphType { Market = new MarketGraphType(markets.First(y => y.Id == x.Key)), Amount = x.Sum(t => t.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog ? t.TotalAmount : -t.TotalAmount) }), request.Page);
        }

        public class Query : IRequest<MarketAmountOwedPagination<MarketAmountOwedGraphType>>
        {
            public Page Page { get; set; }
            public long MarketGroupId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }
    }
}
