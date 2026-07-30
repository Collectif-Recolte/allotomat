using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BackgroundJobs;
using Sig.App.Backend.Helpers;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    // CRCL-2577: the AddingFundToCardRun set for "today" is request-global. Loading it through a DataLoader
    // (which runs in its own DI scope, batched and cached per request) avoids concurrent operations on the
    // shared request-scoped AppDbContext when GraphQL resolves several paymentRemaining fields in parallel.
    public class GetTodaysAddingFundToCardRuns : BatchQuery<GetTodaysAddingFundToCardRuns.Query, int, IReadOnlyList<AddingFundToCardRun>>
    {
        // Request-global value: always loaded under the single constant key below.
        public const int Key = 0;

        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IClock clock;

        public GetTodaysAddingFundToCardRuns(AppDbContext db, IClock clock)
        {
            this.db = db;
            this.clock = clock;
        }

        public override async Task<IDictionary<int, IReadOnlyList<AddingFundToCardRun>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var todaysRuns = await SubscriptionHelper.GetTodaysAddingFundToCardRunsAsync(db, clock, cancellationToken);

            return new Dictionary<int, IReadOnlyList<AddingFundToCardRun>>
            {
                [Key] = todaysRuns
            };
        }
    }
}
