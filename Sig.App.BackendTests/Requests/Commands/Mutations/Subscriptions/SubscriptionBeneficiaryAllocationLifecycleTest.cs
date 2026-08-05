using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
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
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    /// <summary>
    /// Cycle de vie complet d'une réservation à travers les vrais handlers : assignation, livraison par
    /// le job, retrait. L'invariant vérifié est la CONSERVATION de l'enveloppe :
    ///
    ///     solde final == solde initial - argent réellement livré sur les cartes
    ///
    /// C'est l'invariant que le calcul calendaire cassait, et ce que ces tests permettent de valider en
    /// local avant d'aller tester à la main.
    /// </summary>
    public class SubscriptionBeneficiaryAllocationLifecycleTest : TestBase
    {
        private const decimal AmountPerPayment = 25m;
        private const decimal InitialEnvelope = 1000m;

        private readonly AssignBeneficiariesToSubscription assignHandler;
        private readonly RemoveBeneficiaryFromSubscription removeHandler;
        private readonly AddingFundToCard job;

        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly Beneficiary beneficiaryWithCard;
        private readonly Beneficiary beneficiaryWithoutCard;

        public SubscriptionBeneficiaryAllocationLifecycleTest()
        {
            var project = new Project() { Name = "Project 1" };
            DbContext.Projects.Add(project);

            organization = new Organization() { Name = "Organization 1", Project = project };
            DbContext.Organizations.Add(organization);

            var beneficiaryType = new BeneficiaryType() { Project = project, Keys = "type1", Name = "Type 1" };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            var productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            beneficiaryWithCard = new Beneficiary()
            {
                Firstname = "Avec",
                Lastname = "Carte",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            var card = new Card()
            {
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiaryWithCard,
                Funds = new List<Fund>(),
                Transactions = new List<Transaction>()
            };
            beneficiaryWithCard.Card = card;
            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiaryWithCard);

            beneficiaryWithoutCard = new Beneficiary()
            {
                Firstname = "Sans",
                Lastname = "Carte",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiaryWithoutCard);

            organization.Beneficiaries = new List<Beneficiary>() { beneficiaryWithCard, beneficiaryWithoutCard };

            // Saison du 1er juillet au 1er décembre 2025, versement le 1er de chaque mois.
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                StartDate = new DateTime(2025, 7, 1),
                EndDate = new DateTime(2025, 12, 1),
                FundsExpirationDate = new DateTime(2026, 1, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = AmountPerPayment,
                        BeneficiaryType = beneficiaryType,
                        ProductGroup = productGroup
                    }
                },
                Beneficiaries = new List<SubscriptionBeneficiary>()
            };
            DbContext.Subscriptions.Add(subscription);

            DbContext.BudgetAllowances.Add(new BudgetAllowance()
            {
                AvailableFund = InitialEnvelope,
                OriginalFund = InitialEnvelope,
                Organization = organization,
                Subscription = subscription
            });

            DbContext.SaveChanges();

            assignHandler = new AssignBeneficiariesToSubscription(
                NullLogger<AssignBeneficiariesToSubscription>.Instance, Clock, HttpContextAccessor, DbContext,
                NullLogger<AddingFundToCard>.Instance);
            removeHandler = new RemoveBeneficiaryFromSubscription(
                NullLogger<RemoveBeneficiaryFromSubscription>.Instance, DbContext, Clock, HttpContextAccessor);
            job = new AddingFundToCard(DbContext, Clock, NullLogger<AddingFundToCard>.Instance);
        }

        [Fact]
        public async Task NothingDelivered_EnvelopeReturnsToItsInitialAmount()
        {
            // Assignation le 2 juillet : 5 versements restants au calendrier, donc 125 réservés.
            await Assign(beneficiaryWithCard);
            await EnvelopeShouldBe(InitialEnvelope - 125m);

            // Deux jours de versement passent sans que le job tourne : rien n'est livré, mais le
            // calendrier restant a rétréci. C'est le scénario qui creusait le déficit.
            Clock.Reset(Instant.FromUtc(2025, 9, 2, 0, 0));

            await Remove(beneficiaryWithCard);

            await EnvelopeShouldBe(InitialEnvelope);
            // L'ancien calcul rendait le calendrier restant (3 x 25 = 75) et laissait -50 dans l'enveloppe.
            (await Envelope()).AvailableFund.Should().NotBe(950m);
        }

        [Fact]
        public async Task MidSeasonEnrollee_IsRefundedOnlyWhatWasReserved()
        {
            // Inscription en cours de saison : 3 versements restants, donc 75 réservés et non 150.
            Clock.Reset(Instant.FromUtc(2025, 9, 2, 0, 0));

            await Assign(beneficiaryWithCard);
            await EnvelopeShouldBe(InitialEnvelope - 75m);

            await Remove(beneficiaryWithCard);

            // Rembourser « total de la saison - livré » aurait sur-crédité l'enveloppe d'argent que le
            // programme n'y a jamais mis.
            await EnvelopeShouldBe(InitialEnvelope);
        }

        [Fact]
        public async Task OneVersementDelivered_EnvelopeLosesExactlyWhatTheCardReceived()
        {
            await Assign(beneficiaryWithCard);

            // Le 1er août le job livre un versement : l'enveloppe ne bouge pas, la réservation baisse.
            Clock.Reset(Instant.FromUtc(2025, 8, 1, 0, 0));
            await RunFundJob();

            await EnvelopeShouldBe(InitialEnvelope - 125m);
            (await AllocationOf(beneficiaryWithCard)).Should().Be(100m);

            var card = await DbContext.Cards.Include(x => x.Funds).FirstAsync();
            card.Funds.Sum(x => x.Amount).Should().Be(AmountPerPayment);

            await Remove(beneficiaryWithCard);

            // Conservation : seul le versement réellement livré reste sorti de l'enveloppe.
            await EnvelopeShouldBe(InitialEnvelope - AmountPerPayment);
        }

        [Fact]
        public async Task NoCard_ReleasedReservationIsNotRefundedTwice()
        {
            await Assign(beneficiaryWithoutCard);
            await EnvelopeShouldBe(InitialEnvelope - 125m);

            // Sans carte, le job rend le versement à l'enveloppe et consomme la réservation d'autant.
            Clock.Reset(Instant.FromUtc(2025, 8, 1, 0, 0));
            await RunFundJob();

            await EnvelopeShouldBe(InitialEnvelope - 100m);
            (await AllocationOf(beneficiaryWithoutCard)).Should().Be(100m);

            await Remove(beneficiaryWithoutCard);

            // Rien n'a été livré : l'enveloppe doit être intacte, et le versement relâché ne doit pas
            // avoir été rendu deux fois.
            await EnvelopeShouldBe(InitialEnvelope);
        }

        [Fact]
        public async Task FullSeasonDelivered_EnvelopeLosesEverythingThatWasReserved()
        {
            await Assign(beneficiaryWithCard);

            foreach (var month in new[] { 8, 9, 10, 11, 12 })
            {
                Clock.Reset(Instant.FromUtc(2025, month, 1, 0, 0));
                await RunFundJob();
            }

            // 5 versements livrés, la réservation est entièrement consommée.
            (await AllocationOf(beneficiaryWithCard)).Should().Be(0m);

            var card = await DbContext.Cards.Include(x => x.Funds).FirstAsync();
            card.Funds.Sum(x => x.Amount).Should().Be(125m);

            await Remove(beneficiaryWithCard);

            // Plus rien à rendre : l'enveloppe reste à 875.
            await EnvelopeShouldBe(InitialEnvelope - 125m);
        }

        private async Task Assign(Beneficiary beneficiary)
        {
            await assignHandler.Handle(new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                Beneficiaries = new[] { beneficiary.GetIdentifier() }
            }, CancellationToken.None);
        }

        private async Task Remove(Beneficiary beneficiary)
        {
            await removeHandler.Handle(new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            }, CancellationToken.None);
        }

        private Task RunFundJob()
        {
            return job.Run($"AddingFundToCard:FirstDayOfTheMonth",
                new[] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });
        }

        private Task<BudgetAllowance> Envelope()
        {
            return DbContext.BudgetAllowances.FirstAsync(x => x.SubscriptionId == subscription.Id);
        }

        private async Task EnvelopeShouldBe(decimal expected)
        {
            (await Envelope()).AvailableFund.Should().Be(expected);
        }

        private async Task<decimal?> AllocationOf(Beneficiary beneficiary)
        {
            var pair = await DbContext.SubscriptionBeneficiaries
                .FirstAsync(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionId == subscription.Id);
            return pair.RemainingAllocatedAmount;
        }
    }
}
