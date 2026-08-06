using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    /// <summary>
    /// Contrôle d'intégrité des réservations d'enveloppe. N'écrit rien, ne fait que rapporter.
    ///
    /// Invariant vérifié, par enveloppe :
    ///
    ///     somme des RemainingAllocatedAmount  &lt;=  OriginalFund - AvailableFund
    ///
    /// <c>OriginalFund - AvailableFund</c> est tout ce qui est sorti de l'enveloppe et n'y est jamais
    /// revenu : le réservé pas encore livré, plus le livré parti sur les cartes. Le réservé seul en est
    /// un sous-ensemble, il ne peut donc pas le dépasser. Un dépassement est une sur-réservation
    /// certaine, qui se paiera en remboursement de trop au prochain retrait.
    ///
    /// EditBudgetAllowance et MoveBudgetAllowance déplacent les deux montants du même delta, donc
    /// l'invariant survit aux modifications d'enveloppe.
    ///
    /// Le contrôle est volontairement unilatéral. Détecter une SOUS-réservation demanderait de modéliser
    /// les expirations de fonds et les cartes désassignées, ce qui serait bien plus fragile pour un
    /// signal moins utile : c'est la sur-réservation qui coûte de l'argent.
    ///
    /// Une paire dont la réservation est encore inconnue (null) est exclue de la somme et comptée à part.
    /// Le dépassement reste alors un minorant : l'absence de dépassement ne prouve rien tant qu'il reste
    /// des inconnues, alors qu'un dépassement constaté est toujours réel.
    /// </summary>
    public class VerifyBudgetAllowanceReservations
    {
        public const string JobName = "VerifyBudgetAllowanceReservations:Never";

        private readonly AppDbContext db;
        private readonly ILogger<VerifyBudgetAllowanceReservations> logger;

        public VerifyBudgetAllowanceReservations(AppDbContext db, ILogger<VerifyBudgetAllowanceReservations> logger)
        {
            this.db = db;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
            };

            // Cron.Never : contrôle lancé à la main, typiquement après un backfill ou avant un
            // déploiement. Il ne modifie rien, donc le relancer est sans conséquence.
            RecurringJob.AddOrUpdate<VerifyBudgetAllowanceReservations>(JobName, x => x.Run(), Cron.Never(), options);
        }

        public async Task<Report> Run()
        {
            logger.LogInformation("VerifyBudgetAllowanceReservations :: start");

            var envelopes = await db.BudgetAllowances
                .Select(x => new
                {
                    x.Id,
                    OrganizationName = x.Organization.Name,
                    SubscriptionName = x.Subscription.Name,
                    x.OriginalFund,
                    x.AvailableFund
                })
                .ToListAsync();

            var pairs = await db.SubscriptionBeneficiaries
                .Where(x => x.BudgetAllowanceId != null)
                .Select(x => new { BudgetAllowanceId = x.BudgetAllowanceId.Value, x.RemainingAllocatedAmount })
                .ToListAsync();

            var pairsByEnvelope = pairs
                .GroupBy(x => x.BudgetAllowanceId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var lines = new List<EnvelopeLine>();

            foreach (var envelope in envelopes)
            {
                if (!pairsByEnvelope.TryGetValue(envelope.Id, out var envelopePairs))
                {
                    envelopePairs = new();
                }

                var reserved = envelopePairs
                    .Where(x => x.RemainingAllocatedAmount.HasValue)
                    .Sum(x => x.RemainingAllocatedAmount.Value);

                var committed = envelope.OriginalFund - envelope.AvailableFund;

                lines.Add(new EnvelopeLine
                {
                    BudgetAllowanceId = envelope.Id,
                    OrganizationName = envelope.OrganizationName,
                    SubscriptionName = envelope.SubscriptionName,
                    OriginalFund = envelope.OriginalFund,
                    AvailableFund = envelope.AvailableFund,
                    Committed = committed,
                    Reserved = reserved,
                    PairCount = envelopePairs.Count,
                    UnknownPairCount = envelopePairs.Count(x => x.RemainingAllocatedAmount == null),
                    NegativePairCount = envelopePairs.Count(x => x.RemainingAllocatedAmount < 0)
                });
            }

            var report = new Report { Envelopes = lines };
            LogReport(report);

            return report;
        }

        private void LogReport(Report report)
        {
            logger.LogInformation(
                $"VerifyBudgetAllowanceReservations :: {report.Envelopes.Count} enveloppe(s) contrôlée(s), " +
                $"{report.OverReservedEnvelopes.Count} en dépassement pour {report.TotalOvershoot} $, " +
                $"{report.NegativeAvailableFundEnvelopes.Count} à découvert pour {report.TotalDeficit} $, " +
                $"{report.UnknownPairCount} réservation(s) inconnue(s), " +
                $"{report.NegativePairCount} réservation(s) négative(s).");

            if (report.UnknownPairCount > 0)
            {
                logger.LogWarning(
                    $"VerifyBudgetAllowanceReservations :: {report.UnknownPairCount} paire(s) sans réservation connue - " +
                    "le contrôle est un minorant tant que BackfillSubscriptionBeneficiaryAllocation n'a pas tourné.");
            }

            if (report.NegativeAvailableFundEnvelopes.Count > 0)
            {
                logger.LogError(
                    $"VerifyBudgetAllowanceReservations :: {report.NegativeAvailableFundEnvelopes.Count} enveloppe(s) à " +
                    $"découvert pour {report.TotalDeficit} $ - il en est sorti plus qu'elles n'en contenaient : " +
                    string.Join(", ", report.NegativeAvailableFundEnvelopes.Select(x => $"{x.BudgetAllowanceId} ({x.AvailableFund})")));
            }

            if (report.NegativeCommittedEnvelopes.Count > 0)
            {
                logger.LogWarning(
                    $"VerifyBudgetAllowanceReservations :: {report.NegativeCommittedEnvelopes.Count} enveloppe(s) dont le " +
                    "disponible dépasse le budget d'origine - créditées de plus que ce qui en est sorti. Sans lien avec " +
                    "les réservations, à investiguer séparément : " +
                    string.Join(", ", report.NegativeCommittedEnvelopes.Select(x => $"{x.BudgetAllowanceId} ({x.Committed})")));
            }

            if (report.OverReservedEnvelopes.Count == 0)
            {
                logger.LogInformation("VerifyBudgetAllowanceReservations :: aucun dépassement.");
                return;
            }

            logger.LogError(
                "VerifyBudgetAllowanceReservations :: enveloppes en dépassement (CSV) - " +
                "BudgetAllowanceId;Organisation;Abonnement;OriginalFund;AvailableFund;Engage;Reserve;Depassement;Paires;Inconnues;Negatives");

            foreach (var line in report.OverReservedEnvelopes)
            {
                logger.LogError(
                    $"VerifyBudgetAllowanceReservations :: {line.BudgetAllowanceId};{Csv(line.OrganizationName)};{Csv(line.SubscriptionName)};" +
                    $"{line.OriginalFund};{line.AvailableFund};{line.Committed};{line.Reserved};{line.Overshoot};" +
                    $"{line.PairCount};{line.UnknownPairCount};{line.NegativePairCount}");
            }
        }

        // Les noms d'organisation et d'abonnement sont saisis par les utilisateurs et peuvent contenir
        // le séparateur, ce qui décalerait les colonnes une fois collé dans un tableur.
        private static string Csv(string value) => value?.Replace(';', ',');

        public class Report
        {
            public IReadOnlyList<EnvelopeLine> Envelopes { get; init; } = new List<EnvelopeLine>();

            /// <summary>
            /// Sur-réservation avérée. Le garde sur <c>Reserved</c> évite de confondre ce cas avec une
            /// enveloppe à <c>Committed</c> négatif et sans aucune paire, où l'écart arithmétique est
            /// positif alors que rien n'est réservé.
            /// </summary>
            public IReadOnlyList<EnvelopeLine> OverReservedEnvelopes =>
                Envelopes.Where(x => x.Reserved > 0 && x.Overshoot > 0).OrderByDescending(x => x.Overshoot).ToList();

            /// <summary>
            /// Enveloppes dont le disponible dépasse le budget d'origine : elles ont été créditées de
            /// plus que ce qui en est sorti. Anomalie distincte de la sur-réservation, et antérieure à
            /// la reconstruction des réservations.
            /// </summary>
            public IReadOnlyList<EnvelopeLine> NegativeCommittedEnvelopes =>
                Envelopes.Where(x => x.Committed < 0).OrderBy(x => x.Committed).ToList();

            /// <summary>
            /// Enveloppes à découvert : il en est sorti plus qu'elles n'en contenaient. C'est le déficit
            /// que CRCL-2606 visait, et il échappe aux deux autres contrôles - un disponible négatif
            /// gonfle <c>Committed</c> au lieu de le rendre négatif.
            /// </summary>
            public IReadOnlyList<EnvelopeLine> NegativeAvailableFundEnvelopes =>
                Envelopes.Where(x => x.AvailableFund < 0).OrderBy(x => x.AvailableFund).ToList();

            public decimal TotalDeficit => NegativeAvailableFundEnvelopes.Sum(x => x.AvailableFund);

            public decimal TotalOvershoot => OverReservedEnvelopes.Sum(x => x.Overshoot);
            public int UnknownPairCount => Envelopes.Sum(x => x.UnknownPairCount);
            public int NegativePairCount => Envelopes.Sum(x => x.NegativePairCount);
        }

        public class EnvelopeLine
        {
            public long BudgetAllowanceId { get; init; }
            public string OrganizationName { get; init; }
            public string SubscriptionName { get; init; }
            public decimal OriginalFund { get; init; }
            public decimal AvailableFund { get; init; }

            /// <summary>Sorti de l'enveloppe et jamais revenu : <c>OriginalFund - AvailableFund</c>.</summary>
            public decimal Committed { get; init; }

            /// <summary>Somme des réservations connues des paires de cette enveloppe.</summary>
            public decimal Reserved { get; init; }

            public int PairCount { get; init; }
            public int UnknownPairCount { get; init; }
            public int NegativePairCount { get; init; }

            /// <summary>Positif = sur-réservation certaine.</summary>
            public decimal Overshoot => Reserved - Committed;
        }
    }
}
