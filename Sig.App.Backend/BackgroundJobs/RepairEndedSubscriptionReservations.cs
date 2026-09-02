using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    /// <summary>
    /// Répare les réservations d'enveloppe orphelines : de l'argent sorti de l'enveloppe pour une paire
    /// bénéficiaire/abonnement, jamais livré sur la carte et jamais relâché, sur un abonnement qui est
    /// maintenant terminé. Plus aucune exécution d'<see cref="AddingFundToCard"/> ne les touchera : sans
    /// intervention, l'argent reste immobilisé indéfiniment (CRCL-2676).
    ///
    /// Population visée, strictement :
    ///
    ///   * <c>RemainingAllocatedAmount &gt; 0</c> — une réservation connue et non nulle. Les paires à
    ///     null sont hors sujet : leur montant réservé est inconnu, seul
    ///     <see cref="BackfillSubscriptionBeneficiaryAllocation"/> peut le résoudre. Elles sont comptées
    ///     dans le rapport pour signaler que la réparation reste partielle tant qu'il n'a pas tourné.
    ///   * <c>Subscription.EndDate &lt; aujourd'hui</c> — l'abonnement est terminé. Sur un abonnement
    ///     encore actif, une réservation en attente est normale : le prochain versement la consommera.
    ///   * <c>BudgetAllowanceId != null</c> — l'argent vient bien d'une enveloppe.
    ///
    /// Deux modes, parce que la décision appartient à Récolte/BCAFM et pas au code :
    ///
    ///   * <see cref="RepairMode.Deliver"/> — verser en retard ce qui était dû. La livraison passe par
    ///     <see cref="AddingFundToCard.AddFundToExistingSubscriptionBeneficiary"/>, donc par le même code
    ///     que le job régulier : transaction, journal, fonds de carte et consommation de la réservation
    ///     restent cohérents par construction. L'enveloppe n'est PAS redébitée — l'argent en est déjà
    ///     sorti, c'est tout le sujet.
    ///   * <see cref="RepairMode.Release"/> — remettre l'argent dans l'enveloppe : <c>AvailableFund</c>
    ///     remonte du montant réservé, la réservation tombe à zéro, et le mouvement est journalisé.
    ///
    /// Les deux modes sont en dry run par défaut. Le dry run n'écrit rien du tout, pas même en mémoire :
    /// il calcule la décision par paire et produit le rapport par enveloppe à présenter avant d'appliquer.
    ///
    /// L'application est volontairement tout-ou-rien : un seul <c>SaveChanges</c> à la fin (par
    /// <see cref="BudgetAllowanceConcurrencyExtensions.SaveChangesWithBudgetAllowanceRetryAsync"/>,
    /// puisque les deux modes créditent des enveloppes), aucune sauvegarde intermédiaire, et rien
    /// n'intercepte les exceptions. Une paire qui explose laisse donc la base exactement dans son état
    /// d'origine, ce qui est la bonne propriété quand on déplace de l'argent : l'alternative, une
    /// réparation à moitié appliquée, se raconte mal et s'audite encore plus mal. Le job est idempotent -
    /// la population est définie par <c>RemainingAllocatedAmount &gt; 0</c>, qu'une réparation réussie
    /// remet à zéro - donc le relancer après avoir corrigé la donnée fautive reprend simplement ce qui
    /// reste.
    /// </summary>
    public class RepairEndedSubscriptionReservations
    {
        public const string DeliverDryRunJobName = "RepairEndedSubscriptionReservations:Deliver:DryRun:Never";
        public const string DeliverApplyJobName = "RepairEndedSubscriptionReservations:Deliver:Apply:Never";
        public const string ReleaseDryRunJobName = "RepairEndedSubscriptionReservations:Release:DryRun:Never";
        public const string ReleaseApplyJobName = "RepairEndedSubscriptionReservations:Release:Apply:Never";

        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<RepairEndedSubscriptionReservations> logger;
        private readonly ILogger<AddingFundToCard> addingFundLogger;

        public RepairEndedSubscriptionReservations(
            AppDbContext db,
            IClock clock,
            ILogger<RepairEndedSubscriptionReservations> logger,
            ILogger<AddingFundToCard> addingFundLogger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.addingFundLogger = addingFundLogger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var options = new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
            };

            // Quatre entrées parce que le tableau de bord Hangfire ne permet pas de passer d'argument :
            // le mode et le dry run doivent être figés à l'enregistrement. Toutes en Cron.Never, rien ne
            // part tout seul - ce job déplace de l'argent réel.
            RecurringJob.AddOrUpdate<RepairEndedSubscriptionReservations>(DeliverDryRunJobName,
                x => x.Run(RepairMode.Deliver, true), Cron.Never(), options);

            RecurringJob.AddOrUpdate<RepairEndedSubscriptionReservations>(DeliverApplyJobName,
                x => x.Run(RepairMode.Deliver, false), Cron.Never(), options);

            RecurringJob.AddOrUpdate<RepairEndedSubscriptionReservations>(ReleaseDryRunJobName,
                x => x.Run(RepairMode.Release, true), Cron.Never(), options);

            RecurringJob.AddOrUpdate<RepairEndedSubscriptionReservations>(ReleaseApplyJobName,
                x => x.Run(RepairMode.Release, false), Cron.Never(), options);
        }

        /// <summary>
        /// <see cref="DisableConcurrentExecutionAttribute"/> est indispensable, pas décoratif :
        /// l'idempotence repose sur <c>RemainingAllocatedAmount &gt; 0</c> tel que LU en base, et cette
        /// colonne n'a pas de jeton de concurrence. Deux exécutions Apply qui se chevauchent
        /// sélectionnent donc les mêmes paires et livrent (ou relâchent) chacune le même argent : deux
        /// jeux de transactions et de journaux, pour une seule réservation. Or le tableau de bord
        /// Hangfire laisse cliquer « Trigger now » deux fois, et le serveur a plusieurs workers. Le
        /// verrou est pris sur la méthode, donc partagé par les quatre entrées : un Deliver et un
        /// Release ne peuvent pas non plus se marcher dessus.
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
        public async Task<Report> Run(RepairMode mode, bool dryRun = true)
        {
            logger.LogInformation($"RepairEndedSubscriptionReservations :: start (mode: {mode}, dryRun: {dryRun})");

            // La fenêtre se compare en DATE : EndDate est stocké à minuit UTC alors que le run tombe en
            // cours de journée. Comparer les timestamps déclarerait terminé un abonnement dont c'est
            // justement aujourd'hui le dernier jour, et volerait à ses participants le versement que le
            // job régulier doit encore livrer (même piège que CRCL-2675).
            var today = clock.GetCurrentInstant().ToDateTimeUtc().Date;

            var candidates = await db.SubscriptionBeneficiaries
                .Include(x => x.Subscription).ThenInclude(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Subscription).ThenInclude(x => x.BudgetAllowances)
                .Include(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.BeneficiaryType)
                .Include(x => x.BudgetAllowance).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .AsSplitQuery()
                .Where(x => x.BudgetAllowanceId != null)
                .Where(x => x.RemainingAllocatedAmount > 0)
                .Where(x => x.Subscription.EndDate < today)
                .ToListAsync();

            // Une paire à réservation inconnue sur un abonnement terminé cache peut-être le même argent
            // orphelin, mais on ne sait pas combien. Le rapport doit le dire plutôt que de laisser croire
            // que le total est complet.
            var unknownReservationPairCount = await db.SubscriptionBeneficiaries
                .Where(x => x.BudgetAllowanceId != null)
                .Where(x => x.RemainingAllocatedAmount == null)
                .Where(x => x.Subscription.EndDate < today)
                .CountAsync();

            var lines = new List<PairLine>();

            foreach (var pair in candidates)
            {
                lines.Add(await RepairAsync(pair, mode, dryRun, today));
            }

            var report = new Report
            {
                Mode = mode,
                DryRun = dryRun,
                Pairs = lines,
                UnknownReservationPairCount = unknownReservationPairCount
            };

            LogReport(report);

            if (dryRun)
            {
                logger.LogInformation("RepairEndedSubscriptionReservations :: DRY RUN, aucune écriture. Relancer le job Apply du même mode pour appliquer.");
                return report;
            }

            // Release crédite l'enveloppe, et Deliver aussi pour un participant sans carte. AvailableFund
            // étant un jeton de concurrence, un SaveChanges brut ferait échouer tout le run dès qu'un
            // mouvement d'enveloppe ordinaire s'est glissé entre le chargement des candidats et
            // l'écriture. Le rebase réapplique nos crédits sur le solde réel ; un crédit n'est jamais
            // refusé, donc le tout-ou-rien du run est préservé.
            await db.SaveChangesWithBudgetAllowanceRetryAsync();
            logger.LogInformation($"RepairEndedSubscriptionReservations :: appliqué - {report.Delivered.Count} versement(s) pour {report.TotalDelivered}, {report.Released.Count} relâchement(s) pour {report.TotalReleased}.");

            return report;
        }

        private async Task<PairLine> RepairAsync(SubscriptionBeneficiary pair, RepairMode mode, bool dryRun, DateTime today)
        {
            var reserved = pair.RemainingAllocatedAmount.Value;
            var subscription = pair.Subscription;
            var types = subscription.Types.Where(x => x.BeneficiaryTypeId == pair.BeneficiaryTypeId).ToList();
            var amountPerPayment = types.Sum(x => x.Amount);

            var line = new PairLine
            {
                BeneficiaryId = pair.BeneficiaryId,
                SubscriptionId = pair.SubscriptionId,
                SubscriptionName = subscription.Name,
                BudgetAllowanceId = pair.BudgetAllowanceId.Value,
                BeneficiaryOrganizationName = pair.Beneficiary.Organization.Name,
                EnvelopeOrganizationName = pair.BudgetAllowance.Organization.Name,
                Reserved = reserved,
                AmountPerPayment = amountPerPayment
            };

            if (mode == RepairMode.Release)
            {
                // Rendre l'argent ne demande de connaître ni le calendrier ni les types de versement :
                // le montant réservé est exactement ce qui est sorti de l'enveloppe et n'y est pas revenu.
                // C'est pour ça que Release n'a aucun des gardes du mode Deliver ci-dessous.
                if (!dryRun) Release(pair, reserved, types);

                // Faute de types, le journal ne peut pas ventiler le montant par groupe de produits : la
                // ligne du rapport ne totalisera pas ses colonnes. L'argent revient quand même - le
                // laisser immobilisé serait pire - mais l'opérateur doit voir lesquelles sont concernées.
                return types.Count == 0 || amountPerPayment <= 0
                    ? line with { Action = RepairAction.Release, Reason = "relâché sans ventilation par groupe de produits (aucun type de versement)" }
                    : line with { Action = RepairAction.Release };
            }

            // À partir d'ici : mode Deliver. Chaque refus ci-dessous laisse la réservation intacte plutôt
            // que de deviner - l'opérateur voit la ligne dans le rapport et peut choisir le mode Release
            // pour ces paires.
            if (pair.BeneficiaryTypeId == null || types.Count == 0 || amountPerPayment <= 0)
            {
                return line with { Action = RepairAction.Skip, Reason = "aucun type de versement pour ce type de bénéficiaire" };
            }

            // Un versement produit une transaction par groupe de produits, chacune d'un montant fixe. Une
            // réservation qui n'est pas un multiple entier du versement ne se répartit donc pas sans
            // inventer une clé de répartition, ce que ce job ne fera pas sur de l'argent réel.
            if (reserved % amountPerPayment != 0)
            {
                return line with { Action = RepairAction.Skip, Reason = $"réservation {reserved} non multiple du versement {amountPerPayment}" };
            }

            var cycles = (int)(reserved / amountPerPayment);

            if (pair.Beneficiary.Card == null)
            {
                // Rien à livrer : sans carte, le job régulier aurait relâché vers l'enveloppe. On
                // reproduit ce comportement plutôt que d'immobiliser l'argent une fois de plus. Le
                // relâchement lui-même est fait par AddingFundToCard, qui le journalise comme tel.
                //
                // Mais AddingFundToCard retrouve l'enveloppe par l'organisation COURANTE du participant,
                // pas par celle d'où l'argent est sorti. Si le participant a changé de groupe depuis son
                // assignation, il n'en trouverait aucune (exception, et tout le lot est perdu) ou en
                // créditerait une autre que celle qui a été débitée. On écarte donc la paire dès que les
                // deux ne coïncident pas : le mode Release, lui, vise directement la bonne enveloppe.
                // Ce même chemin appelle GetEffectiveMaxNumberOfPayments, qui est mutuellement récursif
                // avec GetTotalPayment sans cas terminal quand un abonnement usage-based n'a pas de
                // MaxNumberOfPayments et que la paire n'a pas d'override. Le résultat serait une
                // StackOverflowException : impossible à intercepter, elle tue le worker Hangfire et
                // emporte tout le lot. BackfillSubscriptionBeneficiaryAllocation refuse de tourner face à
                // cette forme de données ; ici on se contente d'écarter la paire, pour ne pas priver de
                // leur rattrapage tous les autres participants. Le mode Release, purement arithmétique,
                // traite ces paires sans problème.
                if (subscription.IsSubscriptionPaymentBasedCardUsage
                    && subscription.MaxNumberOfPayments == null
                    && pair.MaxNumberOfPaymentsOverride == null)
                {
                    return line with
                    {
                        Action = RepairAction.Skip,
                        Cycles = cycles,
                        Reason = "abonnement usage-based sans MaxNumberOfPayments et participant sans carte - utiliser le mode Release"
                    };
                }

                var refundTarget = subscription.BudgetAllowances
                    .FirstOrDefault(x => x.OrganizationId == pair.Beneficiary.OrganizationId);

                if (refundTarget == null || refundTarget.Id != pair.BudgetAllowanceId.Value)
                {
                    return line with
                    {
                        Action = RepairAction.Skip,
                        Cycles = cycles,
                        Reason = "participant sans carte dont le groupe ne correspond plus à l'enveloppe réservée - utiliser le mode Release"
                    };
                }

                if (!dryRun) await DeliverAsync(pair, cycles);
                return line with { Action = RepairAction.Release, Cycles = cycles, Reason = "participant sans carte" };
            }

            var expiration = subscription.GetExpirationDate(clock);
            if (expiration <= today)
            {
                return line with { Action = RepairAction.Skip, Cycles = cycles, Reason = $"fonds déjà expirés le {expiration:yyyy-MM-dd}" };
            }

            if (!dryRun) await DeliverAsync(pair, cycles);
            return line with { Action = RepairAction.Deliver, Cycles = cycles };
        }

        /// <summary>
        /// Rejoue les versements manqués par le même chemin que le job régulier, un cycle à la fois.
        /// Chaque appel consomme un versement de la réservation ; le garde de divisibilité de
        /// <see cref="RepairAsync"/> garantit qu'elle tombe exactement à zéro.
        ///
        /// L'identité passée en <c>initiatedBy</c> a deux rôles : rendre la réparation reconnaissable
        /// dans les rapports, et forcer <see cref="AddingFundToCard"/> à traiter l'appel comme un
        /// versement délibéré. Sans elle, un abonnement usage-based inspecterait l'historique de la carte
        /// et pourrait relâcher l'argent au lieu de le livrer, ce qui rendrait le mode Deliver
        /// imprévisible.
        /// </summary>
        private async Task DeliverAsync(SubscriptionBeneficiary pair, int cycles)
        {
            var job = new AddingFundToCard(db, clock, addingFundLogger);

            for (var cycle = 0; cycle < cycles; cycle++)
            {
                await job.AddFundToExistingSubscriptionBeneficiary(pair, new AddingFundToCard.InitiatedBy
                {
                    TransactionInitiatorFirstname = "Reparation",
                    TransactionInitiatorLastname = "CRCL-2676"
                });
            }
        }

        private void Release(SubscriptionBeneficiary pair, decimal reserved, IReadOnlyCollection<SubscriptionType> types)
        {
            var beneficiary = pair.Beneficiary;
            var subscription = pair.Subscription;
            var envelope = pair.BudgetAllowance;

            envelope.AvailableFund += reserved;
            pair.AdjustAllocation(-reserved, logger);

            logger.LogInformation($"RepairEndedSubscriptionReservations :: relâche {reserved} vers l'enveloppe {pair.BudgetAllowanceId} pour bénéficiaire {beneficiary.Id} / abonnement {subscription.Id} (abonnement terminé, versement jamais livré)");

            db.TransactionLogs.Add(new TransactionLog
            {
                Discriminator = TransactionLogDiscriminator.ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                TotalAmount = reserved,
                BeneficiaryId = beneficiary.Id,
                BeneficiaryID1 = beneficiary.ID1,
                BeneficiaryID2 = beneficiary.ID2,
                BeneficiaryFirstname = beneficiary.Firstname,
                BeneficiaryLastname = beneficiary.Lastname,
                BeneficiaryEmail = beneficiary.Email,
                BeneficiaryPhone = beneficiary.Phone,
                BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,

                // Le groupe de l'ENVELOPPE créditée, pas celui du participant. TransactionLog n'a pas de
                // colonne d'enveloppe : OrganizationId est le seul lien. Prendre le groupe courant du
                // participant ferait apparaître le remboursement dans le rapport d'un groupe dont
                // l'enveloppe n'a pas bougé, et laisserait sans trace celui qui a réellement été crédité,
                // dès qu'un participant a changé de groupe depuis son assignation.
                OrganizationId = envelope.OrganizationId,
                OrganizationName = envelope.Organization.Name,
                SubscriptionId = subscription.Id,
                SubscriptionName = subscription.Name,
                ProjectId = envelope.Organization.ProjectId,
                ProjectName = envelope.Organization.Project.Name,
                TransactionLogProductGroups = SplitByProductGroup(reserved, types)
            });
        }

        /// <summary>
        /// Répartit le montant relâché entre les groupes de produits de la paire, au prorata du versement.
        /// Le dernier groupe absorbe le reste d'arrondi, pour que la somme des lignes égale toujours
        /// <c>TotalAmount</c> : un rapport dont les colonnes ne totalisent pas la ligne est pire qu'une
        /// répartition approximative.
        ///
        /// Sans type de versement, il n'y a rien à ventiler et la liste revient vide : la ligne du rapport
        /// portera son total sans détail par groupe. <see cref="RepairAsync"/> marque ces paires pour que
        /// l'écart soit visible plutôt que subi.
        /// </summary>
        private static List<TransactionLogProductGroup> SplitByProductGroup(decimal amount, IReadOnlyCollection<SubscriptionType> types)
        {
            var groups = new List<TransactionLogProductGroup>();
            var amountPerPayment = types.Sum(x => x.Amount);
            if (types.Count == 0 || amountPerPayment <= 0) return groups;

            var remaining = amount;
            var lastType = types.Last();

            foreach (var type in types)
            {
                var share = ReferenceEquals(type, lastType)
                    ? remaining
                    : Math.Round(amount * (type.Amount / amountPerPayment), 2);

                remaining -= share;

                groups.Add(new TransactionLogProductGroup
                {
                    Amount = share,
                    ProductGroupId = type.ProductGroupId,
                    ProductGroupName = type.ProductGroup?.Name
                });
            }

            return groups;
        }

        private void LogReport(Report report)
        {
            logger.LogInformation(
                $"RepairEndedSubscriptionReservations :: {report.Pairs.Count} paire(s) orpheline(s) pour {report.TotalReserved}, " +
                $"{report.Delivered.Count} à verser pour {report.TotalDelivered}, " +
                $"{report.Released.Count} à relâcher pour {report.TotalReleased}, " +
                $"{report.Skipped.Count} écartée(s) pour {report.TotalSkipped}.");

            if (report.UnknownReservationPairCount > 0)
            {
                logger.LogWarning(
                    $"RepairEndedSubscriptionReservations :: {report.UnknownReservationPairCount} paire(s) sur abonnement terminé " +
                    "dont la réservation est inconnue (null) - elles peuvent cacher le même argent orphelin. " +
                    "Lancer BackfillSubscriptionBeneficiaryAllocation puis relancer ce rapport.");
            }

            logger.LogInformation(
                "RepairEndedSubscriptionReservations :: par enveloppe (CSV) - " +
                "BudgetAllowanceId;Organisation;Abonnement;Paires;Reserve;AVerser;ARelacher;Ecarte");

            foreach (var envelope in report.Envelopes)
            {
                logger.LogInformation(
                    $"RepairEndedSubscriptionReservations :: {envelope.BudgetAllowanceId};{Csv(envelope.OrganizationName)};" +
                    $"{Csv(envelope.SubscriptionName)};{envelope.PairCount};{envelope.Reserved};{envelope.ToDeliver};" +
                    $"{envelope.ToRelease};{envelope.Skipped}");
            }

            if (report.Skipped.Count == 0) return;

            logger.LogWarning(
                "RepairEndedSubscriptionReservations :: paires écartées (CSV) - " +
                "BeneficiaryId;GroupeParticipant;SubscriptionId;BudgetAllowanceId;GroupeEnveloppe;Reserve;Versement;Raison");

            foreach (var line in report.Skipped)
            {
                logger.LogWarning(
                    $"RepairEndedSubscriptionReservations :: {line.BeneficiaryId};{Csv(line.BeneficiaryOrganizationName)};" +
                    $"{line.SubscriptionId};{line.BudgetAllowanceId};{Csv(line.EnvelopeOrganizationName)};" +
                    $"{line.Reserved};{line.AmountPerPayment};{Csv(line.Reason)}");
            }
        }

        // Les noms saisis par les utilisateurs peuvent contenir le séparateur, ce qui décalerait les
        // colonnes une fois collé dans un tableur.
        private static string Csv(string value) => value?.Replace(';', ',');

        public enum RepairMode
        {
            /// <summary>Verser en retard sur la carte ce qui était dû, sans redébiter l'enveloppe.</summary>
            Deliver = 0,

            /// <summary>Remettre le montant réservé dans l'enveloppe.</summary>
            Release = 1
        }

        public enum RepairAction
        {
            Deliver = 0,
            Release = 1,
            Skip = 2
        }

        public record PairLine
        {
            public long BeneficiaryId { get; init; }
            public long SubscriptionId { get; init; }
            public string SubscriptionName { get; init; }
            public long BudgetAllowanceId { get; init; }

            /// <summary>Groupe COURANT du participant : sert à le retrouver dans la liste des écartées.</summary>
            public string BeneficiaryOrganizationName { get; init; }

            /// <summary>
            /// Groupe qui possède l'enveloppe débitée. Distinct du précédent dès qu'un participant a
            /// changé de groupe depuis son assignation, et c'est celui-là qui nomme l'enveloppe.
            /// </summary>
            public string EnvelopeOrganizationName { get; init; }

            /// <summary>Montant orphelin de la paire : sorti de l'enveloppe, ni livré ni relâché.</summary>
            public decimal Reserved { get; init; }

            public decimal AmountPerPayment { get; init; }

            /// <summary>Nombre de versements que la réservation représente, quand il est déterminable.</summary>
            public int Cycles { get; init; }

            public RepairAction Action { get; init; }

            /// <summary>Pourquoi la paire a été écartée, ou relâchée alors que le mode demandait un versement.</summary>
            public string Reason { get; init; }
        }

        public class Report
        {
            public RepairMode Mode { get; init; }
            public bool DryRun { get; init; }
            public IReadOnlyList<PairLine> Pairs { get; init; } = new List<PairLine>();

            /// <summary>
            /// Paires sur abonnement terminé dont la réservation vaut encore null. Tant qu'il en reste,
            /// l'argent orphelin rapporté ici est un minorant.
            /// </summary>
            public int UnknownReservationPairCount { get; init; }

            public IReadOnlyList<PairLine> Delivered => Pairs.Where(x => x.Action == RepairAction.Deliver).ToList();
            public IReadOnlyList<PairLine> Released => Pairs.Where(x => x.Action == RepairAction.Release).ToList();
            public IReadOnlyList<PairLine> Skipped => Pairs.Where(x => x.Action == RepairAction.Skip).ToList();

            public decimal TotalReserved => Pairs.Sum(x => x.Reserved);
            public decimal TotalDelivered => Delivered.Sum(x => x.Reserved);
            public decimal TotalReleased => Released.Sum(x => x.Reserved);
            public decimal TotalSkipped => Skipped.Sum(x => x.Reserved);

            public IReadOnlyList<EnvelopeLine> Envelopes => Pairs
                .GroupBy(x => x.BudgetAllowanceId)
                .Select(x => new EnvelopeLine
                {
                    BudgetAllowanceId = x.Key,
                    OrganizationName = x.First().EnvelopeOrganizationName,
                    SubscriptionName = x.First().SubscriptionName,
                    PairCount = x.Count(),
                    Reserved = x.Sum(y => y.Reserved),
                    ToDeliver = x.Where(y => y.Action == RepairAction.Deliver).Sum(y => y.Reserved),
                    ToRelease = x.Where(y => y.Action == RepairAction.Release).Sum(y => y.Reserved),
                    Skipped = x.Where(y => y.Action == RepairAction.Skip).Sum(y => y.Reserved)
                })
                .OrderByDescending(x => x.Reserved)
                .ToList();
        }

        public class EnvelopeLine
        {
            public long BudgetAllowanceId { get; init; }
            public string OrganizationName { get; init; }
            public string SubscriptionName { get; init; }
            public int PairCount { get; init; }
            public decimal Reserved { get; init; }
            public decimal ToDeliver { get; init; }
            public decimal ToRelease { get; init; }
            public decimal Skipped { get; init; }
        }
    }
}
