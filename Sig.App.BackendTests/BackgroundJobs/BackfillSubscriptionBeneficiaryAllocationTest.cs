using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class BackfillSubscriptionBeneficiaryAllocationTest : TestBase
    {
        private readonly BackfillSubscriptionBeneficiaryAllocation job;
        private readonly Subscription subscription;
        private readonly Beneficiary beneficiary;
        private readonly BeneficiaryType beneficiaryType;
        private readonly SubscriptionType subscriptionType;
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;
        private readonly BudgetAllowance budgetAllowance;
        private readonly Organization organization;

        public BackfillSubscriptionBeneficiaryAllocationTest()
        {
            var project = new Project() { Name = "Project 1" };
            DbContext.Projects.Add(project);

            organization = new Organization() { Name = "Organization 1", Project = project };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType() { Project = project, Keys = "type1", Name = "Type 1" };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            var productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            subscriptionType = new SubscriptionType()
            {
                Amount = 25,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup
            };

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>() { subscriptionType },
                Project = project
            };

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 100,
                OriginalFund = 200,
                Organization = organization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            // Ligne antérieure à la migration : la réservation est inconnue.
            subscriptionBeneficiary = new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                BeneficiaryType = beneficiaryType,
                Subscription = subscription,
                BudgetAllowance = budgetAllowance,
                RemainingAllocatedAmount = null
            };
            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };

            DbContext.Subscriptions.Add(subscription);
            DbContext.SaveChanges();

            job = new BackfillSubscriptionBeneficiaryAllocation(DbContext, Clock, NullLogger<BackfillSubscriptionBeneficiaryAllocation>.Instance);
        }

        [Fact]
        public async Task DryRunWritesNothing()
        {
            await job.Run(dryRun: true);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().BeNull();
        }

        [Fact]
        public async Task ReconstructsExactlyFromTheLedgerWhenItIsComplete()
        {
            // 3 versements alloués (75), 1 livré (25), rien de relâché : il reste 50 de réservé.
            AddAllocationLog(75m);
            AddDeliveredVersement();

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            // Surtout pas 25, qui serait l'estimation calendaire (1 versement restant x 25).
            local.RemainingAllocatedAmount.Should().Be(50m);
        }

        [Fact]
        public async Task FallsBackToTheCalendarEstimateWhenNoAllocationLogExists()
        {
            // Aucun log d'allocation : c'est le cas de la quasi-totalité des lignes, le discriminant 12
            // n'existant que depuis CRCL-2577. On gèle le nombre calculé par le code actuel.
            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(25m);
        }

        [Fact]
        public async Task FallsBackToTheCalendarEstimateWhenAnUnloggedMovementIsDetected()
        {
            // Le log d'allocation existe, mais un changement de max a débité l'enveloppe sans le
            // journaliser : la reconstruction sous-estimerait, donc on ne l'utilise pas.
            AddAllocationLog(75m);
            AddDeliveredVersement();
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 5;

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(25m);
        }

        [Fact]
        public async Task FallsBackToTheCalendarEstimateAfterAPreviousRemoval()
        {
            // Un retrait passé (discriminant 8) signifie que la paire a été réinscrite : les logs
            // d'allocation des deux épisodes s'additionneraient.
            AddAllocationLog(75m);
            DbContext.TransactionLogs.Add(BuildLog(
                TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog, 25m));

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(25m);
        }

        [Fact]
        public async Task SubtractsReleasedReservationsButNotDeliveredMoneyReturns()
        {
            // Discriminant 7 = réservation relâchée, jamais livrée : à soustraire.
            // Discriminant 9 = carte désassignée, argent DÉJÀ livré qui revient : à NE PAS soustraire,
            // sinon on compte la livraison deux fois.
            AddAllocationLog(75m);
            DbContext.TransactionLogs.Add(BuildLog(
                TransactionLogDiscriminator.RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog, 25m));
            DbContext.TransactionLogs.Add(BuildLog(
                TransactionLogDiscriminator.RefundBudgetAllowanceFromUnassignedCardTransactionLog, 25m));

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            // 75 - 0 livré - 25 relâché = 50. Soustraire aussi le discriminant 9 donnerait 25.
            local.RemainingAllocatedAmount.Should().Be(50m);
        }

        [Fact]
        public async Task NeverWritesANegativeReservation()
        {
            // Plus livré qu'alloué : la réservation est plancher à 0, jamais négative.
            AddAllocationLog(25m);
            AddDeliveredVersement();
            AddDeliveredVersement();
            AddDeliveredVersement();

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(0m);
        }

        [Fact]
        public async Task LeavesAlreadyReconstructedRowsUntouched()
        {
            subscriptionBeneficiary.RemainingAllocatedAmount = 999m;
            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(999m);
        }

        [Fact]
        public async Task AbortsWithoutWritingWhenAUsageBasedSubscriptionHasNoMaxNumberOfPayments()
        {
            // GetEffectiveMaxNumberOfPayments et GetTotalPayment sont mutuellement récursifs et ne
            // terminent que grâce à cet invariant. Le job doit refuser de tourner plutôt que de faire
            // sauter la pile.
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = null;
            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().BeNull();
        }

        [Fact]
        public async Task SubtractsDeliveredVersementsFromTheQuotaInTheCalendarEstimate()
        {
            // Le cas du retour client : max 2 versements, 1 déjà livré, 4 dates encore au calendrier.
            // Le max est un quota sur la vie de l'abonnement, donc il ne reste qu'un versement à
            // livrer - surtout pas 2, qui re-réserverait celui de juillet.
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4);
            AddDeliveredVersement();

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(25m);
        }

        [Fact]
        public async Task ReservesNothingWhenTheQuotaIsAlreadyExhausted()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4);
            AddDeliveredVersement();
            AddDeliveredVersement();

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(0m);
        }

        [Fact]
        public async Task ReservesNothingOnAnEndedSubscription()
        {
            // GetCardPaymentRemaining compte les mois écoulés depuis la fin et renvoie donc un négatif
            // sur un abonnement terminé. La réservation doit valoir 0, pas un montant négatif qu'un
            // garde en aval viendrait rattraper.
            subscription.StartDate = new DateTime(2024, 1, 1);
            subscription.EndDate = new DateTime(2024, 8, 3);

            DbContext.SaveChanges();

            await job.Run(dryRun: false);

            var local = await DbContext.SubscriptionBeneficiaries.FirstAsync();
            local.RemainingAllocatedAmount.Should().Be(0m);
        }

        private void AddAllocationLog(decimal amount)
        {
            DbContext.TransactionLogs.Add(BuildLog(
                TransactionLogDiscriminator.AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog, amount));
        }

        private TransactionLog BuildLog(TransactionLogDiscriminator discriminator, decimal amount)
        {
            return new TransactionLog()
            {
                Discriminator = discriminator,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                TotalAmount = amount,
                BeneficiaryId = beneficiary.Id,
                BeneficiaryTypeId = beneficiaryType.Id,
                OrganizationId = organization.Id,
                SubscriptionId = subscription.Id
            };
        }

        private void AddDeliveredVersement()
        {
            DbContext.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                Amount = subscriptionType.Amount,
                AvailableFund = subscriptionType.Amount,
                Beneficiary = beneficiary,
                OrganizationId = organization.Id,
                SubscriptionType = subscriptionType,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc()
            });
        }
    }
}
