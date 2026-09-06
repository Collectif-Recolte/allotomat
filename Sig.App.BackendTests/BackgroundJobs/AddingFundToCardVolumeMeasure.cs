// Measures how long AddingFundToCard.Run takes, and how much it touches, against a database seeded
// to production scale by VolumeDataSeeder. This is not a correctness test and carries no pass/fail
// threshold: there is no baseline yet to judge a duration against (that is exactly the number this
// file exists to produce), so it prints what it measures and stops there.
//
// Excluded from a normal `dotnet test` run: the [VolumeMeasureFact] attribute below marks the test
// Skip unless VOLUME_MEASURE_CONNECTION_STRING is set, so a filterless run stays fast and green, and
// this class never touches TestBase's in-memory provider.
//
// What must be in place to run it:
//   1. A real SQL Server reachable from this machine (LocalDB, a container, or the same instance
//      already configured for `dotnet run --project Sig.App.Backend`), pointed at an EMPTY database.
//      VolumeDataSeeder refuses to run against a database that already carries VolumeSeed data.
//   2. Seed that database at production scale: from Sig.App.Backend/, with
//      ASPNETCORE_ENVIRONMENT=Development, ConnectionStrings__AppDbContext=<connection string> and
//      VolumeSeed__Scale=1 all set, run `dotnet run --project Sig.App.Backend` once and let it finish
//      seeding (the log line "[VolumeDataSeeder] Seed done ..." marks completion), then stop it.
//   3. Run this test against that same database:
//        VOLUME_MEASURE_CONNECTION_STRING="<connection string>" \
//          dotnet test Sig.App.BackendTests/Sig.App.BackendTests.csproj \
//          --filter "FullyQualifiedName~AddingFundToCardVolumeMeasure" \
//          --logger "console;verbosity=detailed"
//      The last flag is what makes the printed report visible; without it a passing test prints
//      nothing to the console.
//
// This test executes the real job against that database: it creates transactions, funds and an
// AddingFundToCardRun row exactly as a production run would. Repeating it on the same data is not
// the same measurement as the first pass against a fresh seed (allocations get consumed, fund
// balances change), so restore the seeded database, or reseed a fresh one, before a run whose
// numbers you intend to compare against an earlier one.
//
// KNOWN LIMIT OF THIS MEASUREMENT: AddingFundToCard has a second mode, gated per subscription by
// Subscription.IsSubscriptionPaymentBasedCardUsage, that calls LoadCardUsageStats and batches cards
// through CardStatsBatchSize (1000) to count each card's payment history. That is precisely the
// code path the August 1st fix (11122afe) bounded. VolumeDataSeeder never sets that flag on the
// subscriptions it creates, so this measurement never exercises that mode or its batching.
//
// Measured directly in production on 2026-08-30: 734 of 13,906 active participants (5.3%) are on a
// payment-based subscription, carried by 10 of 109 active subscriptions. With that number in hand,
// the decision was to document this gap rather than teach VolumeDataSeeder to produce payment-based
// subscriptions: below 1,000 participants on such a subscription, CardStatsBatchSize's batching
// would only ever run once, so adding it here would sharpen the participant count without actually
// exercising the batching mechanism the August 1st fix was about.
//
// What the 5.3% figure does NOT tell you, and what remains unmeasured: the per-participant cost of
// the payment-based mode. LoadCardUsageStats issues counting queries the default mode never runs,
// so 5.3% of participants is not evidence of 5.3% of this measurement's duration.

