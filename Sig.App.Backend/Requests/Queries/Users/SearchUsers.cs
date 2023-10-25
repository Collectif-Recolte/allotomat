using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;

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

            var sorted = Sort(query, request.Sort?.Field ?? UserSort.Default, request.Sort?.Order ?? SortOrder.Asc);
            return await Pagination.For(sorted, request.Page);
        }

        public class Query : IRequest<Pagination<AppUser>>
        {
            public Page Page { get; set; }
            public Sort<UserSort> Sort { get; set; }
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
