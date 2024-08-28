using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.Transactions;

namespace Sig.App.Backend.Requests.Queries.Transactions
{
    public class SearchTransactions : IRequestHandler<SearchTransactions.Query, TransactionsPagination<Transaction>>
    {
        private readonly AppDbContext db;

        public SearchTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<TransactionsPagination<Transaction>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<Transaction> query = db.Transactions.Where(x => x.CardId == request.CardId);

            var sorted = Sort(query, TransactionSort.Default, SortOrder.Desc);
            return await TransactionsPagination.For(sorted, request.Page);
        }

        public class Query : IRequest<TransactionsPagination<Transaction>>
        {
            public Page Page { get; set; }
            public long CardId { get; set; }
        }

        private static IOrderedQueryable<Transaction> Sort(IQueryable<Transaction> query, TransactionSort sort, SortOrder order)
        {
            switch (sort)
            {
                case TransactionSort.Default:
                    return query.SortBy(x => x.CreatedAtUtc, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }

    public enum TransactionSort
    {
        Default
    }
}
