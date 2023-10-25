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
            var cards = await db.Cards.Include(x => x.Beneficiary)
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return cards.ToDictionary(x => x.Id, x => x.Beneficiary != null ? x.Beneficiary is OffPlatformBeneficiary ? new OffPlatformBeneficiaryGraphType(x.Beneficiary as OffPlatformBeneficiary) as IBeneficiaryGraphType : new BeneficiaryGraphType(x.Beneficiary) : null);
        }
    }
}