using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Extensions
{
    /// <summary>
    /// CRCL-2677 / CRCL-2669 — Money movements that survive concurrency.
    ///
    /// <para>
    /// Tomat keeps every balance as a read-modify-write on a plain column: envelopes
    /// (<c>budgetAllowance.AvailableFund += amount</c>), card balances (<c>fund.Amount -= amount</c>) and
    /// deposits (<c>addingFundTransaction.AvailableFund -= amount</c>). The read is separated from the
    /// write by the whole handler — or, for the monthly deposit job, by the whole run. Two writers that
    /// overlap read the same value and the second writes a total computed from a stale one: the first
    /// movement disappears, without any error, while its TransactionLog (an INSERT, which never
    /// conflicts) survives. CRCL-2677 measured it on envelopes; CRCL-2669 on cards, where the deposit
    /// job of August 1st 2026 overwrote the purchases made while it held its snapshot.
    /// </para>
    ///
    /// <para>The fix has two halves, and neither is enough on its own:</para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///     <b>Rebase the movement right before writing.</b> What a caller expresses with <c>+= 216</c>
    ///     is not "the balance is 216" but "add 216 to whatever it is". The movement (wanted − read)
    ///     stays valid whatever a concurrent writer did; it only has to be re-applied on the value
    ///     really in the database, re-read at the last moment. This is the change-tracker equivalent
    ///     of an atomic <c>SET Amount += @delta</c>, without leaving the SaveChanges that also writes
    ///     the TransactionLog: both stay one transaction, so a log can no longer outlive a movement.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     <b>Prove nobody moved in between.</b> Each counter is its own concurrency token in
    ///     <see cref="AppDbContext"/>: the UPDATE carries "WHERE Amount = &lt;re-read value&gt;". The
    ///     remaining window between the re-read and the write can no longer lose a movement silently
    ///     — it raises a <see cref="DbUpdateConcurrencyException"/>, rebased and replayed the same way.
    ///     </description>
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <b>Only a debit can be refused.</b> If a concurrent operation consumed the funds a debit was
    /// authorized against, replaying it would overdraw the counter: it is refused
    /// (<see cref="BudgetAllowanceInsufficientFundException"/> for envelopes,
    /// <see cref="CardFundInsufficientException"/> for card balances and deposits). A credit is never
    /// refused, even into a counter already below zero — refusing a refund would block a withdrawal
    /// without ever giving the money back.
    /// </para>
    ///
    /// <para>
    /// <b>What a rebase cannot know.</b> The movement is per counter. A handler that derives one
    /// movement from another counter it read stale — the nightly expiry debiting <c>fund.Amount</c> by
    /// the <c>AvailableFund</c> it is zeroing, a purchase allocating itself to a deposit slice — ends
    /// up with a debit the persisted value cannot cover, and is refused rather than guessed. Such a
    /// caller must re-plan on fresh data (see <c>CreateTransaction</c>) or let Hangfire retry the job.
    /// Loud and retryable beats silent and wrong.
    /// </para>
    ///
    /// <para>
    /// <b>Both envelope amounts are rebased together</b>: <c>MoveBudgetAllowance</c> and
    /// <c>EditBudgetAllowance</c> move <c>OriginalFund</c> by the same delta as <c>AvailableFund</c>.
    /// Rebasing only one would make <c>OriginalFund − AvailableFund</c> — the commitment audited by
    /// <see cref="BackgroundJobs.VerifyBudgetAllowanceReservations"/> — drift silently.
    /// </para>
    /// </summary>
    public static class FundConcurrencyExtensions
    {
        /// <summary>
        /// SaveChanges attempts before giving up. The re-read beforehand already makes a conflict
        /// rare; past a few replays the counter is contested by so many writers that looping would
        /// hide a problem instead of solving it.
        /// </summary>
        public const int MaxAttempts = 5;

        /// <summary>
        /// The counters this joint knows how to rebase: which entity, which properties carry a
        /// movement, which one of them may refuse a debit, how to re-read them, and what to throw.
        /// </summary>
        private static readonly IReadOnlyList<Counter> Counters = new[]
        {
            new Counter(
                typeof(BudgetAllowance),
                new[] { nameof(BudgetAllowance.AvailableFund), nameof(BudgetAllowance.OriginalFund) },
                nameof(BudgetAllowance.AvailableFund),
                async (db, ids, cancellationToken) => (await db.BudgetAllowances.AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new { x.Id, x.AvailableFund, x.OriginalFund })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(x => x.Id, x => new[] { x.AvailableFund, x.OriginalFund }),
                refusal => new BudgetAllowanceInsufficientFundException(
                    $"Le mouvement d'enveloppe ne peut pas être appliqué : le solde en base " +
                    $"({refusal.Persisted}) ne couvre plus le débit de {-refusal.Movement} " +
                    $"autorisé sur la valeur lue ({refusal.Read}).",
                    refusal.Conflict)),

            new Counter(
                typeof(Fund),
                new[] { nameof(Fund.Amount) },
                nameof(Fund.Amount),
                async (db, ids, cancellationToken) => (await db.Funds.AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new { x.Id, x.Amount })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(x => x.Id, x => new[] { x.Amount }),
                refusal => new CardFundInsufficientException(
                    $"Le mouvement sur le solde de la carte ne peut pas être appliqué : le solde en base " +
                    $"({refusal.Persisted}) ne couvre plus le débit de {-refusal.Movement} " +
                    $"autorisé sur la valeur lue ({refusal.Read}).",
                    refusal.Conflict)),

            new Counter(
                typeof(AddingFundTransaction),
                new[] { nameof(AddingFundTransaction.AvailableFund) },
                nameof(AddingFundTransaction.AvailableFund),
                async (db, ids, cancellationToken) => (await db.Transactions.OfType<AddingFundTransaction>().AsNoTracking()
                        .Where(x => ids.Contains(x.Id))
                        .Select(x => new { x.Id, x.AvailableFund })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(x => x.Id, x => new[] { x.AvailableFund }),
                refusal => new CardFundInsufficientException(
                    $"Le mouvement sur le versement ne peut pas être appliqué : le disponible en base " +
                    $"({refusal.Persisted}) ne couvre plus le débit de {-refusal.Movement} " +
                    $"autorisé sur la valeur lue ({refusal.Read}).",
                    refusal.Conflict))
        };

        /// <summary>
        /// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> for any operation that moves
        /// money — envelope, card balance or deposit: rebases the movements on the persisted amounts
        /// right before writing, then replays residual concurrency conflicts. A conflict on anything
        /// else is rethrown untouched.
        /// </summary>
        public static async Task<int> SaveChangesWithFundRetryAsync(
            this AppDbContext db, CancellationToken cancellationToken = default)
        {
            await RebaseOnPersistedAmountsAsync(db, cancellationToken);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    if (attempt >= MaxAttempts) throw;
                    if (!await TryRebaseConflictsAsync(exception, cancellationToken)) throw;
                }
            }
        }

        /// <summary>
        /// Re-reads the persisted amounts of every counter about to be written and re-applies the
        /// wanted movements on them. The read is deliberately <c>AsNoTracking</c> and projected: a
        /// tracked query would hand back the instance already in memory — the very stale values this
        /// exists to correct.
        /// </summary>
        private static async Task RebaseOnPersistedAmountsAsync(AppDbContext db, CancellationToken cancellationToken)
        {
            var plan = new List<PlannedRebase>();

            foreach (var counter in Counters)
            {
                var entries = db.ChangeTracker.Entries()
                    .Where(x => x.State == EntityState.Modified && counter.Owns(x.Entity))
                    .Where(x => counter.Properties.Any(property => x.Property(property).IsModified))
                    .ToList();

                if (entries.Count == 0) continue;

                var persisted = await counter.LoadPersisted(db, entries.Select(IdOf).ToList(), cancellationToken);

                foreach (var entry in entries)
                {
                    if (!persisted.TryGetValue(IdOf(entry), out var amounts)) continue;
                    PlanRebase(entry, counter, amounts, plan);
                }
            }

            Apply(plan);
        }

        /// <summary>
        /// Rebases each conflicting counter on its persisted amounts. Returns <c>false</c> as soon as
        /// a conflict is not rebasable, so the caller rethrows the original exception instead of
        /// inventing a result.
        /// </summary>
        private static async Task<bool> TryRebaseConflictsAsync(
            DbUpdateConcurrencyException exception, CancellationToken cancellationToken)
        {
            if (exception.Entries.Count == 0) return false;

            var plan = new List<PlannedRebase>();

            foreach (var entry in exception.Entries)
            {
                // A conflict on another entity, or on a row deleted in the meantime, has no movement
                // to re-apply: it cannot be resolved without risking overwriting something else.
                var counter = Counters.FirstOrDefault(x => x.Owns(entry.Entity));
                if (counter == null) return false;
                if (entry.State != EntityState.Modified) return false;

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null) return false;

                var persisted = counter.Properties.Select(property => (decimal)databaseValues[property]).ToArray();

                PlanRebase(entry, counter, persisted, plan, exception);
            }

            Apply(plan);
            return true;
        }

        /// <summary>
        /// Computes — without modifying anything — the rebased values of one counter, and refuses
        /// here, before any mutation, a debit the persisted amount no longer covers. Planning first and
        /// applying afterwards is what makes the rebase all-or-nothing: a refusal on the second
        /// counter of a batch cannot leave the first one half rebased.
        /// </summary>
        private static void PlanRebase(
            EntityEntry entry, Counter counter, decimal[] persisted, List<PlannedRebase> plan,
            DbUpdateConcurrencyException conflict = null)
        {
            var planned = new List<PlannedRebase>();

            for (var i = 0; i < counter.Properties.Length; i++)
            {
                var property = entry.Property(counter.Properties[i]);
                var read = (decimal)property.OriginalValue;
                var movement = (decimal)property.CurrentValue - read;
                var rebased = persisted[i] + movement;

                // A credit always lands, even into a counter already below zero: see the class note.
                // Only a debit the persisted amount no longer covers is refused.
                if (counter.Properties[i] == counter.RefusableProperty && movement < 0m && rebased < 0m)
                {
                    throw counter.Refuse(new Refusal(read, persisted[i], movement, conflict));
                }

                planned.Add(new PlannedRebase(property, persisted[i], rebased));
            }

            plan.AddRange(planned);
        }

        private static void Apply(List<PlannedRebase> plan)
        {
            foreach (var rebase in plan)
            {
                // The read value becomes the persisted one: the "WHERE Amount = ..." of the SaveChanges
                // targets the real state, and the movement is re-applied on top.
                rebase.Property.OriginalValue = rebase.Persisted;
                rebase.Property.CurrentValue = rebase.Rebased;
            }
        }

        private static long IdOf(EntityEntry entry) => (long)entry.Property("Id").CurrentValue;

        private sealed class Counter
        {
            public Counter(
                Type entityType, string[] properties, string refusableProperty,
                Func<AppDbContext, List<long>, CancellationToken, Task<Dictionary<long, decimal[]>>> loadPersisted,
                Func<Refusal, Exception> refuse)
            {
                EntityType = entityType;
                Properties = properties;
                RefusableProperty = refusableProperty;
                LoadPersisted = loadPersisted;
                Refuse = refuse;
            }

            public Type EntityType { get; }
            public string[] Properties { get; }
            public string RefusableProperty { get; }
            public Func<AppDbContext, List<long>, CancellationToken, Task<Dictionary<long, decimal[]>>> LoadPersisted { get; }
            public Func<Refusal, Exception> Refuse { get; }

            // IsInstanceOfType, not an equality on the type: the deposits are a TPH hierarchy and a
            // LoyaltyAddingFundTransaction carries the same AvailableFund as its base.
            public bool Owns(object entity) => EntityType.IsInstanceOfType(entity);
        }

        private readonly record struct Refusal(decimal Read, decimal Persisted, decimal Movement, DbUpdateConcurrencyException Conflict);

        private readonly record struct PlannedRebase(PropertyEntry Property, decimal Persisted, decimal Rebased);
    }

    /// <summary>
    /// An envelope debit authorized against a balance a concurrent operation has since consumed.
    /// Refusing is the only safe behaviour: applying the movement anyway would overdraw the envelope,
    /// which no domain guard allows. A credit never takes this path.
    /// </summary>
    public class BudgetAllowanceInsufficientFundException : RequestValidationException
    {
        public BudgetAllowanceInsufficientFundException(string message, Exception innerException = null)
            : base(message)
        {
            ConcurrencyConflict = innerException;
        }

        /// <summary>
        /// The concurrency conflict behind the refusal, when there is one — the race itself is the
        /// useful diagnostic, and <see cref="RequestValidationException"/> exposes no inner exception.
        /// </summary>
        public Exception ConcurrencyConflict { get; }
    }

    /// <summary>
    /// A card balance or deposit debit authorized against an amount a concurrent operation has since
    /// consumed. Same rule as for envelopes: refuse rather than overdraw. <c>CreateTransaction</c>
    /// catches it to re-plan the purchase on fresh data; a job lets Hangfire retry it.
    /// </summary>
    public class CardFundInsufficientException : RequestValidationException
    {
        public CardFundInsufficientException(string message, Exception innerException = null)
            : base(message)
        {
            ConcurrencyConflict = innerException;
        }

        public Exception ConcurrencyConflict { get; }
    }
}
