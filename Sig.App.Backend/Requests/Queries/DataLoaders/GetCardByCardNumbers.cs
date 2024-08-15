using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCardByCardNumbers : BatchQuery<GetCardByCardNumbers.Query, string, CardGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetCardByCardNumbers(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, CardGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cards = await db.Cards
                .Where(c => request.Ids.Contains(c.CardNumber.Replace("-", string.Empty)))
                .ToListAsync(cancellationToken);

            return cards.ToDictionary(x => x.CardNumber.Replace("-", string.Empty), x => new CardGraphType(x));
        }
    }
}