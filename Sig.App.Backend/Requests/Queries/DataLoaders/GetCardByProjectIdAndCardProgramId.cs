using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCardByProjectIdAndCardProgramId : BatchCollectionQuery<GetCardByProjectIdAndCardProgramId.Query, long, CardGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<long>
        {
            public long Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetCardByProjectIdAndCardProgramId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, CardGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var project = await db.Projects.FirstAsync(x => x.Id == request.Group);

            var types = await db.Cards
                .Where(c => request.Ids.Contains(c.ProgramCardId) && c.ProjectId == project.Id)
                .ToListAsync(cancellationToken);

            return types.ToLookup(x => x.ProgramCardId, x => new CardGraphType(x));
        }
    }
}