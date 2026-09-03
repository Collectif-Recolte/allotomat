using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class CreditLostBudgetAllowanceRefundsTest : TestBase
    {
        private const string OrganizationName = "Mid-Island Pensioners & Hobbyist Assoc";
        private const string SubscriptionName = "FMNCP 2026";

        private readonly CreditLostBudgetAllowanceRefunds job;
        private readonly Project project;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;
        private readonly Subscription subscription;
        private readonly SubscriptionType subscriptionType;

        public CreditLostBudgetAllowanceRefundsTest()
        {
            project = new Project() { Name = "BC FMNCP" };
            DbContext.Projects.Add(project);

            organization = new Organization() { Name = OrganizationName, Project = project };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType() { Project = project, Keys = "type1", Name = "Type 1" };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            subscriptionType = new SubscriptionType()
            {
                Amount = 216,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup
            };

            subscription = new Subscription()
            {
                Name = SubscriptionName,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 7, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>() { subscriptionType },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            job = new CreditLostBudgetAllowanceRefunds(
                DbContext, Clock, NullLogger<CreditLostBudgetAllowanceRefunds>.Instance);
        }

        [Fact]
        public async Task CreditsTheReviewedAmountAndLeavesATrace()
        {
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: false);

            var line = report.Corrections.Single();
            line.Outcome.Should().Be(CreditLostBudgetAllowanceRefunds.Outcome.Credited);
            line.Credit.Should().Be(1080m);
            line.AvailableFundBefore.Should().Be(0m);
            line.AvailableFundAfter.Should().Be(1080m);
            report.TotalCredited.Should().Be(1080m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(1080m);

            var log = DbContext.BudgetAllowanceLogs.Single();
            log.Discriminator.Should().Be(BudgetAllowanceLogDiscriminator.CreditLostRefundBudgetAllowanceLog);
            log.Amount.Should().Be(1080m);
            log.BudgetAllowanceId.Should().Be(envelope.Id);
            log.OrganizationName.Should().Be(OrganizationName);
            log.SubscriptionName.Should().Be(SubscriptionName);
            log.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public async Task DryRunReportsTheSameCreditWithoutTouchingAnything()
        {
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: true);

            report.DryRun.Should().BeTrue();
            report.Corrections.Single().Outcome.Should().Be(CreditLostBudgetAllowanceRefunds.Outcome.Credited);
            report.Corrections.Single().AvailableFundAfter.Should().Be(1080m);
            report.TotalCredited.Should().Be(1080m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(0m);
            DbContext.BudgetAllowanceLogs.Should().BeEmpty();
        }

        [Fact]
        public async Task CreditsAnEnvelopeOnlyOnce()
        {
            // Le job est lancé à la main depuis Hangfire, où rien n'empêche de cliquer deux fois.
            // Un deuxième crédit doublerait l'argent rendu : la trace laissée par le premier est ce
            // qui rend le job rejouable sans danger.
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            await job.Run(Corrections(1080m), dryRun: false);
            var report = await job.Run(Corrections(1080m), dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedAlreadyCredited);
            report.TotalCredited.Should().Be(0m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(1080m);
            DbContext.BudgetAllowanceLogs.Should().HaveCount(1);
        }

        /// <summary>
        /// <c>AvailableFund</c> est un jeton de concurrence depuis CRCL-2677 : un mouvement d'enveloppe
        /// ordinaire (versement, retrait, ajustement) qui se glisse entre le chargement de l'enveloppe
        /// et l'écriture du crédit ferait échouer un <c>SaveChanges</c> brut — et sur un job d'argent
        /// lancé à la main, l'exception remonterait avant le rapport, laissant l'opérateur sans même la
        /// liste des enveloppes déjà créditées. Le crédit doit se rebaser sur le solde réel, par le même
        /// chemin que tous les autres mouvements d'enveloppe.
        /// </summary>
        [Fact]
        public async Task CreditSurvivesAnEnvelopeMovementMadeWhileTheJobWasRunning()
        {
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 500);
            DbContext.SaveChanges();

            // Le DbContext du job a déjà sa photographie de l'enveloppe (AvailableFund = 500). Un autre
            // écrivain la débite entre-temps : la base dit 300, le job croit encore 500.
            using (var other = CreateDbContext())
            {
                var concurrent = await other.BudgetAllowances.FindAsync(envelope.Id);
                concurrent.AvailableFund -= 200m;
                await other.SaveChangesAsync();
            }

            var report = await job.Run(Corrections(1080m), dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.Credited);
            report.TotalCredited.Should().Be(1080m);

            // 300 après le débit concurrent + 1 080 crédités : aucun des deux n'a écrasé l'autre.
            (await ReloadAsync(envelope)).AvailableFund.Should().Be(1380m);
            DbContext.BudgetAllowanceLogs.Should().HaveCount(1);
        }

        [Fact]
        public async Task RecalculatesTheEnvelopeAsAControlOnTheReviewedAmount()
        {
            // Les chiffres de Mid-Island / FMNCP 2026 tels que le client les a calculés (CRCL-2674) :
            // 8 208 d'enveloppe, 1 463,85 encore sur les cartes, 5 664,15 dépensés, aucune réservation
            // vivante. Il devrait rester 1 080, l'enveloppe est à 0.
            AddEnvelope(originalFund: 8208, availableFund: 0);
            AddDelivery(amount: 1463.85m, stillOnCard: 1463.85m);
            AddDelivery(amount: 5664.15m, stillOnCard: 0m);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: true);

            var line = report.Corrections.Single();
            line.Delivered.Should().Be(7128m);
            line.StillOnCards.Should().Be(1463.85m);
            line.Spent.Should().Be(5664.15m);
            line.Reserved.Should().Be(0m);
            line.ComputedShortfall.Should().Be(1080m);
            line.MatchesReviewedAmount.Should().BeTrue();
        }

        [Fact]
        public async Task FlagsAReviewedAmountTheDataNoLongerBacksWithoutRefusingIt()
        {
            // Le recalcul a des angles morts connus (expiration de fonds, désassignation de carte),
            // donc il informe le rapport mais ne décide pas du montant. L'écart doit sauter aux yeux
            // de qui relit le dry run, sans quoi on n'aurait plus de garde-fou du tout.
            AddEnvelope(originalFund: 8208, availableFund: 0);
            AddDelivery(amount: 7128m, stillOnCard: 0m);
            AddPair(remainingAllocatedAmount: 500m);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: true);

            var line = report.Corrections.Single();
            line.Reserved.Should().Be(500m);
            line.ComputedShortfall.Should().Be(580m);
            line.MatchesReviewedAmount.Should().BeFalse();
            line.Outcome.Should().Be(CreditLostBudgetAllowanceRefunds.Outcome.Credited);
            line.Credit.Should().Be(1080m);
            report.MismatchedCorrections.Should().HaveCount(1);
        }

        [Fact]
        public async Task WithholdsTheControlsBlessingWhileReservationsAreStillUnknown()
        {
            // Le recalcul tombe pile sur 1 080, mais une paire n'a pas encore de réservation connue :
            // elle compte pour 0, donc la concordance est une coïncidence. La présenter comme une
            // vérification donnerait une confiance imméritée avant d'approuver le mouvement d'argent.
            AddEnvelope(originalFund: 8208, availableFund: 0);
            AddDelivery(amount: 7128m, stillOnCard: 0m);
            AddPair(remainingAllocatedAmount: null);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: true);

            var line = report.Corrections.Single();
            line.ComputedShortfall.Should().Be(1080m);
            line.UnknownPairCount.Should().Be(1);
            line.MatchesReviewedAmount.Should().BeFalse();
            report.MismatchedCorrections.Should().HaveCount(1);
        }

        [Fact]
        public async Task RefusesToPushAnEnvelopeAboveItsOriginalBudget()
        {
            // Une enveloppe ne peut pas contenir plus que ce qui lui a été confié. Si le crédit l'y
            // amène, la prémisse est fausse quelque part et on ne touche pas à l'argent.
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 8000);
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedWouldExceedOriginalFund);
            report.TotalCredited.Should().Be(0m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(8000m);
            DbContext.BudgetAllowanceLogs.Should().BeEmpty();
        }

        [Fact]
        public async Task ReportsACorrectionWhoseEnvelopeIsNowhereToBeFound()
        {
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedEnvelopeNotFound);
            report.TotalCredited.Should().Be(0m);
        }

        [Fact]
        public async Task PointsAtTheRealNameWhenTheReviewedOneIsOffByAPunctuationMark()
        {
            // Les noms de la table viennent d'un ticket : « CR Transition Society — Rose Harbour » y
            // porte un tiret long. Sans cette aide, le dry run dirait « introuvable » et il faudrait
            // aller chercher le vrai nom en base à la main.
            AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(
                    "Mid-Island Pensioners and Hobbyist Assoc", SubscriptionName, 1080m)
            };

            var report = await job.Run(corrections, dryRun: true);

            var line = report.Corrections.Single();
            line.Outcome.Should().Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedEnvelopeNotFound);
            line.Note.Should().Contain(OrganizationName);
        }

        [Fact]
        public async Task ListsTheSubscriptionsAnOrganizationDoesHaveWhenTheSubscriptionNameIsWrong()
        {
            AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(OrganizationName, "FMNCP 2025", 1080m)
            };

            var report = await job.Run(corrections, dryRun: true);

            var line = report.Corrections.Single();
            line.Outcome.Should().Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedEnvelopeNotFound);
            line.Note.Should().Contain(SubscriptionName);
        }

        [Fact]
        public async Task RefusesToChooseBetweenTwoEnvelopesCarryingTheSameNames()
        {
            // Les noms sont saisis par les utilisateurs, rien ne garantit leur unicité. Créditer
            // « la première trouvée » mettrait l'argent au hasard dans l'une des deux.
            var otherOrganization = new Organization() { Name = OrganizationName, Project = project };
            DbContext.Organizations.Add(otherOrganization);

            AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.BudgetAllowances.Add(new BudgetAllowance()
            {
                OriginalFund = 8208,
                AvailableFund = 0,
                Organization = otherOrganization,
                Subscription = subscription
            });
            DbContext.SaveChanges();

            var report = await job.Run(Corrections(1080m), dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedEnvelopeAmbiguous);
            report.TotalCredited.Should().Be(0m);
            DbContext.BudgetAllowances.ToList().Should().OnlyContain(x => x.AvailableFund == 0m);
        }

        [Fact]
        public async Task NeverCreditsACorrectionStillAwaitingConfirmation()
        {
            // Les deux cas antérieurs (Haney 2024, Cumberland 2023) portent la même signature mais
            // n'ont pas été confirmés. Ils doivent apparaître au rapport et rester intouchés.
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(
                    OrganizationName, SubscriptionName, 1080m, RequiresConfirmation: true)
            };

            var report = await job.Run(corrections, dryRun: false);

            report.Corrections.Single().Outcome.Should()
                .Be(CreditLostBudgetAllowanceRefunds.Outcome.SkippedAwaitingConfirmation);
            report.TotalCredited.Should().Be(0m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(0m);
            DbContext.BudgetAllowanceLogs.Should().BeEmpty();
        }

        [Fact]
        public async Task NormalizesNegativeReservationsWithoutMovingAnyEnvelope()
        {
            // Une réservation négative veut dire qu'il a été livré plus que réservé. Le retrait
            // plafonne déjà son remboursement à 0, donc remettre la paire à 0 ne change aucun
            // versement ; ça enlève seulement une valeur qui n'a pas de sens des sommes d'audit.
            // L'enveloppe ne bouge pas de ce fait : la sur-livraison est un fait passé, pas une dette
            // à percevoir. Elle ne bouge ici que du crédit correctif lui-même.
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 1080);
            AddPair(remainingAllocatedAmount: -648m);
            AddPair(remainingAllocatedAmount: -54m);
            AddPair(remainingAllocatedAmount: 216m);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(OrganizationName, SubscriptionName, 1080m)
            };

            var report = await job.Run(corrections, dryRun: false);

            report.NegativeReservations.Should().HaveCount(2);
            report.NormalizedReservations.Should().HaveCount(2);
            report.OutOfScopeReservations.Should().BeEmpty();
            report.TotalNegativeReservation.Should().Be(-702m);

            var pairs = DbContext.SubscriptionBeneficiaries.ToList();
            pairs.Where(x => x.RemainingAllocatedAmount == 0m).Should().HaveCount(2);
            pairs.Should().NotContain(x => x.RemainingAllocatedAmount < 0m);
            pairs.Should().Contain(x => x.RemainingAllocatedAmount == 216m);

            // 1080 de départ + 1080 de crédit : la normalisation n'a rien ajouté au mouvement.
            (await ReloadAsync(envelope)).AvailableFund.Should().Be(2160m);
        }

        [Fact]
        public async Task ReportsNegativeReservationsOutsideThePerimeterWithoutTouchingThem()
        {
            // Le radar est global, l'écriture ne l'est pas. Une réservation négative sur une enveloppe
            // que la table de corrections ne nomme pas appartient à une autre enquête (CRCL-2681) : la
            // signaler oui, l'écrire non - on ne comprend pas ce programme-là, et tant que sa cause
            // tourne la valeur reviendrait de toute façon.
            AddEnvelope(originalFund: 8208, availableFund: 1080);
            AddPair(remainingAllocatedAmount: -648m);

            var otherEnvelope = AddEnvelopeForOtherOrganization(availableFund: 500m);
            AddPairOn(otherEnvelope, remainingAllocatedAmount: -175m);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(OrganizationName, SubscriptionName, 1080m)
            };

            var report = await job.Run(corrections, dryRun: false);

            report.NegativeReservations.Should().HaveCount(2);

            report.NormalizedReservations.Should().ContainSingle()
                .Which.RemainingAllocatedAmount.Should().Be(-648m);
            report.TotalNegativeReservation.Should().Be(-648m);

            var untouched = report.OutOfScopeReservations.Should().ContainSingle().Subject;
            untouched.RemainingAllocatedAmount.Should().Be(-175m);
            untouched.Normalized.Should().BeFalse();
            untouched.Note.Should().NotBeNullOrEmpty();
            report.TotalNegativeReservationOutOfScope.Should().Be(-175m);

            // Ce qui compte vraiment : la valeur hors périmètre est encore là, en base.
            DbContext.SubscriptionBeneficiaries
                .Should().Contain(x => x.RemainingAllocatedAmount == -175m);

            (await ReloadAsync(otherEnvelope)).AvailableFund.Should().Be(500m);
        }

        [Fact]
        public async Task WithoutCorrectionsNothingIsNormalized()
        {
            // Sans correction, le job n'a aucun périmètre : il ne peut que regarder.
            AddEnvelope(originalFund: 8208, availableFund: 1080);
            AddPair(remainingAllocatedAmount: -648m);
            DbContext.SaveChanges();

            var report = await job.Run(Array.Empty<CreditLostBudgetAllowanceRefunds.Correction>(), dryRun: false);

            report.NegativeReservations.Should().ContainSingle();
            report.NormalizedReservations.Should().BeEmpty();
            report.OutOfScopeReservations.Should().ContainSingle();
            report.TotalNegativeReservation.Should().Be(0m);

            DbContext.SubscriptionBeneficiaries.Single().RemainingAllocatedAmount.Should().Be(-648m);
        }

        [Fact]
        public async Task DryRunLeavesNegativeReservationsAsTheyAre()
        {
            AddEnvelope(originalFund: 8208, availableFund: 1080);
            AddPair(remainingAllocatedAmount: -648m);
            DbContext.SaveChanges();

            var report = await job.Run(Array.Empty<CreditLostBudgetAllowanceRefunds.Correction>(), dryRun: true);

            report.NegativeReservations.Should().HaveCount(1);
            DbContext.SubscriptionBeneficiaries.Single().RemainingAllocatedAmount.Should().Be(-648m);
        }

        [Fact]
        public async Task AbandonsRatherThanCreditTheSameEnvelopeTwiceInOneRun()
        {
            // Sans ce garde, la deuxième ligne serait classée « déjà créditée » et le rapport laisserait
            // croire à un run antérieur, alors que le vrai problème est une faute de copie dans la table.
            var envelope = AddEnvelope(originalFund: 8208, availableFund: 0);
            DbContext.SaveChanges();

            var corrections = new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(OrganizationName, SubscriptionName, 1080m),
                new CreditLostBudgetAllowanceRefunds.Correction(OrganizationName, SubscriptionName, 1080m)
            };

            var report = await job.Run(corrections, dryRun: false);

            report.Abandoned.Should().BeTrue();
            report.Corrections.Should().BeEmpty();
            report.TotalCredited.Should().Be(0m);

            (await ReloadAsync(envelope)).AvailableFund.Should().Be(0m);
            DbContext.BudgetAllowanceLogs.Should().BeEmpty();
        }

        [Fact]
        public void KeepsTheReviewedCorrectionsFromTheTicketInSync()
        {
            // La table de CRCL-2678 est la pièce que Récolte a validée. Si quelqu'un l'édite, ces
            // totaux le disent - c'est plus sûr qu'une relecture ligne à ligne.
            var confirmed = CreditLostBudgetAllowanceRefunds.ReviewedCorrections
                .Where(x => !x.RequiresConfirmation).ToList();

            confirmed.Should().HaveCount(8);
            confirmed.Sum(x => x.ExpectedCredit).Should().Be(5076m);
            confirmed.Should().OnlyContain(x => x.SubscriptionName == "FMNCP 2026");

            var awaiting = CreditLostBudgetAllowanceRefunds.ReviewedCorrections
                .Where(x => x.RequiresConfirmation).ToList();

            awaiting.Should().HaveCount(2);
            awaiting.Sum(x => x.ExpectedCredit).Should().Be(3105.99m);

            // Une enveloppe citée deux fois ferait abandonner le run entier (garde anti-double-crédit),
            // donc la table livrée doit être sans répétition sous peine d'être inapplicable.
            CreditLostBudgetAllowanceRefunds.ReviewedCorrections
                .Select(x => (x.OrganizationName, x.SubscriptionName))
                .Should().OnlyHaveUniqueItems();
        }

        private static CreditLostBudgetAllowanceRefunds.Correction[] Corrections(decimal expectedCredit)
        {
            return new[]
            {
                new CreditLostBudgetAllowanceRefunds.Correction(
                    OrganizationName, SubscriptionName, expectedCredit)
            };
        }

        private BudgetAllowance AddEnvelope(decimal originalFund, decimal availableFund)
        {
            var envelope = new BudgetAllowance()
            {
                OriginalFund = originalFund,
                AvailableFund = availableFund,
                Organization = organization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(envelope);
            return envelope;
        }

        private void AddPair(decimal? remainingAllocatedAmount)
        {
            var envelope = DbContext.BudgetAllowances.Local
                .First(x => x.Organization == organization && x.Subscription == subscription);

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType,
                Subscriptions = new List<SubscriptionBeneficiary>()
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                Subscription = subscription,
                BeneficiaryType = beneficiaryType,
                BudgetAllowance = envelope,
                RemainingAllocatedAmount = remainingAllocatedAmount
            });
        }

        /// <summary>
        /// Une enveloppe d'un autre programme, que la table de corrections ne nomme pas : c'est elle
        /// qui tient le rôle de « Trousses 2026-2027 » dans les tests de périmètre.
        /// </summary>
        private BudgetAllowance AddEnvelopeForOtherOrganization(decimal availableFund)
        {
            var otherOrganization = new Organization()
            {
                Name = "Trousses Manger dans le Centre-Sud",
                Project = project
            };
            DbContext.Organizations.Add(otherOrganization);

            var envelope = new BudgetAllowance()
            {
                OriginalFund = 51572.31m,
                AvailableFund = availableFund,
                Organization = otherOrganization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(envelope);
            return envelope;
        }

        private void AddPairOn(BudgetAllowance envelope, decimal? remainingAllocatedAmount)
        {
            var beneficiary = new Beneficiary()
            {
                Firstname = "Franca",
                Lastname = "Carduci",
                Organization = envelope.Organization,
                BeneficiaryType = beneficiaryType,
                Subscriptions = new List<SubscriptionBeneficiary>()
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                Subscription = subscription,
                BeneficiaryType = beneficiaryType,
                BudgetAllowance = envelope,
                RemainingAllocatedAmount = remainingAllocatedAmount
            });
        }

        private void AddDelivery(decimal amount, decimal stillOnCard)
        {
            var card = new Card() { Project = project, Status = CardStatus.Assigned };
            DbContext.Cards.Add(card);

            var beneficiary = new Beneficiary()
            {
                Firstname = "Jane",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType,
                Card = card,
                Subscriptions = new List<SubscriptionBeneficiary>()
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                Card = card,
                Beneficiary = beneficiary,
                OrganizationId = organization.Id,
                SubscriptionType = subscriptionType,
                Amount = amount,
                AvailableFund = stillOnCard,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ProductGroup = productGroup
            });
        }

        private async Task<BudgetAllowance> ReloadAsync(BudgetAllowance envelope)
        {
            var context = CreateDbContext();
            return await context.BudgetAllowances.FindAsync(envelope.Id);
        }
    }
}
