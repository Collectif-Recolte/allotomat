using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class RemoveBeneficiaryFromSubscriptionTest : TestBase
    {
        private readonly IRequestHandler<RemoveBeneficiaryFromSubscription.Input> handler;
        private readonly Subscription subscription;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Project project;
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;
        private readonly ProductGroup productGroup1;
        private readonly ProductGroup productGroup2;
        private readonly BeneficiaryType beneficiaryType;

        public RemoveBeneficiaryFromSubscriptionTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 1",
                Name = "Beneficiary type 1"
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            productGroup1 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup1);

            productGroup2 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2
            };
            DbContext.ProductGroups.Add(productGroup2);

            var subscriptionType = new SubscriptionType()
            {
                Amount = 25,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup1
            };

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    subscriptionType,
                    new SubscriptionType()
                    {
                        Amount = 50,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 100,
                        ProductGroup = productGroup2
                    }
                },
                Project = project
            };

            var budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 25,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 75
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            // CRCL-2606 : l'enveloppe est passée de 75 à 25, donc 50 $ (2 versements de 25) ont été
            // réservés pour cette paire à l'assignation. C'est cette réservation qu'un retrait rend,
            // pas le calendrier restant.
            subscriptionBeneficiary = new SubscriptionBeneficiary
            {
                Beneficiary = beneficiary,
                BeneficiaryType = beneficiaryType,
                Subscription = subscription,
                BudgetAllowance = budgetAllowance,
                RemainingAllocatedAmount = 50m
            };
            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };

            DbContext.Subscriptions.Add(subscription);

            // Record today's AddingFundToCard run so payment-day logic is deterministic for these tests
            DbContext.AddingFundToCardRuns.Add(new Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun
            {
                Date = Clock.GetCurrentInstant().ToDateTimeUtc(),
                Name = SubscriptionHelper.AddingFundToCardFirstDayOfTheMonthJobName
            });

            DbContext.SaveChanges();

            handler = new RemoveBeneficiaryFromSubscription(NullLogger<RemoveBeneficiaryFromSubscription>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscription()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBeneficiary.Subscriptions.Should().HaveCount(0);
            localSubscription.Beneficiaries.Should().HaveCount(0);
            // CRCL-2606 : 25 (disponible) + 50 (réservation non livrée) = 75
            localBudgetAllowance.AvailableFund.Should().Be(75);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscriptionWithMaximumPaymentCount()
        {
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            // Max 1 versement, 1 déjà livré ci-dessous : il ne reste rien de réservé.
            subscriptionBeneficiary.RemainingAllocatedAmount = 0m;
            beneficiary.Card = new Card()
            {
                Transactions = new List<Transaction>() {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 25,
                        SubscriptionType = new SubscriptionType()
                        {
                            Subscription = subscription,
                            ProductGroup = subscription.Types.First(x => x.BeneficiaryType == beneficiary.BeneficiaryType).ProductGroup,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        },
                        Beneficiary = beneficiary
                    }
                }
            };

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBeneficiary.Subscriptions.Should().HaveCount(0);
            localSubscription.Beneficiaries.Should().HaveCount(0);
            localBudgetAllowance.AvailableFund.Should().Be(25);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscriptionWithTwoMaximumPaymentAndOneTransaction()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4);
            // Max 2 versements réservés (50), 1 livré ci-dessous : il reste 25 de réservé.
            subscriptionBeneficiary.RemainingAllocatedAmount = 25m;
            beneficiary.Card = new Card()
            {
                Transactions = new List<Transaction>() {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 25,
                        SubscriptionType = new SubscriptionType()
                        {
                            Subscription = subscription,
                            ProductGroup = subscription.Types.First(x => x.BeneficiaryType == beneficiary.BeneficiaryType).ProductGroup,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        },
                        Beneficiary = beneficiary
                    }
                }
            };

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBeneficiary.Subscriptions.Should().HaveCount(0);
            localSubscription.Beneficiaries.Should().HaveCount(0);
            localBudgetAllowance.AvailableFund.Should().Be(50);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscriptionWithMaxNumberOfPaymentsOverride()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4);
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 3;
            // Max effectif 3 versements (75 réservés), 1 livré ci-dessous : il reste 50 de réservé.
            subscriptionBeneficiary.RemainingAllocatedAmount = 50m;

            beneficiary.Card = new Card()
            {
                Transactions = new List<Transaction>() {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 25,
                        SubscriptionType = new SubscriptionType()
                        {
                            Subscription = subscription,
                            ProductGroup = subscription.Types.First(x => x.BeneficiaryType == beneficiary.BeneficiaryType).ProductGroup,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        },
                        Beneficiary = beneficiary
                    }
                }
            };

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            // CRCL-2606 : le calendrier n'est plus consulté. On rend la réservation non livrée (50),
            // soit 25 (disponible) + 50 = 75.
            localBudgetAllowance.AvailableFund.Should().Be(75);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscriptionCreatesTransactionLogWithCorrectFields()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync(x =>
                x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            transactionLog.TotalAmount.Should().Be(50);
            transactionLog.BeneficiaryId.Should().Be(beneficiary.Id);
            transactionLog.BeneficiaryFirstname.Should().Be(beneficiary.Firstname);
            transactionLog.BeneficiaryLastname.Should().Be(beneficiary.Lastname);
            transactionLog.OrganizationId.Should().Be(organization.Id);
            transactionLog.OrganizationName.Should().Be(organization.Name);
            transactionLog.SubscriptionId.Should().Be(subscription.Id);
            transactionLog.SubscriptionName.Should().Be(subscription.Name);
            transactionLog.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public async Task NonUsageBasedUnderDeliveredRefundsOnlyTheUnusedReservation()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            // CRCL-2606 (AC 1) — Abonnement non usage-based, personne sous-livrée : elle a reçu moins
            // de versements que le calendrier écoulé. On étend la saison pour que le calendrier restant
            // (5) diverge nettement de la réservation réelle (2 versements = 50).
            subscription.IsSubscriptionPaymentBasedCardUsage = false;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(5);
            subscriptionBeneficiary.RemainingAllocatedAmount = 50m;

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLog = await DbContext.TransactionLogs.FirstAsync(x =>
                x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            // 25 (disponible) + 50 (réservation non livrée) = 75
            localBudgetAllowance.AvailableFund.Should().Be(75);
            transactionLog.TotalAmount.Should().Be(50);

            // C'est ce test qui aurait attrapé le déficit de -1216 $ : l'ancienne formule remboursait
            // le calendrier restant (5 x 25 = 125), donc l'enveloppe montait à 150.
            localBudgetAllowance.AvailableFund.Should().NotBe(150);
        }

        [Fact]
        public async Task NonUsageBasedOverDeliveredRefundsZero()
        {
            // CRCL-2606 (AC 2) — Personne en avance sur sa réservation : le solde est négatif. Un
            // retrait ne débite jamais l'enveloppe, le remboursement est plafonné à 0.
            subscription.IsSubscriptionPaymentBasedCardUsage = false;
            subscriptionBeneficiary.RemainingAllocatedAmount = -25m;

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLog = await DbContext.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .FirstAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBudgetAllowance.AvailableFund.Should().Be(25);
            transactionLog.TotalAmount.Should().Be(0);
            transactionLog.TransactionLogProductGroups.Should().OnlyContain(x => x.Amount == 0);
        }

        [Fact]
        public async Task UsageBasedIgnoresCalendarAndRefundsTheReservation()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            // CRCL-2606 (AC 3) — Usage-based : comportement inchangé, mais la preuve que le calendrier
            // n'est plus consulté du tout. EndDate volontairement lointain (calendrier restant = 5)
            // alors que la réservation non livrée ne vaut qu'un versement.
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;
            subscription.EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(5);
            subscriptionBeneficiary.RemainingAllocatedAmount = 25m;

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLog = await DbContext.TransactionLogs.FirstAsync(x =>
                x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBudgetAllowance.AvailableFund.Should().Be(50);
            transactionLog.TotalAmount.Should().Be(25);
        }

        [Fact]
        public async Task LegacyNullAllocatedAmountFallsBackToCalendarEstimate()
        {
            // CRCL-2606 — Ligne que le job de backfill n'a pas encore reconstruite. Elle doit garder
            // l'ancien comportement calendaire (1 versement restant x 25 = 25) plutôt que rembourser 0
            // et immobiliser l'argent. Cette branche sert de vrais retraits jusqu'à ce que le backfill
            // ait tourné partout : elle doit rester couverte.
            subscriptionBeneficiary.RemainingAllocatedAmount = null;

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();

            localBudgetAllowance.AvailableFund.Should().Be(50);
        }

        [Fact]
        public async Task RefundProductGroupBreakdownAlwaysSumsToTotalAmount()
        {
            // CRCL-2606 — Le remboursement n'est plus forcément un multiple entier du versement, donc
            // la ventilation par groupe de produits est au prorata. La somme des parts doit rester
            // exactement égale à TotalAmount, résidu d'arrondi inclus.
            subscription.Types.Add(new SubscriptionType()
            {
                Amount = 5,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup2
            });
            // 30 par versement (25 + 5) et une réservation qui n'en est pas un multiple.
            subscriptionBeneficiary.RemainingAllocatedAmount = 17m;

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var transactionLog = await DbContext.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .FirstAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            transactionLog.TotalAmount.Should().Be(17);
            transactionLog.TransactionLogProductGroups.Should().HaveCount(2);
            transactionLog.TransactionLogProductGroups.Sum(x => x.Amount).Should().Be(17);
        }

        [Fact]
        public async Task ThrowsIfSubsriptionNotFound()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotInSubscription()
        {
            var localProject = new Project();
            var localBeneficiary = new Beneficiary()
            {
                Firstname = "Jane",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Organization = new Organization()
                {
                    Project = localProject
                }
            };
            DbContext.Beneficiaries.Add(localBeneficiary);
            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                BeneficiaryId = localBeneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.BeneficiaryNotInSubscriptionException>();
        }
    }
}
