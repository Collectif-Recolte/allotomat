using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using System.Threading.Tasks;
using System.Threading;
using Sig.App.Backend.Gql.Schema.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.Utilities.Sorting;
using System;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Markets;
using GraphQL.Conventions;
using System.Collections.Generic;
using Sig.App.Backend.DbModel.Entities.MarketGroups;

namespace Sig.App.Backend.Requests.Queries.Markets
{
    public class SearchMarkets : IRequestHandler<SearchMarkets.Query, Pagination<Market>>
    {
        private readonly AppDbContext db;

        public SearchMarkets(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Pagination<Market>> Handle(Query request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId;
            IQueryable<Market> query = null;
            if (request.ProjectId.HasValue)
            {
                query = db.ProjectMarkets.Include(x => x.Market).Where(x => request.ProjectId.Value == x.ProjectId).Select(x => x.Market);
            }
            else
            {
                query = db.ProjectMarkets.Include(x => x.Market).Select(x => x.Market);
            }

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    query = query.Where(x => x.Name.ToString().Contains(text));
                }
            }

            if (request.MarketGroups?.Any() ?? false)
            {
                var marketGroupsLongIdentifiers = request.MarketGroups.Select(x => x.LongIdentifierForType<MarketGroup>());
                query = query.Where(x => x.MarketGroups.Select(x => x.MarketGroupId).Any(y => marketGroupsLongIdentifiers.Contains(y)));
            }

            var sorted = Sort(query, request.Sort?.Field ?? MarketSort.Default, SortOrder.Asc);
            return await Pagination.For(sorted, request.Page.Value);
        }

        public class Query : IRequest<Pagination<Market>>
        {
            public Page? Page { get; set; }
            public long? ProjectId { get; set; }
            public Maybe<string> SearchText { get; set; }
            public Sort<MarketSort> Sort { get; set; }
            public IEnumerable<Id> MarketGroups { get; set; }
        }

        private static IOrderedQueryable<Market> Sort(IQueryable<Market> query, MarketSort sort, SortOrder order)
        {
            switch (sort)
            {
                case MarketSort.Default:
                    return query
                        .SortBy(x => x.Id, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public enum MarketSort
        {
            Default
        }
    }
}