using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class AddingFundToCardVolumeMeasure
    {
        private const string ConnectionStringEnvironmentVariable = "VOLUME_MEASURE_CONNECTION_STRING";
        private const string JobName = "VolumeMeasure";

        private readonly ITestOutputHelper output;

        public AddingFundToCardVolumeMeasure(ITestOutputHelper output)
        {
            this.output = output;
        }

        [VolumeMeasureFact]
        public async Task Run_AgainstAVolumeSeededDatabase_ReportsDurationAndRowsTouched()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);

            var counter = new QueryAndMaterializationCounter();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString, sql => sql.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds))
                .AddInterceptors(counter)
                .Options;

            await using var db = new AppDbContext(options);

            var subscriptionBeneficiaryCount = await db.SubscriptionBeneficiaries.CountAsync();
            subscriptionBeneficiaryCount.Should().BeGreaterThan(0,
                $"this measurement needs a database seeded by VolumeDataSeeder (VolumeSeed:Scale set) at the " +
                $"connection string in {ConnectionStringEnvironmentVariable}; that seeder is the only source of " +
                "SubscriptionBeneficiary rows for this job to iterate");

            // Clears a leftover run row from an earlier pass of this same measurement, so the "can't add fund
            // twice in the same day" guard in AddingFundToCard cannot turn this run into a silent no-op with a
            // misleadingly fast duration and a zero participant count.
            var previousRuns = await db.AddingFundToCardRuns.Where(x => x.Name == JobName).ToListAsync();
            db.AddingFundToCardRuns.RemoveRange(previousRuns);
            await db.SaveChangesAsync();

            var job = new AddingFundToCard(db, SystemClock.Instance, NullLogger<AddingFundToCard>.Instance);

            var utcBefore = DateTime.UtcNow;
            var commandsBefore = counter.CommandCount;
            var materializedBefore = counter.MaterializedInstanceCount;

            var stopwatch = Stopwatch.StartNew();
            // FirstAndFifteenthDayOfTheMonth is the one moment AddingFundToCard.Run never gates behind a
            // "is today the 1st/15th/Monday" check (see the three day-of-month guards near its top), and it
            // is the moment VolumeDataSeeder assigns to every subscription it creates, so this call always
            // reaches the participant loop regardless of which day the measurement happens to run on.
            await job.Run(JobName, new[] { SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth });
            stopwatch.Stop();

            var commandsIssued = counter.CommandCount - commandsBefore;
            var rowsMaterialized = counter.MaterializedInstanceCount - materializedBefore;

            var transactionsCreated = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Where(x => x.CreatedAtUtc >= utcBefore)
                .CountAsync();
            var transactionLogsCreated = await db.TransactionLogs
                .Where(x => x.CreatedAtUtc >= utcBefore)
                .CountAsync();
            // Counts a participant once per run even when they received more than one product-group
            // transaction, matching how the job itself iterates Subscription.Beneficiaries once per
            // participant regardless of how many transaction rows that participant ends up producing.
            var participantsServed = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Where(x => x.CreatedAtUtc >= utcBefore)
                .Select(x => x.BeneficiaryId)
                .Distinct()
                .CountAsync();

            output.WriteLine("=== AddingFundToCard volume measure ===");
            output.WriteLine($"Duration: {stopwatch.Elapsed}");
            output.WriteLine($"Participants served: {participantsServed} (of {subscriptionBeneficiaryCount} SubscriptionBeneficiary rows in the dataset)");
            output.WriteLine($"SubscriptionAddingFundTransaction rows created: {transactionsCreated}");
            output.WriteLine($"TransactionLog rows created: {transactionLogsCreated}");
            output.WriteLine($"SQL commands issued: {commandsIssued}");
            output.WriteLine($"Entity instances materialized: {rowsMaterialized}");

            // Consistency check, not a performance threshold: a measurement that served zero participants
            // out of a dataset that has beneficiaries to pay is not a slow run, it is a broken measurement,
            // and must be reported as such rather than as a (misleadingly fast) number.
            participantsServed.Should().BeGreaterThan(0,
                $"the dataset has {subscriptionBeneficiaryCount} SubscriptionBeneficiary rows for this job to " +
                "pay; a zero count here means the measurement broke, not that the job served no one");
        }

        // Counts, across the lifetime of one AppDbContext, every SQL command EF Core sends to the server
        // and every entity instance EF Core materializes from a query result. Registered only on the
        // ad hoc DbContext this file builds for the real SQL Server connection; it never touches the
        // application's own DbContext wiring.
        private sealed class QueryAndMaterializationCounter : DbCommandInterceptor, IMaterializationInterceptor
        {
            private long commandCount;
            private long materializedInstanceCount;

            public long CommandCount => Interlocked.Read(ref commandCount);
            public long MaterializedInstanceCount => Interlocked.Read(ref materializedInstanceCount);

            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            {
                Interlocked.Increment(ref commandCount);
                return base.ReaderExecuting(command, eventData, result);
            }

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref commandCount);
                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
            {
                Interlocked.Increment(ref commandCount);
                return base.NonQueryExecuting(command, eventData, result);
            }

            public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref commandCount);
                return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
            }

            public object CreatedInstance(MaterializationInterceptionData materializationData, object instance)
            {
                Interlocked.Increment(ref materializedInstanceCount);
                return instance;
            }
        }

        // The xUnit-provided way to keep an expensive, environment-dependent test out of a normal
        // `dotnet test` run while still letting a filtered, explicitly-invoked run execute it: Skip is
        // evaluated fresh every time the attribute is constructed (i.e. every test discovery), rather
        // than being a value baked in at compile time, so removing this attribute (or the environment
        // variable check inside it) is what a mutation must undo to prove it was doing anything.
        private sealed class VolumeMeasureFactAttribute : FactAttribute
        {
            public VolumeMeasureFactAttribute()
            {
                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)))
                {
                    Skip = $"Set {ConnectionStringEnvironmentVariable} to a SQL Server connection string pointing " +
                        "at a database seeded by VolumeDataSeeder to run this measurement; see the file header " +
                        "for the full setup.";
                }
            }
        }
    }
}
