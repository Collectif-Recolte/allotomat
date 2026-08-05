using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    /// <summary>
    /// Reconstruit <see cref="SubscriptionBeneficiary.RemainingAllocatedAmount"/> pour les
    /// lignes créées avant la migration, où il vaut null.
    ///
    /// Déclenché à la main depuis le tableau de bord Hangfire (Cron.Never), jamais automatiquement :
    /// c'est une opération sur de l'argent réel, elle doit être lancée délibérément, en dry run d'abord,
    /// et son rapport par enveloppe est l'artefact à présenter à Récolte.
    ///
    /// Deux populations :
    ///
    ///   1. Grand livre complet — la paire a exactement UN log d'allocation (discriminant 12, introduit
    ///      par CRCL-2577) et aucun mouvement d'enveloppe non journalisé. La valeur est alors exacte :
    ///      alloué - livré - relâché.
    ///
    ///   2. Tout le reste — on écrit le nombre calculé par le code actuel (calendrier restant x montant
    ///      par versement), via les vrais helpers pour que la valeur écrite et celle calculée à l'exécution
    ///      concordent par construction. Ça gèle l'écart historique sans en créer de nouveau : chaque
    ///      livraison future consomme légitimement un des cycles restants. C'est exact pour toute paire
    ///      dont chaque cycle programmé a bien été livré, soit la grande majorité.
    /// </summary>
    public class BackfillSubscriptionBeneficiaryAllocation
    {
        public const string DryRunJobName = "BackfillSubscriptionBeneficiaryAllocation:DryRun:Never";
        public const string ApplyJobName = "BackfillSubscriptionBeneficiaryAllocation:Apply:Never";

        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<BackfillSubscriptionBeneficiaryAllocation> logger;

        public BackfillSubscriptionBeneficiaryAllocation(AppDbContext db, IClock clock, ILogger<BackfillSubscriptionBeneficiaryAllocation> logger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
            };

            // Deux entrées distinctes parce que le tableau de bord Hangfire ne permet pas de passer un
            // argument : sans le job Apply, il n'y aurait aucun moyen d'appliquer le backfill. Les deux
            // sont en Cron.Never, donc rien ne part tout seul.
            RecurringJob.AddOrUpdate<BackfillSubscriptionBeneficiaryAllocation>(DryRunJobName,
                x => x.Run(true), Cron.Never(), options);

            RecurringJob.AddOrUpdate<BackfillSubscriptionBeneficiaryAllocation>(ApplyJobName,
                x => x.Run(false), Cron.Never(), options);
        }

        public async Task Run(bool dryRun = true)
        {
            logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: start (dryRun: {dryRun})");

            var recursionHazards = await db.Subscriptions
                .Where(x => x.IsSubscriptionPaymentBasedCardUsage && x.MaxNumberOfPayments == null)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();

            if (recursionHazards.Count > 0)
            {
                logger.LogError($"BackfillSubscriptionBeneficiaryAllocation :: ABANDON - {recursionHazards.Count} abonnement(s) usage-based sans MaxNumberOfPayments : {string.Join(", ", recursionHazards.Select(x => $"{x.Id} ({x.Name})"))}. Corriger ces données avant de relancer.");
                return;
            }

            var pairs = await db.SubscriptionBeneficiaries
                .Include(x => x.Subscription).ThenInclude(x => x.Types)
                .Where(x => x.RemainingAllocatedAmount == null)
                .ToListAsync();

            if (pairs.Count == 0)
            {
                logger.LogInformation("BackfillSubscriptionBeneficiaryAllocation :: aucune ligne à reconstruire, rien à faire.");
                return;
            }

            logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: {pairs.Count} ligne(s) à reconstruire.");

            var ledger = await LoadLedgerAsync(pairs);

            var ledgerExactCount = 0;
            var calendarEstimateCount = 0;
            var perBudgetAllowanceTotals = new Dictionary<long, decimal>();

            foreach (var pair in pairs)
            {
                var amountPerPayment = pair.Subscription.Types
                    .Where(x => x.BeneficiaryTypeId == pair.BeneficiaryTypeId)
                    .Sum(x => x.Amount);

                var key = (pair.BeneficiaryId, pair.SubscriptionId);
                ledger.TryGetValue(key, out var entry);
                entry ??= new LedgerEntry();

                decimal value;
                string population;

                if (IsLedgerComplete(pair, entry))
                {
                    value = entry.Allocated - entry.Delivered - entry.NoCardReleased;
                    population = "grand livre complet";
                    ledgerExactCount++;
                }
                else
                {
                    var paymentRemaining = await pair.GetPaymentRemainingAsync(db, clock);
                    value = paymentRemaining * amountPerPayment;
                    population = "estimation calendaire";
                    calendarEstimateCount++;
                }

                // La réservation ne peut pas être négative : on n'a jamais réservé moins que rien.
                if (value < 0) value = 0m;

                if (pair.BudgetAllowanceId.HasValue)
                {
                    perBudgetAllowanceTotals.TryGetValue(pair.BudgetAllowanceId.Value, out var runningTotal);
                    perBudgetAllowanceTotals[pair.BudgetAllowanceId.Value] = runningTotal + value;
                }

                logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: bénéficiaire {pair.BeneficiaryId} / abonnement {pair.SubscriptionId} / enveloppe {pair.BudgetAllowanceId} -> {value} ({population})");

                if (!dryRun)
                {
                    pair.RemainingAllocatedAmount = value;
                }
            }

            logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: {ledgerExactCount} exacte(s) via le grand livre, {calendarEstimateCount} par estimation calendaire.");

            foreach (var total in perBudgetAllowanceTotals.OrderBy(x => x.Key))
            {
                logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: enveloppe {total.Key} - total réservé reconstruit {total.Value}");
            }

            if (dryRun)
            {
                logger.LogInformation("BackfillSubscriptionBeneficiaryAllocation :: DRY RUN, aucune écriture. Relancer avec dryRun: false pour appliquer.");
                return;
            }

            await db.SaveChangesAsync();
            logger.LogInformation($"BackfillSubscriptionBeneficiaryAllocation :: {pairs.Count} ligne(s) écrite(s).");
        }

        private static bool IsLedgerComplete(SubscriptionBeneficiary pair, LedgerEntry entry)
        {
            return entry.AllocationLogCount == 1
                && entry.RemovalReleasedCount == 0
                && !entry.HasManualPayment
                && pair.MaxNumberOfPaymentsOverride == null
                && pair.BeneficiaryTypeId != null
                && entry.AllocationBeneficiaryTypeId == pair.BeneficiaryTypeId;
        }

        private async Task<Dictionary<(long BeneficiaryId, long SubscriptionId), LedgerEntry>> LoadLedgerAsync(
            IReadOnlyCollection<SubscriptionBeneficiary> pairs)
        {
            var beneficiaryIds = pairs.Select(x => x.BeneficiaryId).Distinct().ToList();
            var subscriptionIds = pairs.Select(x => x.SubscriptionId).Distinct().ToList();

            var ledger = new Dictionary<(long, long), LedgerEntry>();

            LedgerEntry EntryFor(long beneficiaryId, long subscriptionId)
            {
                var key = (beneficiaryId, subscriptionId);
                if (!ledger.TryGetValue(key, out var entry))
                {
                    entry = new LedgerEntry();
                    ledger[key] = entry;
                }
                return entry;
            }

            var logs = await db.TransactionLogs
                .Where(x => x.BeneficiaryId != null && x.SubscriptionId != null
                    && beneficiaryIds.Contains(x.BeneficiaryId.Value)
                    && subscriptionIds.Contains(x.SubscriptionId.Value))
                .Where(x => x.Discriminator == TransactionLogDiscriminator.AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog
                    || x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog
                    || x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog
                    || x.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog)
                .Select(x => new { x.Discriminator, x.TotalAmount, x.BeneficiaryId, x.SubscriptionId, x.BeneficiaryTypeId, x.InitiatedByProject })
                .ToListAsync();

            foreach (var log in logs)
            {
                var entry = EntryFor(log.BeneficiaryId.Value, log.SubscriptionId.Value);

                switch (log.Discriminator)
                {
                    case TransactionLogDiscriminator.AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog:
                        entry.AllocationLogCount++;
                        entry.Allocated += log.TotalAmount;
                        entry.AllocationBeneficiaryTypeId = log.BeneficiaryTypeId;
                        break;
                    case TransactionLogDiscriminator.RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog:
                        entry.NoCardReleased += log.TotalAmount;
                        break;
                    case TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog:
                        entry.RemovalReleasedCount++;
                        break;
                    case TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog:
                        if (log.InitiatedByProject) entry.HasManualPayment = true;
                        break;
                }
            }

            var delivered = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Include(x => x.SubscriptionType)
                .Where(x => x.BeneficiaryId != null && beneficiaryIds.Contains(x.BeneficiaryId.Value)
                    && subscriptionIds.Contains(x.SubscriptionType.SubscriptionId))
                .Select(x => new { x.BeneficiaryId, x.SubscriptionType.SubscriptionId, x.Amount })
                .ToListAsync();

            foreach (var transaction in delivered)
            {
                EntryFor(transaction.BeneficiaryId.Value, transaction.SubscriptionId).Delivered += transaction.Amount;
            }

            return ledger;
        }

        private class LedgerEntry
        {
            public int AllocationLogCount { get; set; }
            public decimal Allocated { get; set; }
            public long? AllocationBeneficiaryTypeId { get; set; }
            public decimal Delivered { get; set; }
            public decimal NoCardReleased { get; set; }
            public int RemovalReleasedCount { get; set; }
            public bool HasManualPayment { get; set; }
        }
    }
}
