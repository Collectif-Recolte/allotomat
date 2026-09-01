using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    /// <summary>
    /// CRCL-2678 — rend aux enveloppes les crédits perdus sous concurrence.
    ///
    /// Le bogue corrigé par CRCL-2677 : <c>RemoveBeneficiaryFromSubscription</c> faisait un
    /// lire-modifier-écrire sur <c>AvailableFund</c> sans protection. Sur des retraits en rafale, le
    /// crédit du Nième retrait écrasait celui du précédent. L'INSERT du <c>TransactionLog</c>, lui,
    /// survivait toujours.
    ///
    /// D'où la forme de la réparation, et c'est le point à comprendre avant de relire le reste :
    /// <b>le grand livre est déjà juste, c'est le solde qui est faux</b>. Les logs de remboursement
    /// existent pour chacun des crédits perdus. Créditer l'enveloppe remet donc le solde en accord avec
    /// le grand livre — et écrire un <c>TransactionLog</c> de remboursement supplémentaire ferait
    /// exactement l'inverse : le rapport de transactions compterait deux fois le même remboursement.
    /// La trace de la correction est donc un <see cref="BudgetAllowanceLog"/>, au niveau de l'enveloppe,
    /// qui alimente le rapport d'enveloppes sans toucher au rapport de transactions.
    ///
    /// C'est aussi ce log qui rend le job rejouable sans danger : une enveloppe déjà porteuse d'un log
    /// de correction est sautée. Sur un job d'argent réel lancé à la main depuis Hangfire, où rien
    /// n'empêche un double clic, ça n'est pas un luxe.
    ///
    /// Le montant crédité vient de la table validée par Récolte (<see cref="ReviewedCorrections"/>),
    /// jamais du recalcul. Le recalcul est reporté à côté, comme contrôle : voir
    /// <see cref="CorrectionLine.ComputedShortfall"/> pour ce qu'il vaut et ce qu'il ne vaut pas.
    ///
    /// Deuxième volet, indépendant du premier : les réservations négatives des paires vivantes sont
    /// remises à 0. Voir <see cref="NormalizeNegativeReservationsAsync"/>.
    ///
    /// <b>À lancer après le déploiement de CRCL-2677</b>, pour deux raisons. Sans le correctif, la cause
    /// tourne toujours et rouvrirait l'écart derrière le job. Et ce job crédite lui-même par
    /// lire-modifier-écrire : tant que <c>BudgetAllowances</c> n'a pas de jeton de concurrence, un retrait
    /// simultané peut écraser le crédit sans rien dire. Avec le jeton, le même conflit fait échouer le
    /// <c>SaveChanges</c> — rien n'est appliqué et ça se voit, ce qui est le bon comportement ici.
    /// </summary>
    public class CreditLostBudgetAllowanceRefunds
    {
        public const string DryRunJobName = "CreditLostBudgetAllowanceRefunds:DryRun:Never";
        public const string ApplyJobName = "CreditLostBudgetAllowanceRefunds:Apply:Never";

        /// <summary>
        /// Les écarts relevés par l'enquête CRCL-2674 et repris dans CRCL-2678. Le total confirmé est
        /// de 5 076 $ sur la saison 2026 de BC FMNCP.
        ///
        /// Les deux derniers portent la même signature mais sont antérieurs et n'ont pas été confirmés :
        /// ils sont là pour apparaître au rapport, et <see cref="Correction.RequiresConfirmation"/> les
        /// empêche d'être crédités. Pour les créditer, il faut passer le drapeau à false — donc un
        /// changement de code, donc une relecture.
        /// </summary>
        // Les noms doivent être ceux de la base, au caractère près : la correspondance est exacte, pas
        // approximative. Le ticket les abrège (« CR Transition Society - Rose Harbour »), la base ne les
        // abrège pas - une abréviation ici se solde par un SkippedEnvelopeNotFound et un remboursement
        // jamais rendu. Vérifiés contre la production le 2026-09-01.
        public static readonly IReadOnlyList<Correction> ReviewedCorrections = new List<Correction>
        {
            new("Mid-Island Pensioners & Hobbyist Assoc", "FMNCP 2026", 1080m),
            new("Campbell River and North Island Transition Society- Rose Harbour", "FMNCP 2026", 864m),
            new("Fernie Citizens Housing Society", "FMNCP 2026", 756m),
            new("Campbell River and North Island Transition Society- Women's Centre", "FMNCP 2026", 756m),
            new("Fernie Family Housing Society", "FMNCP 2026", 648m),
            new("Fernie Womens Resource Centre- Bellies to Babies", "FMNCP 2026", 648m),
            new("Tobacco Plains Indian Band", "FMNCP 2026", 216m),
            new("Elk Valley Family Society", "FMNCP 2026", 108m),

            new("Family Education & Support Centre", "Haney Winter 2024", 1890.99m, RequiresConfirmation: true),
            new("Cumberland Community Schools Society", "FMNCP 2023", 1215m, RequiresConfirmation: true)
        };

        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<CreditLostBudgetAllowanceRefunds> logger;

        public CreditLostBudgetAllowanceRefunds(
            AppDbContext db, IClock clock, ILogger<CreditLostBudgetAllowanceRefunds> logger)
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

            // Deux entrées, comme pour BackfillSubscriptionBeneficiaryAllocation : le tableau de bord
            // Hangfire ne permet pas de passer un argument, donc sans le job Apply il n'y aurait aucun
            // moyen d'appliquer la correction. Les deux sont en Cron.Never, rien ne part tout seul.
            RecurringJob.AddOrUpdate<CreditLostBudgetAllowanceRefunds>(DryRunJobName,
                x => x.Run(true), Cron.Never(), options);

            RecurringJob.AddOrUpdate<CreditLostBudgetAllowanceRefunds>(ApplyJobName,
                x => x.Run(false), Cron.Never(), options);
        }

        /// <summary>
        /// <see cref="DisableConcurrentExecutionAttribute"/> est indispensable, pas décoratif : le garde
        /// anti-rejeu lit les logs ENREGISTRÉS, donc deux exécutions qui se chevauchent ne se voient pas
        /// l'une l'autre et créditeraient toutes les deux. Or le tableau de bord Hangfire laisse
        /// parfaitement cliquer « Trigger now » deux fois, et le serveur a plusieurs workers.
        /// Sérialiser les exécutions est ce qui rend le garde effectif.
        /// </summary>
        [DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
        public Task<Report> Run(bool dryRun = true) => Run(ReviewedCorrections, dryRun);

        public async Task<Report> Run(IReadOnlyList<Correction> corrections, bool dryRun)
        {
            logger.LogInformation($"CreditLostBudgetAllowanceRefunds :: start (dryRun: {dryRun})");

            // Une enveloppe citée deux fois n'est pas rattrapée en silence : le garde anti-rejeu la
            // verrait au deuxième passage et la classerait « déjà créditée », ce qui laisserait croire à
            // un run antérieur alors que le vrai problème est une faute de copie dans la table. On refuse
            // donc le run entier, avec le nom de l'enveloppe fautive.
            var duplicates = corrections
                .GroupBy(x => (x.OrganizationName, x.SubscriptionName))
                .Where(x => x.Count() > 1)
                .ToList();

            if (duplicates.Count > 0)
            {
                logger.LogError(
                    $"CreditLostBudgetAllowanceRefunds :: ABANDON - {duplicates.Count} enveloppe(s) citée(s) " +
                    "plus d'une fois dans la table de corrections, ce qui les créditerait plusieurs fois : " +
                    string.Join(", ", duplicates.Select(x => $"{x.Key.OrganizationName} / {x.Key.SubscriptionName}")) +
                    ". Corriger la table avant de relancer.");

                return new Report { DryRun = dryRun, Abandoned = true };
            }

            var lines = new List<CorrectionLine>();

            foreach (var correction in corrections)
            {
                var line = await BuildLineAsync(correction, dryRun);
                lines.Add(line);

                // Enregistré enveloppe par enveloppe, et non en un seul SaveChanges à la fin, pour deux
                // raisons.
                //
                // D'abord la concurrence : ce job crédite lui-même par lire-modifier-écrire, et tant que
                // CRCL-2677 n'a pas posé de jeton sur BudgetAllowances, tout remboursement qui s'insère
                // entre la lecture et l'écriture est écrasé sans bruit. Écrire tout de suite réduit cette
                // fenêtre à une enveloppe au lieu du run entier - le balayage des réservations négatives,
                // qui est long, se retrouve hors de la fenêtre.
                //
                // Ensuite la reprise : le crédit et sa trace partent dans le même SaveChanges, donc une
                // interruption en cours de route laisse un état cohérent et le relancer reprend là où il
                // s'est arrêté. L'atomicité du run entier n'aurait rien apporté ici : les corrections
                // sont indépendantes.
                if (!dryRun && line.Outcome == Outcome.Credited)
                {
                    await db.SaveChangesAsync();
                }
            }

            // Après les corrections, jamais avant : le recalcul de contrôle lit les réservations, et les
            // valeurs négatives font partie de la somme qui le rend juste (voir ComputedShortfall).
            // Normaliser d'abord fausserait silencieusement le contrôle de chaque enveloppe.
            var negativeReservations = await NormalizeNegativeReservationsAsync(dryRun);

            if (!dryRun && negativeReservations.Count > 0)
            {
                await db.SaveChangesAsync();
            }

            var report = new Report
            {
                DryRun = dryRun,
                Corrections = lines,
                NegativeReservations = negativeReservations
            };

            LogReport(report);

            return report;
        }

        private async Task<CorrectionLine> BuildLineAsync(Correction correction, bool dryRun)
        {
            var line = new CorrectionLine
            {
                OrganizationName = correction.OrganizationName,
                SubscriptionName = correction.SubscriptionName,
                Credit = correction.ExpectedCredit
            };

            var envelopes = await db.BudgetAllowances
                .Include(x => x.Organization)
                .Include(x => x.Subscription)
                .Where(x => x.Organization.Name == correction.OrganizationName
                    && x.Subscription.Name == correction.SubscriptionName)
                .ToListAsync();

            if (envelopes.Count == 0)
            {
                line.Outcome = Outcome.SkippedEnvelopeNotFound;
                line.Note = await DescribeNearMissesAsync(correction);
                return line;
            }

            // Rien n'impose l'unicité des noms d'organisation ni d'abonnement : ils sont saisis par les
            // utilisateurs. Créditer « la première trouvée » mettrait l'argent au hasard dans l'une des
            // deux, et le rapport affirmerait que c'est réglé.
            if (envelopes.Count > 1)
            {
                line.Outcome = Outcome.SkippedEnvelopeAmbiguous;
                line.Note = $"{envelopes.Count} enveloppes portent ces noms : " +
                    string.Join(", ", envelopes.Select(x => x.Id));
                return line;
            }

            var envelope = envelopes.Single();

            line.BudgetAllowanceId = envelope.Id;
            line.OriginalFund = envelope.OriginalFund;
            line.AvailableFundBefore = envelope.AvailableFund;
            line.AvailableFundAfter = envelope.AvailableFund;

            await FillControlFiguresAsync(line, envelope.Id, envelope.OrganizationId, envelope.SubscriptionId);

            var alreadyCredited = await db.BudgetAllowanceLogs.AnyAsync(x =>
                x.BudgetAllowanceId == envelope.Id
                && x.Discriminator == BudgetAllowanceLogDiscriminator.CreditLostRefundBudgetAllowanceLog);

            if (alreadyCredited)
            {
                line.Outcome = Outcome.SkippedAlreadyCredited;
                return line;
            }

            if (correction.RequiresConfirmation)
            {
                line.Outcome = Outcome.SkippedAwaitingConfirmation;
                return line;
            }

            // Une enveloppe ne peut pas contenir plus que ce qui lui a été confié. Si le crédit l'y
            // amène, la prémisse est fausse quelque part - l'écart a déjà été rendu par une autre voie,
            // ou l'enveloppe a été modifiée depuis l'enquête. Dans les deux cas on ne touche pas à
            // l'argent, on le dit.
            if (envelope.AvailableFund + correction.ExpectedCredit > envelope.OriginalFund)
            {
                line.Outcome = Outcome.SkippedWouldExceedOriginalFund;
                return line;
            }

            line.Outcome = Outcome.Credited;
            line.AvailableFundAfter = envelope.AvailableFund + correction.ExpectedCredit;

            if (dryRun) return line;

            envelope.AvailableFund += correction.ExpectedCredit;

            db.BudgetAllowanceLogs.Add(new BudgetAllowanceLog
            {
                Discriminator = BudgetAllowanceLogDiscriminator.CreditLostRefundBudgetAllowanceLog,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                Amount = correction.ExpectedCredit,
                ProjectId = envelope.Organization.ProjectId,
                BudgetAllowanceId = envelope.Id,
                OrganizationId = envelope.OrganizationId,
                OrganizationName = envelope.Organization.Name,
                SubscriptionId = envelope.SubscriptionId,
                SubscriptionName = envelope.Subscription.Name

                // Pas d'initiateur : aucun utilisateur n'a fait ce mouvement. Mettre l'admin qui a
                // cliqué dans Hangfire laisserait croire à une opération courante faite depuis l'appli.
            });

            return line;
        }

        /// <summary>
        /// Quand aucune enveloppe ne porte les deux noms, dit lesquelles portent l'un des deux.
        ///
        /// Les noms de <see cref="ReviewedCorrections"/> sont recopiés d'un ticket, où les tirets longs
        /// et les apostrophes n'ont pas forcément survécu au passage. La correspondance reste exacte —
        /// créditer une enveloppe approchante serait pire que ne rien faire — mais le rapport doit dire
        /// à quoi le nom cherché ressemble en base, sinon il faut aller le chercher à la main.
        /// </summary>
        private async Task<string> DescribeNearMissesAsync(Correction correction)
        {
            var sameSubscription = await db.BudgetAllowances
                .Where(x => x.Subscription.Name == correction.SubscriptionName)
                .Select(x => x.Organization.Name)
                .ToListAsync();

            if (sameSubscription.Count > 0)
            {
                return $"Aucune enveloppe « {correction.OrganizationName} » sur cet abonnement. " +
                    $"Organisations qui en ont une : {string.Join(" | ", sameSubscription.OrderBy(x => x))}";
            }

            var sameOrganization = await db.BudgetAllowances
                .Where(x => x.Organization.Name == correction.OrganizationName)
                .Select(x => x.Subscription.Name)
                .ToListAsync();

            if (sameOrganization.Count > 0)
            {
                return $"Aucun abonnement « {correction.SubscriptionName} » pour cette organisation. " +
                    $"Abonnements qu'elle a : {string.Join(" | ", sameOrganization.OrderBy(x => x))}";
            }

            return "Ni l'organisation ni l'abonnement ne portent une enveloppe sous ces noms.";
        }

        /// <summary>
        /// Reconstitue où est passé l'argent de l'enveloppe, pour que le dry run porte de quoi juger le
        /// montant validé au lieu de demander de le croire.
        /// </summary>
        private async Task FillControlFiguresAsync(
            CorrectionLine line, long budgetAllowanceId, long organizationId, long subscriptionId)
        {
            var deliveries = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Where(x => x.OrganizationId == organizationId
                    && x.SubscriptionType.SubscriptionId == subscriptionId)
                .Select(x => new { x.Amount, x.AvailableFund })
                .ToListAsync();

            line.Delivered = deliveries.Sum(x => x.Amount);
            line.StillOnCards = deliveries.Sum(x => x.AvailableFund);

            var reservations = await db.SubscriptionBeneficiaries
                .Where(x => x.BudgetAllowanceId == budgetAllowanceId)
                .Select(x => x.RemainingAllocatedAmount)
                .ToListAsync();

            line.Reserved = reservations.Where(x => x.HasValue).Sum(x => x.Value);

            // Une paire dont la réservation est encore inconnue compte pour 0 dans la somme, ce qui gonfle
            // l'écart recalculé d'autant. Sans ce compteur, le contrôle pourrait tomber pile sur le montant
            // validé par pure coïncidence et donner une confiance qu'il n'a pas. Même précaution que
            // VerifyBudgetAllowanceReservations, qui traite son propre contrôle comme un minorant tant
            // qu'il reste des inconnues.
            line.UnknownPairCount = reservations.Count(x => x == null);
        }

        /// <summary>
        /// Remet à 0 les réservations négatives des paires vivantes.
        ///
        /// Une réservation négative veut dire qu'il a été livré plus que réservé. La valeur n'a pas de
        /// sens - on n'a jamais réservé moins que rien - et elle fausse toutes les sommes d'audit par
        /// enveloppe.
        ///
        /// <b>L'enveloppe ne bouge pas.</b> C'est délibéré : la sur-livraison est un fait passé, l'argent
        /// est parti sur des cartes et a pu être dépensé. Reprendre l'écart à l'enveloppe serait une
        /// décision de Récolte, pas une correction de données.
        ///
        /// Aucun versement ne change de montant : <c>RemoveBeneficiaryFromSubscription</c> plafonne déjà
        /// son remboursement à <c>Math.Max(0, réservation)</c>, donc -648 et 0 remboursent tous les deux
        /// zéro. La seule différence de comportement possible est une paire qui recevrait ensuite un
        /// delta positif (changement de type d'abonnement) : elle repartirait de 0 au lieu de -648.
        /// C'est le bon point de départ, puisque la sur-livraison a déjà eu lieu et que l'enveloppe n'en
        /// a jamais été débitée.
        /// </summary>
        private async Task<List<NegativeReservationLine>> NormalizeNegativeReservationsAsync(bool dryRun)
        {
            var pairs = await db.SubscriptionBeneficiaries
                .Include(x => x.BudgetAllowance).ThenInclude(x => x.Organization)
                .Include(x => x.BudgetAllowance).ThenInclude(x => x.Subscription)
                .Where(x => x.RemainingAllocatedAmount < 0)
                .ToListAsync();

            var lines = new List<NegativeReservationLine>();

            foreach (var pair in pairs)
            {
                lines.Add(new NegativeReservationLine
                {
                    BeneficiaryId = pair.BeneficiaryId,
                    SubscriptionId = pair.SubscriptionId,
                    BudgetAllowanceId = pair.BudgetAllowanceId,
                    OrganizationName = pair.BudgetAllowance?.Organization?.Name,
                    SubscriptionName = pair.BudgetAllowance?.Subscription?.Name,
                    RemainingAllocatedAmount = pair.RemainingAllocatedAmount.Value
                });

                if (!dryRun)
                {
                    pair.RemainingAllocatedAmount = 0m;
                }
            }

            return lines;
        }

        private void LogReport(Report report)
        {
            // Le dry run est l'artefact sur lequel Récolte approuve un mouvement d'argent : il ne doit
            // jamais raconter au passé ce qui n'a pas eu lieu.
            var mode = report.DryRun ? "DRY RUN" : "APPLIQUÉ";
            var credited = report.DryRun ? "à créditer" : "créditée(s)";
            var normalized = report.DryRun ? "à remettre à 0" : "remise(s) à 0";

            logger.LogInformation(
                $"CreditLostBudgetAllowanceRefunds :: {mode} - {report.CreditedCorrections.Count} enveloppe(s) " +
                $"{credited} pour {report.TotalCredited} $, {report.SkippedCorrections.Count} sautée(s), " +
                $"{report.NegativeReservations.Count} réservation(s) négative(s) {normalized} " +
                $"({report.TotalNegativeReservation} $).");

            logger.LogInformation(
                "CreditLostBudgetAllowanceRefunds :: corrections (CSV) - " +
                "Organisation;Abonnement;EnveloppeId;OriginalFund;DisponibleAvant;Credit;DisponibleApres;" +
                "Livre;SurCartes;Depense;Reserve;ReservationsInconnues;EcartRecalcule;RecalculConcorde;Resultat;Note");

            foreach (var line in report.Corrections)
            {
                logger.LogInformation(
                    $"CreditLostBudgetAllowanceRefunds :: {Csv(line.OrganizationName)};{Csv(line.SubscriptionName)};" +
                    $"{line.BudgetAllowanceId};{line.OriginalFund};{line.AvailableFundBefore};{line.Credit};" +
                    $"{line.AvailableFundAfter};{line.Delivered};{line.StillOnCards};{line.Spent};{line.Reserved};" +
                    $"{line.UnknownPairCount};{line.ComputedShortfall};{line.MatchesReviewedAmount};" +
                    $"{line.Outcome};{Csv(line.Note)}");
            }

            foreach (var line in report.SkippedCorrections)
            {
                logger.LogWarning(
                    $"CreditLostBudgetAllowanceRefunds :: NON CRÉDITÉ - {line.OrganizationName} / " +
                    $"{line.SubscriptionName} ({line.Credit} $) : {line.Outcome}. {line.Note}");
            }

            if (report.MismatchedCorrections.Count > 0)
            {
                logger.LogWarning(
                    $"CreditLostBudgetAllowanceRefunds :: {report.MismatchedCorrections.Count} montant(s) que le " +
                    "recalcul ne retrouve pas. Le recalcul a des angles morts connus (expiration de fonds, " +
                    "désassignation de carte, versements manuels), donc ce n'est pas une preuve d'erreur - mais " +
                    "c'est à regarder avant d'appliquer : " +
                    string.Join(", ", report.MismatchedCorrections.Select(x =>
                        $"{x.OrganizationName} (validé {x.Credit}, recalculé {x.ComputedShortfall})")));
            }

            if (report.NegativeReservations.Count > 0)
            {
                logger.LogWarning(
                    "CreditLostBudgetAllowanceRefunds :: réservations négatives (CSV) - " +
                    "BeneficiaireId;AbonnementId;EnveloppeId;Organisation;Abonnement;Reservation");

                foreach (var line in report.NegativeReservations)
                {
                    logger.LogWarning(
                        $"CreditLostBudgetAllowanceRefunds :: {line.BeneficiaryId};{line.SubscriptionId};" +
                        $"{line.BudgetAllowanceId};{Csv(line.OrganizationName)};{Csv(line.SubscriptionName)};" +
                        $"{line.RemainingAllocatedAmount}");
                }
            }

            if (report.DryRun)
            {
                logger.LogInformation(
                    "CreditLostBudgetAllowanceRefunds :: DRY RUN, aucune écriture. Lancer le job Apply pour " +
                    "appliquer.");
            }
        }

        // Les noms d'organisation et d'abonnement sont saisis par les utilisateurs et peuvent contenir
        // le séparateur, ce qui décalerait les colonnes une fois collé dans un tableur.
        private static string Csv(string value) => value?.Replace(';', ',');

        /// <param name="ExpectedCredit">Le montant validé par Récolte, en dollars.</param>
        /// <param name="RequiresConfirmation">
        /// Vrai pour un écart à la même signature mais non confirmé : il sera reporté, jamais crédité.
        /// </param>
        public record Correction(
            string OrganizationName,
            string SubscriptionName,
            decimal ExpectedCredit,
            bool RequiresConfirmation = false);

        public enum Outcome
        {
            Credited,
            SkippedEnvelopeNotFound,
            SkippedEnvelopeAmbiguous,
            SkippedAlreadyCredited,
            SkippedAwaitingConfirmation,
            SkippedWouldExceedOriginalFund
        }

        public class Report
        {
            public bool DryRun { get; init; }

            /// <summary>Run interrompu avant tout examen : la table de corrections est fautive.</summary>
            public bool Abandoned { get; init; }
            public IReadOnlyList<CorrectionLine> Corrections { get; init; } = new List<CorrectionLine>();
            public IReadOnlyList<NegativeReservationLine> NegativeReservations { get; init; } = new List<NegativeReservationLine>();

            public IReadOnlyList<CorrectionLine> CreditedCorrections =>
                Corrections.Where(x => x.Outcome == Outcome.Credited).ToList();

            public IReadOnlyList<CorrectionLine> SkippedCorrections =>
                Corrections.Where(x => x.Outcome != Outcome.Credited).ToList();

            /// <summary>
            /// Les enveloppes dont le recalcul ne retrouve pas le montant validé. À relire avant
            /// d'appliquer, sans être une preuve d'erreur - voir <see cref="CorrectionLine.ComputedShortfall"/>.
            /// Les corrections sautées sont hors sujet : leur montant ne sera pas versé de toute façon.
            /// </summary>
            public IReadOnlyList<CorrectionLine> MismatchedCorrections =>
                Corrections.Where(x => x.Outcome == Outcome.Credited && !x.MatchesReviewedAmount).ToList();

            public decimal TotalCredited => CreditedCorrections.Sum(x => x.Credit);
            public decimal TotalNegativeReservation => NegativeReservations.Sum(x => x.RemainingAllocatedAmount);
        }

        public class CorrectionLine
        {
            public string OrganizationName { get; init; }
            public string SubscriptionName { get; init; }

            /// <summary>Le montant validé par Récolte. C'est lui qui est crédité, jamais le recalcul.</summary>
            public decimal Credit { get; init; }

            public long? BudgetAllowanceId { get; set; }
            public decimal OriginalFund { get; set; }
            public decimal AvailableFundBefore { get; set; }
            public decimal AvailableFundAfter { get; set; }

            /// <summary>Total versé sur les cartes depuis cette enveloppe, tel que journalisé.</summary>
            public decimal Delivered { get; set; }

            /// <summary>Ce qu'il reste de ces versements sur les cartes, non dépensé.</summary>
            public decimal StillOnCards { get; set; }

            /// <summary>Somme des réservations connues des paires de l'enveloppe.</summary>
            public decimal Reserved { get; set; }

            /// <summary>
            /// Paires dont la réservation vaut encore null. Chacune compte pour 0 dans
            /// <see cref="Reserved"/>, donc tant qu'il en reste, <see cref="ComputedShortfall"/> est un
            /// majorant et non une mesure.
            /// </summary>
            public int UnknownPairCount { get; set; }

            public Outcome Outcome { get; set; }
            public string Note { get; set; }

            public decimal Spent => Delivered - StillOnCards;

            /// <summary>
            /// Ce que les données disent qu'il manque : sorti de l'enveloppe et jamais revenu, moins ce
            /// qui est parti sur les cartes, moins ce qui est encore réservé.
            ///
            /// À lire comme un contrôle, pas comme une mesure. Il <b>sous-estime</b> l'écart dès que des
            /// fonds sont revenus des cartes (expiration, désassignation de carte) : ces retours ont
            /// recrédité l'enveloppe sans retirer la livraison correspondante de <see cref="Delivered"/>.
            /// Il ignore aussi les mouvements d'enveloppe non journalisés (CRCL-2679), et il compte les
            /// livraisons par (organisation, abonnement) plutôt que par enveloppe : si l'enveloppe
            /// courante a remplacé une enveloppe supprimée sur la même paire de noms, les livraisons de
            /// l'ancienne sont comptées contre le budget de la nouvelle. Le filtrer par les paires
            /// vivantes ne serait pas une amélioration - ce sont justement des participants retirés qui
            /// ont provoqué l'écart, leurs paires n'existent plus.
            ///
            /// C'est pourquoi il ne décide pas du montant crédité. Sur les enveloppes de CRCL-2678, sans
            /// expiration ni retour, il retrouve les écarts au sou près - d'où son intérêt comme contrôle.
            /// </summary>
            public decimal ComputedShortfall => OriginalFund - AvailableFundBefore - Delivered - Reserved;

            /// <summary>
            /// Vrai seulement si le recalcul retrouve le montant validé <b>et</b> qu'aucune réservation
            /// inconnue ne le rend approximatif. Une concordance obtenue avec des inconnues au tableau
            /// serait une coïncidence présentée comme une vérification.
            /// </summary>
            public bool MatchesReviewedAmount => UnknownPairCount == 0 && ComputedShortfall == Credit;
        }

        public class NegativeReservationLine
        {
            public long BeneficiaryId { get; init; }
            public long SubscriptionId { get; init; }
            public long? BudgetAllowanceId { get; init; }
            public string OrganizationName { get; init; }
            public string SubscriptionName { get; init; }
            public decimal RemainingAllocatedAmount { get; init; }
        }
    }
}
