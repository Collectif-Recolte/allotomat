using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public class RepairEndedSubscriptionReservationsTest : TestBase
    {
        private readonly RepairEndedSubscriptionReservations job;
        private readonly Project project;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;
        private readonly Beneficiary beneficiary;
        private readonly Card card;
        private readonly Subscription subscription;
        private readonly SubscriptionType subscriptionType;
        private readonly BudgetAllowance budgetAllowance;
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;

        public RepairEndedSubscriptionReservationsTest()
        {
            project = new Project() { Name = "Project 1" };
            DbContext.Projects.Add(project);

            organization = new Organization() { Name = "Organization 1", Project = project };
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

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            card = new Card()
            {
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Funds = new List<Fund>(),
                Transactions = new List<Transaction>()
            };
            card.Funds.Add(new Fund() { Amount = 0, ProductGroup = productGroup, Card = card });
            DbContext.Cards.Add(card);
            beneficiary.Card = card;

            subscriptionType = new SubscriptionType()
            {
                Amount = 50,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup
            };

            // Le calendrier est terminé : le clock des tests est au 2 juillet 2025, l'abonnement s'est
            // arrêté la veille. Les fonds, eux, ne meurent qu'en décembre - c'est la situation de
            // CRCL-2676, où l'argent réservé est encore récupérable.
            subscription = new Subscription()
            {
                Name = "2 - Short Markets 2025",
                Project = project,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 7, 1),
                FundsExpirationDate = new DateTime(2025, 12, 21),
                IsFundsAccumulable = true,
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>() { subscriptionType }
            };
            DbContext.Subscriptions.Add(subscription);

            budgetAllowance = new BudgetAllowance()
            {
                OriginalFund = 500,
                AvailableFund = 0,
                Organization = organization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            // Le versement du dernier jour n'a jamais été livré : il est resté réservé dans l'enveloppe.
            subscriptionBeneficiary = new SubscriptionBeneficiary()
            {
                Subscription = subscription,
                Beneficiary = beneficiary,
                BeneficiaryType = beneficiaryType,
                BudgetAllowance = budgetAllowance,
                RemainingAllocatedAmount = 50
            };
            DbContext.SubscriptionBeneficiaries.Add(subscriptionBeneficiary);

            DbContext.SaveChanges();

            job = new RepairEndedSubscriptionReservations(DbContext, Clock,
                Logger<RepairEndedSubscriptionReservations>(), Logger<AddingFundToCard>());
        }

        [Fact]
        public async Task DryRunReportsTheOrphanReservationWithoutWritingAnything()
        {
            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: true);

            report.Delivered.Should().HaveCount(1);
            report.TotalDelivered.Should().Be(50);

            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
            budgetAllowance.AvailableFund.Should().Be(0);
            CardFund().Should().Be(0);
            (await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task DeliversTheMissedPaymentToTheCardWithoutDebitingTheEnvelopeAgain()
        {
            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            CardFund().Should().Be(50);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);

            // L'argent est sorti de l'enveloppe à l'assignation. Le rattrapage consomme la réservation,
            // il ne redébite pas.
            budgetAllowance.AvailableFund.Should().Be(0);

            var transactions = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().ToListAsync();
            transactions.Should().HaveCount(1);
            transactions[0].Amount.Should().Be(50);
            transactions[0].ExpirationDate.Should().Be(new DateTime(2025, 12, 21));

            var logs = await DbContext.TransactionLogs
                .Where(x => x.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog)
                .ToListAsync();
            logs.Should().HaveCount(1);
            logs[0].TotalAmount.Should().Be(50);
            logs[0].SubscriptionId.Should().Be(subscription.Id);
        }

        [Fact]
        public async Task DeliversEveryMissedPaymentWhenTheReservationCoversSeveralCycles()
        {
            subscriptionBeneficiary.RemainingAllocatedAmount = 150;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Delivered.Single().Cycles.Should().Be(3);
            CardFund().Should().Be(150);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);
            (await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync()).Should().Be(3);

            // Les trois versements s'accumulent sur le même solde de carte : un solde dupliqué par
            // groupe de produits fausserait tout le reste de l'application.
            (await DbContext.Funds.CountAsync(x => x.CardId == card.Id)).Should().Be(1);
        }

        /// <summary>
        /// Le rattrapage sans carte passe par AddingFundToCard, qui retrouve l'enveloppe par le groupe
        /// courant du participant. Quand ce groupe n'est plus celui d'où l'argent est sorti, créditer
        /// serait créditer la mauvaise enveloppe : on écarte plutôt que de deviner.
        /// </summary>
        [Fact]
        public async Task SkipsACardlessParticipantWhoseOrganizationNoLongerMatchesTheReservedEnvelope()
        {
            var otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            DbContext.Organizations.Add(otherOrganization);
            DbContext.BudgetAllowances.Add(new BudgetAllowance()
            {
                OriginalFund = 500,
                AvailableFund = 0,
                Organization = otherOrganization,
                Subscription = subscription
            });

            beneficiary.Card = null;
            card.Beneficiary = null;
            beneficiary.Organization = otherOrganization;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Skipped.Should().HaveCount(1);
            report.Skipped.Single().Reason.Should().Contain("Release");
            budgetAllowance.AvailableFund.Should().Be(0);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
        }

        /// <summary>
        /// Le mode Release ne dépend pas du groupe courant : il rend l'argent à l'enveloppe qui a
        /// effectivement été débitée.
        /// </summary>
        [Fact]
        public async Task ReleasesToTheReservedEnvelopeEvenWhenTheParticipantChangedOrganization()
        {
            var otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            DbContext.Organizations.Add(otherOrganization);
            var otherBudgetAllowance = new BudgetAllowance()
            {
                OriginalFund = 500,
                AvailableFund = 0,
                Organization = otherOrganization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(otherBudgetAllowance);

            beneficiary.Organization = otherOrganization;
            await DbContext.SaveChangesAsync();

            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            budgetAllowance.AvailableFund.Should().Be(50);
            otherBudgetAllowance.AvailableFund.Should().Be(0);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);
        }

        [Fact]
        public async Task ReleasesTheReservationBackToTheEnvelope()
        {
            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            report.Released.Should().HaveCount(1);
            report.TotalReleased.Should().Be(50);

            budgetAllowance.AvailableFund.Should().Be(50);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);
            CardFund().Should().Be(0);
            (await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync()).Should().Be(0);
        }

        /// <summary>
        /// Le run entier tient dans un seul SaveChanges, et <c>AvailableFund</c> est un jeton de
        /// concurrence : un mouvement d'enveloppe ordinaire (retrait, ajustement, versement) qui se
        /// glisse entre le chargement des candidats et l'écriture finale ferait échouer tout le run
        /// avec un <c>DbUpdateConcurrencyException</c>. Le crédit doit se rebaser sur le solde réel,
        /// par le même chemin que toutes les autres mutations d'enveloppe (CRCL-2677).
        /// </summary>
        [Fact]
        public async Task ReleaseSurvivesAnEnvelopeMovementMadeWhileTheJobWasRunning()
        {
            // Le DbContext du job a déjà sa photographie de l'enveloppe (AvailableFund = 0). Un autre
            // écrivain la bouge entre-temps : la base dit 100, le job croit encore 0.
            await using (var other = CreateDbContext())
            {
                var envelope = await other.BudgetAllowances.SingleAsync(x => x.Id == budgetAllowance.Id);
                envelope.AvailableFund += 100;
                await other.SaveChangesAsync();
            }

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            report.Released.Should().HaveCount(1);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);

            // 100 du mouvement concurrent + 50 relâchés : aucun des deux n'a écrasé l'autre.
            await using var fresh = CreateDbContext();
            (await fresh.BudgetAllowances.SingleAsync(x => x.Id == budgetAllowance.Id)).AvailableFund.Should().Be(150);
        }

        [Fact]
        public async Task LogsTheReleaseSoTheReportsStayConsistent()
        {
            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            var log = await DbContext.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .SingleAsync(x => x.Discriminator == TransactionLogDiscriminator.ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog);

            log.TotalAmount.Should().Be(50);
            log.BeneficiaryId.Should().Be(beneficiary.Id);
            log.SubscriptionId.Should().Be(subscription.Id);
            log.OrganizationId.Should().Be(organization.Id);
            log.ProjectId.Should().Be(project.Id);
            log.TransactionLogProductGroups.Sum(x => x.Amount).Should().Be(50);
        }

        [Fact]
        public async Task SplitsTheReleasedAmountAcrossEveryProductGroupOfThePayment()
        {
            var secondProductGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2
            };
            DbContext.ProductGroups.Add(secondProductGroup);
            subscription.Types.Add(new SubscriptionType()
            {
                Amount = 25,
                BeneficiaryType = beneficiaryType,
                ProductGroup = secondProductGroup
            });
            subscriptionBeneficiary.RemainingAllocatedAmount = 75;
            await DbContext.SaveChangesAsync();

            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            var log = await DbContext.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .SingleAsync(x => x.Discriminator == TransactionLogDiscriminator.ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog);

            log.TransactionLogProductGroups.Should().HaveCount(2);
            log.TransactionLogProductGroups.Sum(x => x.Amount).Should().Be(75);
            log.TransactionLogProductGroups.Single(x => x.ProductGroupId == productGroup.Id).Amount.Should().Be(50);
            log.TransactionLogProductGroups.Single(x => x.ProductGroupId == secondProductGroup.Id).Amount.Should().Be(25);
        }

        [Fact]
        public async Task LeavesAStillRunningSubscriptionAlone()
        {
            subscription.EndDate = new DateTime(2025, 9, 1);
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Pairs.Should().BeEmpty();
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
            CardFund().Should().Be(0);
        }

        /// <summary>
        /// Le dernier jour de l'abonnement, le job de versement doit encore livrer. Comparer des
        /// timestamps au lieu de dates déclarerait l'abonnement terminé dès minuit et volerait ce
        /// versement au participant - le piège même de CRCL-2675.
        /// </summary>
        [Fact]
        public async Task LeavesASubscriptionEndingTodayAlone()
        {
            subscription.EndDate = Clock.GetCurrentInstant().ToDateTimeUtc().Date;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Pairs.Should().BeEmpty();
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
        }

        [Fact]
        public async Task LeavesPairsWithNothingReservedAlone()
        {
            subscriptionBeneficiary.RemainingAllocatedAmount = 0;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Pairs.Should().BeEmpty();
            CardFund().Should().Be(0);
        }

        /// <summary>
        /// Une réservation inconnue n'est pas zéro : la réparer demanderait d'inventer un montant. Elle
        /// est comptée à part pour que le rapport dise que son total est un minorant.
        /// </summary>
        [Fact]
        public async Task CountsPairsWhoseReservationIsStillUnknownWithoutTouchingThem()
        {
            subscriptionBeneficiary.RemainingAllocatedAmount = null;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Pairs.Should().BeEmpty();
            report.UnknownReservationPairCount.Should().Be(1);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().BeNull();
            CardFund().Should().Be(0);
        }

        [Fact]
        public async Task SkipsWhenTheReservationIsNotAWholeNumberOfPayments()
        {
            subscriptionBeneficiary.RemainingAllocatedAmount = 75;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Skipped.Should().HaveCount(1);
            report.Skipped.Single().Reason.Should().Contain("non multiple");
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(75);
            CardFund().Should().Be(0);
            budgetAllowance.AvailableFund.Should().Be(0);
        }

        [Fact]
        public async Task SkipsWhenTheFundsWouldBeDeliveredAlreadyExpired()
        {
            subscription.FundsExpirationDate = new DateTime(2025, 6, 1);
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Skipped.Should().HaveCount(1);
            report.Skipped.Single().Reason.Should().Contain("expirés");
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
            CardFund().Should().Be(0);
        }

        /// <summary>
        /// Sans carte, il n'y a rien à créditer : le job régulier aurait rendu l'argent à l'enveloppe.
        /// Le mode Deliver fait pareil plutôt que d'immobiliser l'argent une deuxième fois.
        /// </summary>
        [Fact]
        public async Task ReleasesToTheEnvelopeWhenTheParticipantHasNoCard()
        {
            beneficiary.Card = null;
            card.Beneficiary = null;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Released.Should().HaveCount(1);
            report.Released.Single().Reason.Should().Contain("sans carte");
            budgetAllowance.AvailableFund.Should().Be(50);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);
            (await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync()).Should().Be(0);
        }

        /// <summary>
        /// TransactionLog n'a pas de colonne d'enveloppe : OrganizationId est le seul lien. Il doit donc
        /// nommer le groupe dont l'enveloppe a réellement été créditée, pas le groupe courant du
        /// participant - sinon le remboursement apparaît dans le rapport d'un groupe dont l'enveloppe
        /// n'a pas bougé.
        /// </summary>
        [Fact]
        public async Task LogsTheReleaseAgainstTheCreditedEnvelopesOrganization()
        {
            var otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            DbContext.Organizations.Add(otherOrganization);
            DbContext.BudgetAllowances.Add(new BudgetAllowance()
            {
                OriginalFund = 500,
                AvailableFund = 0,
                Organization = otherOrganization,
                Subscription = subscription
            });

            beneficiary.Organization = otherOrganization;
            await DbContext.SaveChangesAsync();

            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            var log = await DbContext.TransactionLogs
                .SingleAsync(x => x.Discriminator == TransactionLogDiscriminator.ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog);

            log.OrganizationId.Should().Be(organization.Id);
            log.OrganizationName.Should().Be("Organization 1");
            log.BeneficiaryId.Should().Be(beneficiary.Id);
        }

        [Fact]
        public async Task NamesTheEnvelopeAfterItsOwningOrganizationInTheReport()
        {
            var otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            DbContext.Organizations.Add(otherOrganization);
            beneficiary.Organization = otherOrganization;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: true);

            report.Envelopes.Single().OrganizationName.Should().Be("Organization 1");
            report.Pairs.Single().BeneficiaryOrganizationName.Should().Be("Organization 2");
        }

        /// <summary>
        /// GetEffectiveMaxNumberOfPayments et GetTotalPayment sont mutuellement récursifs sans cas
        /// terminal pour cette forme de données. Le chemin sans carte y passe : il doit écarter la paire
        /// plutôt que de provoquer une StackOverflowException, qui tuerait le worker et emporterait tout
        /// le lot.
        /// </summary>
        [Fact]
        public async Task SkipsACardlessParticipantOnAUsageBasedSubscriptionWithoutAPaymentCap()
        {
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = null;
            beneficiary.Card = null;
            card.Beneficiary = null;
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Deliver, dryRun: false);

            report.Skipped.Should().HaveCount(1);
            report.Skipped.Single().Reason.Should().Contain("usage-based");
            budgetAllowance.AvailableFund.Should().Be(0);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(50);
        }

        /// <summary>
        /// Le mode Release est purement arithmétique : il n'appelle aucun helper de calendrier, donc la
        /// même donnée piégeuse ne le met pas en danger et l'argent revient bien à l'enveloppe.
        /// </summary>
        [Fact]
        public async Task ReleasesOnAUsageBasedSubscriptionWithoutAPaymentCap()
        {
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = null;
            beneficiary.Card = null;
            card.Beneficiary = null;
            await DbContext.SaveChangesAsync();

            await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            budgetAllowance.AvailableFund.Should().Be(50);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);
        }

        /// <summary>
        /// Sans type de versement le journal ne peut pas ventiler par groupe de produits. L'argent revient
        /// quand même - l'immobiliser serait pire - mais la paire est marquée pour que l'écart dans le
        /// rapport soit visible.
        /// </summary>
        [Fact]
        public async Task FlagsAReleaseThatCannotBeBrokenDownByProductGroup()
        {
            subscription.Types.Clear();
            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: false);

            report.Released.Single().Reason.Should().Contain("sans ventilation");
            budgetAllowance.AvailableFund.Should().Be(50);
            subscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0);

            var log = await DbContext.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .SingleAsync(x => x.Discriminator == TransactionLogDiscriminator.ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog);
            log.TotalAmount.Should().Be(50);
            log.TransactionLogProductGroups.Should().BeEmpty();
        }

        [Fact]
        public async Task GroupsTheReportByEnvelope()
        {
            var otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            DbContext.Organizations.Add(otherOrganization);

            var otherBeneficiary = new Beneficiary()
            {
                Firstname = "Jane",
                Lastname = "Roe",
                BeneficiaryType = beneficiaryType,
                Organization = otherOrganization
            };
            DbContext.Beneficiaries.Add(otherBeneficiary);

            var otherBudgetAllowance = new BudgetAllowance()
            {
                OriginalFund = 500,
                AvailableFund = 0,
                Organization = otherOrganization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(otherBudgetAllowance);

            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary()
            {
                Subscription = subscription,
                Beneficiary = otherBeneficiary,
                BeneficiaryType = beneficiaryType,
                BudgetAllowance = otherBudgetAllowance,
                RemainingAllocatedAmount = 100
            });

            await DbContext.SaveChangesAsync();

            var report = await job.Run(RepairEndedSubscriptionReservations.RepairMode.Release, dryRun: true);

            report.TotalReserved.Should().Be(150);
            report.Envelopes.Should().HaveCount(2);
            report.Envelopes.First().BudgetAllowanceId.Should().Be(otherBudgetAllowance.Id);
            report.Envelopes.First().Reserved.Should().Be(100);
            report.Envelopes.First().ToRelease.Should().Be(100);
            report.Envelopes.Last().Reserved.Should().Be(50);
        }

        private decimal CardFund() =>
            DbContext.Funds.Where(x => x.CardId == card.Id).Sum(x => x.Amount);
    }
}
