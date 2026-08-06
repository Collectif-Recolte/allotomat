using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class VerifyBudgetAllowanceReservationsTest : TestBase
    {
        private readonly VerifyBudgetAllowanceReservations job;
        private readonly Project project;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Subscription subscription;

        public VerifyBudgetAllowanceReservationsTest()
        {
            project = new Project() { Name = "Project 1" };
            DbContext.Projects.Add(project);

            organization = new Organization() { Name = "Organization 1", Project = project };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType() { Project = project, Keys = "type1", Name = "Type 1" };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            var productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType() { Amount = 25, BeneficiaryType = beneficiaryType, ProductGroup = productGroup }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            job = new VerifyBudgetAllowanceReservations(DbContext, NullLogger<VerifyBudgetAllowanceReservations>.Instance);
        }

        [Fact]
        public async Task FlagsAnEnvelopeReservingMoreThanWhatEverLeftIt()
        {
            // 100 d'origine, 40 encore disponibles : 60 sont sortis de l'enveloppe. Deux paires qui
            // réservent 50 chacune en revendiquent 100, soit 40 de plus qu'il n'en est jamais sorti.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(budgetAllowance, 50m);
            AddPair(budgetAllowance, 50m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().HaveCount(1);
            report.OverReservedEnvelopes[0].BudgetAllowanceId.Should().Be(budgetAllowance.Id);
            report.OverReservedEnvelopes[0].Committed.Should().Be(60m);
            report.OverReservedEnvelopes[0].Reserved.Should().Be(100m);
            report.OverReservedEnvelopes[0].Overshoot.Should().Be(40m);
            report.TotalOvershoot.Should().Be(40m);
        }

        [Fact]
        public async Task AcceptsAnEnvelopeReservingExactlyWhatLeftIt()
        {
            // Le cas limite : tout ce qui est sorti est encore réservé, rien n'a été livré.
            // L'invariant est une inégalité large, donc l'égalité passe.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(budgetAllowance, 60m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().BeEmpty();
            report.Envelopes.Single().Overshoot.Should().Be(0m);
        }

        [Fact]
        public async Task AcceptsAnEnvelopeWhosePairsHaveAlreadyBeenDelivered()
        {
            // 60 sortis, dont 45 déjà livrés sur les cartes : il ne reste que 15 de réservé.
            // C'est le cas normal, et de loin le plus fréquent.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(budgetAllowance, 15m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().BeEmpty();
            report.Envelopes.Single().Overshoot.Should().Be(-45m);
        }

        [Fact]
        public async Task ExcludesUnknownReservationsFromTheSumButCountsThem()
        {
            // Une paire à null n'est pas comptée : le dépassement devient un minorant, jamais un faux
            // positif. Le compteur est là pour que le lecteur sache que le contrôle est partiel.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(budgetAllowance, 50m);
            AddPair(budgetAllowance, null);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().BeEmpty();
            report.Envelopes.Single().Reserved.Should().Be(50m);
            report.Envelopes.Single().PairCount.Should().Be(2);
            report.UnknownPairCount.Should().Be(1);
        }

        [Fact]
        public async Task CountsNegativeReservations()
        {
            // Réservation négative = plus livré que réservé. Ça n'est jamais censé arriver et ça mérite
            // d'être remonté même quand l'enveloppe, elle, reste dans les clous.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(budgetAllowance, -10m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.NegativePairCount.Should().Be(1);
            report.OverReservedEnvelopes.Should().BeEmpty();
        }

        [Fact]
        public async Task HandlesAnEnvelopeWithoutAnyPair()
        {
            AddBudgetAllowance(originalFund: 100, availableFund: 100);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.Envelopes.Should().HaveCount(1);
            report.Envelopes.Single().Reserved.Should().Be(0m);
            report.Envelopes.Single().PairCount.Should().Be(0);
            report.OverReservedEnvelopes.Should().BeEmpty();
        }

        [Fact]
        public async Task ReportsEachEnvelopeIndependentlyAndRanksTheWorstFirst()
        {
            // Le rapport doit isoler l'enveloppe fautive sans contaminer les autres, et présenter le
            // plus gros écart en tête : c'est par là qu'on commence une reprise de données.
            var healthy = AddBudgetAllowance(originalFund: 100, availableFund: 40);
            AddPair(healthy, 60m);

            var slightlyOff = AddBudgetAllowance(originalFund: 100, availableFund: 90);
            AddPair(slightlyOff, 15m);

            var badlyOff = AddBudgetAllowance(originalFund: 100, availableFund: 90);
            AddPair(badlyOff, 80m);

            DbContext.SaveChanges();

            var report = await job.Run();

            report.Envelopes.Should().HaveCount(3);
            report.OverReservedEnvelopes.Select(x => x.BudgetAllowanceId)
                .Should().Equal(badlyOff.Id, slightlyOff.Id);
            report.OverReservedEnvelopes[0].Overshoot.Should().Be(70m);
            report.OverReservedEnvelopes[1].Overshoot.Should().Be(5m);
            report.TotalOvershoot.Should().Be(75m);
        }

        [Fact]
        public async Task DoesNotCallAnEmptyEnvelopeOverReservedWhenItHoldsMoreThanItsOriginalFund()
        {
            // Cas relevé en production : disponible 10 050 pour un budget d'origine de 10 000, aucune
            // paire. L'écart arithmétique est positif, mais rien n'est réservé - ce n'est pas une
            // sur-réservation, c'est une enveloppe créditée de plus que ce qui en est sorti.
            AddBudgetAllowance(originalFund: 10000, availableFund: 10050);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().BeEmpty();
            report.TotalOvershoot.Should().Be(0m);
            report.NegativeCommittedEnvelopes.Should().HaveCount(1);
            report.NegativeCommittedEnvelopes.Single().Committed.Should().Be(-50m);
        }

        [Fact]
        public async Task StillFlagsARealOverReservationOnAnEnvelopeHoldingMoreThanItsOriginalFund()
        {
            // Les deux anomalies peuvent coexister : la paire réserve 30 alors que rien n'est sorti.
            var budgetAllowance = AddBudgetAllowance(originalFund: 100, availableFund: 150);
            AddPair(budgetAllowance, 30m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.OverReservedEnvelopes.Should().HaveCount(1);
            report.OverReservedEnvelopes.Single().Overshoot.Should().Be(80m);
            report.NegativeCommittedEnvelopes.Should().HaveCount(1);
        }

        [Fact]
        public async Task ReportsAnEnvelopeInDeficit()
        {
            // Cas relevé sur la copie de production (enveloppe 47) : disponible négatif, donc il en est
            // sorti plus qu'elle n'en contenait. Committed vaut alors PLUS que OriginalFund, ce qui
            // échappe aux deux autres contrôles - d'où un signal dédié.
            var budgetAllowance = AddBudgetAllowance(originalFund: 22676, availableFund: -299.31m);
            AddPair(budgetAllowance, 0m);
            DbContext.SaveChanges();

            var report = await job.Run();

            report.NegativeAvailableFundEnvelopes.Should().HaveCount(1);
            report.TotalDeficit.Should().Be(-299.31m);
            report.Envelopes.Single().Committed.Should().Be(22975.31m);

            // Les deux autres contrôles restent muets sur ce cas.
            report.OverReservedEnvelopes.Should().BeEmpty();
            report.NegativeCommittedEnvelopes.Should().BeEmpty();
        }

        private BudgetAllowance AddBudgetAllowance(decimal originalFund, decimal availableFund)
        {
            var budgetAllowance = new BudgetAllowance()
            {
                OriginalFund = originalFund,
                AvailableFund = availableFund,
                Organization = organization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);
            return budgetAllowance;
        }

        private void AddPair(BudgetAllowance budgetAllowance, decimal? remainingAllocatedAmount)
        {
            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                BeneficiaryType = beneficiaryType,
                Subscription = subscription,
                BudgetAllowance = budgetAllowance,
                RemainingAllocatedAmount = remainingAllocatedAmount
            });
        }
    }
}
