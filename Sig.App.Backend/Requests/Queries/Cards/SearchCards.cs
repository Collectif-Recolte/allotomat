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
using Sig.App.Backend.Extensions;

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
            var projectId = request.ProjectId;
            IQueryable<Card> query = db.Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Organization).Where(x => x.ProjectId == projectId);

            if (request.Status != null && request.Status.Count() > 0)
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    query = query.Where(x => x.ProgramCardId.ToString().Contains(text) || x.CardNumber.Contains(text));
                }
            }

            if (request.WithCardDisabled.IsSet())
            {
                query = query.Where(x => x.IsDisabled == request.WithCardDisabled.Value);
            }

            var sorted = Sort(query, request.Sort?.Field ?? CardSort.Default, SortOrder.Asc);
            return await Pagination.For(sorted, request.Page);
        }

        public class Query : IRequest<Pagination<Card>>
        {
            public Page Page { get; set; }
            public long ProjectId { get; set; }
            public IEnumerable<CardStatus> Status { get; set; }
            public Maybe<string> SearchText { get; set; }
            public Maybe<bool> WithCardDisabled { get; set; }
            public Sort<CardSort> Sort { get; set; }
        }

        private static IOrderedQueryable<Card> Sort(IQueryable<Card> query, CardSort sort, SortOrder order)
        {
            switch (sort)
            {
                case CardSort.ID:
                case CardSort.Default:
                    return query
                        .SortBy(x => x.Id, order);
                case CardSort.Funds:
                    return query
                        .SortBy(x => x.Funds.Sum(x => x.Amount), SortOrder.Desc);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum CardSort
        {
            Default,
            ID,
            Funds
        }
    }
}
