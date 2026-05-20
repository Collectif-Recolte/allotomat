using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryByCardIds : BatchQuery<GetBeneficiaryByCardIds.Query, long, IBeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiaryByCardIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, IBeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cards = await db.Cards
                .Include(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return cards.ToDictionary(x => x.Id, x => {
                if (x.Beneficiary == null) return null;
                var isBeneficiariesAnonymous = x.Beneficiary.Organization?.Project?.BeneficiariesAreAnonymous ?? true;
                return x.Beneficiary is OffPlatformBeneficiary opb ? new OffPlatformBeneficiaryGraphType(opb, isBeneficiariesAnonymous) as IBeneficiaryGraphType : new BeneficiaryGraphType(x.Beneficiary, isBeneficiariesAnonymous);
            });
        }
    }
}