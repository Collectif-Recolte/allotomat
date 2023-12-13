using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCardStatsByIds : BatchQuery<GetCardStatsByIds.Query, long, CardStatsGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetCardStatsByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, CardStatsGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Projects.Include(x => x.Cards)
                .Where(c => request.Ids.Contains(c.Id))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.Cards);

            return results.ToDictionary(x => x.Key, x =>
            {
                var cardByStatusUnassigned = x.Value.Where(x => x.Status == CardStatus.Unassigned);
                var cardByStatusAssigned = x.Value.Where(x => x.Status == CardStatus.Assigned);

                return new CardStatsGraphType()
                {
                    CardsUnassigned = cardByStatusUnassigned.Count(),
                    CardsAssigned = cardByStatusAssigned.Count()
                };
            });
        }
    }
}
