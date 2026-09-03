using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Extensions
{
    // ------------------------------------------------------------------------------------------
    // CRCL-2677 - Le contrat du joint partagé par les quatorze sites qui déplacent des fonds
    // d'enveloppe. `ConcurrentBudgetAllowanceRefundTest` prouve que le retrait en rafale ne perd plus
    // de crédit ; ces tests-ci vérifient les règles du joint lui-même, y compris celles qu'aucun
    // handler ne déclenche assez souvent pour qu'un test de bout en bout les couvre.
    // ------------------------------------------------------------------------------------------
    public class BudgetAllowanceConcurrencyExtensionsTest : TestBase
    {
        private readonly BudgetAllowance budgetAllowance;

        public BudgetAllowanceConcurrencyExtensionsTest()
        {
            var project = new Project { Name = "Project 1" };
            var organization = new Organization { Name = "Organization 1", Project = project };

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription = new Subscription
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>(),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1)
            };

            budgetAllowance = new BudgetAllowance
            {
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 1000m,
                AvailableFund = 1000m
            };

            DbContext.Projects.Add(project);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.BudgetAllowances.Add(budgetAllowance);
            DbContext.SaveChanges();
        }

        /// <summary>
        /// Charge l'enveloppe dans un contexte neuf et la garde suivie : la valeur lue est figée à cet
        /// instant, comme celle qu'un handler garde en mémoire pendant qu'il travaille.
        /// </summary>
        private async Task<(AppDbContext Context, BudgetAllowance Envelope)> ReadEnvelopeAsync()
        {
            var context = CreateDbContext();
            var envelope = await context.BudgetAllowances.FirstAsync(x => x.Id == budgetAllowance.Id);
            return (context, envelope);
        }

        private async Task<decimal> PersistedFundAsync()
        {
            var verify = CreateDbContext();
            return await verify.BudgetAllowances.AsNoTracking()
                .Where(x => x.Id == budgetAllowance.Id).Select(x => x.AvailableFund).SingleAsync();
        }

        [Fact]
        public async Task TwoCreditsReadingTheSameBalance_BothLand()
        {
            var (contextA, envelopeA) = await ReadEnvelopeAsync();
            var (contextB, envelopeB) = await ReadEnvelopeAsync();

            envelopeA.AvailableFund += 216m;
            await contextA.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            // B a lu 1000 avant l'écriture de A. Sans rebase, il écrirait 1216 et effacerait le crédit
            // de A ; avec rebase, il ajoute son propre 216 à ce qui est réellement en base.
            envelopeB.AvailableFund += 216m;
            await contextB.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            (await PersistedFundAsync()).Should().Be(1432m);
        }

        [Fact]
        public async Task CreditAndDebitReadingTheSameBalance_BothLand()
        {
            var (contextA, envelopeA) = await ReadEnvelopeAsync();
            var (contextB, envelopeB) = await ReadEnvelopeAsync();

            envelopeA.AvailableFund += 216m;
            await contextA.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            envelopeB.AvailableFund -= 300m;
            await contextB.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            // Le rebase porte sur le mouvement, pas sur le total : 1000 + 216 - 300.
            (await PersistedFundAsync()).Should().Be(916m);
        }

        [Fact]
        public async Task DebitWhoseFundsWereConsumedConcurrently_IsRefusedRatherThanOverdrawn()
        {
            var (contextA, envelopeA) = await ReadEnvelopeAsync();
            var (contextB, envelopeB) = await ReadEnvelopeAsync();

            // A vide l'enveloppe. B avait autorisé son débit sur les mêmes 1000 $ : les fonds sur
            // lesquels sa garde s'appuyait n'existent plus.
            envelopeA.AvailableFund -= 1000m;
            await contextA.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            envelopeB.AvailableFund -= 600m;
            Func<Task> secondDebit = () => contextB.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            await secondDebit.Should().ThrowAsync<BudgetAllowanceInsufficientFundException>();

            // Et surtout : l'enveloppe n'est pas passée à -600. Refuser bruyamment est le seul
            // comportement sûr — c'est le pendant du crédit rebasé, pas une exception à la règle.
            (await PersistedFundAsync()).Should().Be(0m);
        }

        [Fact]
        public async Task CreditIntoAnAlreadyOverdrawnEnvelope_IsNotRefused()
        {
            // Les enveloppes à découvert existent en production — VerifyBudgetAllowanceReservations
            // les compte. Un remboursement doit y entrer même s'il ne suffit pas à les ramener à zéro :
            // refuser bloquerait le retrait du participant sans jamais lui rendre son argent.
            var (setup, envelopeToDrain) = await ReadEnvelopeAsync();
            envelopeToDrain.AvailableFund = -50m;
            await setup.SaveChangesAsync();

            var (context, envelope) = await ReadEnvelopeAsync();
            envelope.AvailableFund += 30m;
            await context.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            (await PersistedFundAsync()).Should().Be(-20m);
        }

        [Fact]
        public async Task ConcurrentMoves_KeepOriginalFundAndAvailableFundInStep()
        {
            // MoveBudgetAllowance et EditBudgetAllowance déplacent OriginalFund du même delta que
            // AvailableFund. Si le rebase n'en corrigeait qu'un, « OriginalFund - AvailableFund »
            // — l'engagement qu'audite VerifyBudgetAllowanceReservations — deviendrait faux en
            // silence, alors qu'avant le correctif les deux se perdaient ensemble.
            var (contextA, envelopeA) = await ReadEnvelopeAsync();
            var (contextB, envelopeB) = await ReadEnvelopeAsync();

            envelopeA.AvailableFund -= 100m;
            envelopeA.OriginalFund -= 100m;
            await contextA.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            envelopeB.AvailableFund -= 250m;
            envelopeB.OriginalFund -= 250m;
            await contextB.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            var verify = CreateDbContext();
            var persisted = await verify.BudgetAllowances.AsNoTracking()
                .Where(x => x.Id == budgetAllowance.Id)
                .Select(x => new { x.AvailableFund, x.OriginalFund }).SingleAsync();

            persisted.AvailableFund.Should().Be(650m);
            persisted.OriginalFund.Should().Be(650m);

            // Le seul invariant qui compte vraiment : les deux montants ont encaissé les mêmes
            // mouvements, donc l'engagement reste exact.
            (persisted.OriginalFund - persisted.AvailableFund).Should().Be(0m);
        }

        [Fact]
        public async Task ConflictOnAnotherEntity_IsNotSwallowed()
        {
            // Le joint ne sait rebaser que des mouvements d'enveloppe. Un conflit portant sur autre
            // chose doit ressortir tel quel plutôt que d'être « résolu » par une valeur inventée.
            var contextA = CreateDbContext();
            var contextB = CreateDbContext();

            var envelopeA = await contextA.BudgetAllowances.FirstAsync(x => x.Id == budgetAllowance.Id);
            var envelopeB = await contextB.BudgetAllowances.FirstAsync(x => x.Id == budgetAllowance.Id);

            contextA.BudgetAllowances.Remove(envelopeA);
            await contextA.SaveChangesAsync();

            // L'enveloppe que B veut créditer n'existe plus : il n'y a aucun solde sur lequel rebaser.
            envelopeB.AvailableFund += 216m;
            Func<Task> creditOnDeletedEnvelope = () => contextB.SaveChangesWithBudgetAllowanceRetryAsync(CancellationToken.None);

            await creditOnDeletedEnvelope.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
