using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Sig.App.Backend.DbModel.Enums;
using System.Collections.Generic;
using Sig.App.Backend.Gql.Schema.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.Utilities.Sorting;
using System;

namespace Sig.App.Backend.Requests.Queries.Cards
{
    public class SearchCards : IRequestHandler<SearchCards.Query, Pagination<Card>>
    {
        private readonly AppDbContext db;

        public SearchCards(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Pagination<Card>> Handle(Query request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.Value;
            IQueryable<Card> query = db.Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Organization).Where(x => x.ProjectId == projectId);

            if (request.Status != null)
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            var sorted = Sort(query, CardSort.Default, SortOrder.Asc);
            return await Pagination.For(sorted, request.Page);
        }

        public class Query : IRequest<Pagination<Card>>
        {
            public Page Page { get; set; }
            public Maybe<long> ProjectId { get; set; }
            public IEnumerable<CardStatus> Status { get; set; }
        }

        private static IOrderedQueryable<Card> Sort(IQueryable<Card> query, CardSort sort, SortOrder order)
        {
            switch (sort)
            {
                case CardSort.Default:
                    return query
                        .SortBy(x => x.Id, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum CardSort
        {
            Default
        }
    }
}
