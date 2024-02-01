using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using Sig.App.Backend.DbModel.Enums;
using System.Collections.Generic;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Requests.Queries.Users
{
    public class SearchUsers : IRequestHandler<SearchUsers.Query, Pagination<AppUser>>
    {
        private readonly AppDbContext db;

        public SearchUsers(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Pagination<AppUser>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<AppUser> query = db.Users;

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    query = query.Where(x => x.Email.Contains(text) || x.Profile.FirstName.Contains(text) || x.Profile.LastName.Contains(text));
                }
            }

            if (request.UserTypes != null && request.UserTypes.Count() > 0)
            {
                query = query.Where(x => request.UserTypes.Contains(x.Type));
            }

            var sorted = Sort(query, request.Sort?.Field ?? UserSort.Default, request.Sort?.Order ?? SortOrder.Asc);
            return await Pagination.For(sorted, request.Page);
        }

        public class Query : IRequest<Pagination<AppUser>>
        {
            public Page Page { get; set; }
            public Sort<UserSort> Sort { get; set; }
            public Maybe<string> SearchText { get; set; }
            public IEnumerable<UserType> UserTypes { get; set; }
        }

        private static IOrderedQueryable<AppUser> Sort(IQueryable<AppUser> query, UserSort sort, SortOrder order)
        {
            switch (sort)
            {
                case UserSort.Default:
                    return query
                        .SortBy(x => x.Email, order)
                        .ThenSortBy(x => x.LastAccessTimeUtc, order.Invert());
                case UserSort.LastAccessTimeUtc:
                    return query.SortBy(x => x.LastAccessTimeUtc, order);
                case UserSort.Email:
                    return query.SortBy(x => x.Email, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum UserSort
    {
        Default,
        LastAccessTimeUtc,
        Email
    }
}
