using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.BudgetAllowances;
using Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.BudgetAllowances
{
    public class EditBudgetAllowanceTest : TestBase
    {
        private readonly EditBudgetAllowance handler;
        private readonly BudgetAllowance budgetAllowance;
        public EditBudgetAllowanceTest()
        {
            var user = AddUser("test@example.com", UserType.ProjectManager, password: "Abcd1234!!");
            SetLoggedInUser(user);

            user.Profile = new UserProfile()
            {
                FirstName = "Test",
                LastName = "Example",
                User = user,
                UpdateTimeUtc = DateTime.UtcNow
            };

            var budgetAllowanceLogFactory = new BudgetAllowanceLogFactory(Clock, HttpContextAccessor, DbContext);

            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25
                    },
                    new SubscriptionType()
                    {
                        Amount = 50
                    },
                    new SubscriptionType()
                    {
                        Amount = 100
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month, 1)
            };
            DbContext.Subscriptions.Add(subscription);

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 20,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            DbContext.SaveChanges();

            handler = new EditBudgetAllowance(NullLogger<EditBudgetAllowance>.Instance, DbContext, budgetAllowanceLogFactory);
        }

        [Fact]
        public async Task EditBudgetAllowance()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 10
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            
            localBudgetAllowance.AvailableFund.Should().Be(5);
            localBudgetAllowance.OriginalFund.Should().Be(10);
        }

        [Fact]
        public async Task ThrowsIfBudgetAllowanceNotFound()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = Id.New<BudgetAllowance>(123456),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBudgetAllowance.BudgetAllowanceNotFoundException>();
        }

        // CRCL-2669 - Ces deux tests fixent ce que devient une réduction d'enveloppe sous écriture
        // concurrente, parce que la question a été posée en revue (PR 5480) : le garde
        // « AvailableFund < budgetDifference » n'est pas réévalué après le rebasage, donc la
        // réduction pourrait-elle passer alors qu'elle aurait dû être refusée ?
        //
        // Non, et pour une raison qui tient aux deux montants : le joint rebase AvailableFund ET
        // OriginalFund du MÊME delta, et ce delta (budgetDifference) ne dépend que d'OriginalFund et
        // du montant demandé - deux valeurs qu'un mouvement d'enveloppe concurrent ne touche pas. Un
        // crédit concurrent donne donc exactement l'état qu'aurait produit une lecture fraîche, et un
        // débit concurrent qui rendrait la réduction impossible est refusé par le joint lui-même,
        // AvailableFund étant sa propriété refusable.
        [Fact]
        public async Task ReducingAnEnvelopeWhileACreditLands_GivesTheSameStateAsIfBothHadBeenSeen()
        {
            // 25 confiés, 20 disponibles, donc 5 engagés. Un remboursement de 3 arrive pendant que
            // l'admin réduit l'enveloppe à 10, sur sa lecture à 20.
            using (var concurrent = CreateDbContext())
            {
                var concurrentEnvelope = await concurrent.BudgetAllowances.FindAsync(budgetAllowance.Id);
                concurrentEnvelope.AvailableFund += 3;
                await concurrent.SaveChangesAsync();
            }

            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 10
            };

            await handler.Handle(input, CancellationToken.None);

            var verify = CreateDbContext();
            var persisted = await verify.BudgetAllowances.AsNoTracking()
                .Where(x => x.Id == budgetAllowance.Id)
                .Select(x => new { x.AvailableFund, x.OriginalFund })
                .SingleAsync();

            // Sur données fraîches : 23 disponibles, réduction de 15, donc 8 - le même résultat.
            // Le remboursement concurrent est conservé au lieu d'être écrasé.
            persisted.OriginalFund.Should().Be(10);
            persisted.AvailableFund.Should().Be(8);

            // Et l'engagement audité par VerifyBudgetAllowanceReservations reste positif.
            persisted.AvailableFund.Should().BeLessThanOrEqualTo(persisted.OriginalFund);
        }

        [Fact]
        public async Task ReducingAnEnvelopeWhoseFundsWereReservedConcurrently_IsRefusedRatherThanOverdrawn()
        {
            // Le sens qui compte vraiment : un débit concurrent emporte de quoi rendre la réduction
            // impossible. Le garde initial l'avait autorisée sur 20 disponibles ; il n'en reste que 2.
            using (var concurrent = CreateDbContext())
            {
                var concurrentEnvelope = await concurrent.BudgetAllowances.FindAsync(budgetAllowance.Id);
                concurrentEnvelope.AvailableFund -= 18;
                await concurrent.SaveChangesAsync();
            }

            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<BudgetAllowanceInsufficientFundException>();

            // Refusé avant toute mutation : l'enveloppe est exactement dans l'état où le mouvement
            // concurrent l'a laissée.
            var verify = CreateDbContext();
            var persisted = await verify.BudgetAllowances.AsNoTracking()
                .Where(x => x.Id == budgetAllowance.Id)
                .Select(x => new { x.AvailableFund, x.OriginalFund })
                .SingleAsync();

            persisted.AvailableFund.Should().Be(2);
            persisted.OriginalFund.Should().Be(25);
        }

        [Fact]
        public async Task ThrowsIfAvailableBudgetOverNewBudget()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 4
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBudgetAllowance.AvailableBudgetOverNewBudgetException>();
        }
    }
}
