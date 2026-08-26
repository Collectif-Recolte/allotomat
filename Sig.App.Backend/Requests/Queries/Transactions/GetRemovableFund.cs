using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Helpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Transactions
{
    /// <summary>
    /// Le montant qu'un retrait peut réellement enlever d'une carte pour une combinaison
    /// abonnement / groupe de produits. Ce n'est pas le solde de la carte : le solde d'un groupe
    /// de produits agrège tous les abonnements qui l'ont alimenté, alors qu'un retrait ne peut
    /// piger que dans ce que l'abonnement sélectionné y a versé. (CRCL-2659)
    /// </summary>
    public class GetRemovableFund : IRequestHandler<GetRemovableFund.Query, decimal>
    {
        private readonly AppDbContext db;

        public GetRemovableFund(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<decimal> Handle(Query request, CancellationToken cancellationToken)
        {
            var beneficiaryId = await db.Beneficiaries
                .Where(x => x.CardId == request.CardId)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (beneficiaryId == null) return 0;

            return await TransactionHelper
                .RemovableFundTransactions(db, beneficiaryId.Value, request.ProductGroupId, request.SubscriptionId)
                .SumAsync(x => x.AvailableFund, cancellationToken);
        }

        public class Query : IRequest<decimal>
        {
            public long CardId { get; set; }
            public long SubscriptionId { get; set; }
            public long ProductGroupId { get; set; }
        }
    }
}
