using System.Collections.Generic;
using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Cards
{
    public class UnassignCardToBeneficiaryTest : TestBase
    {
        private readonly UnassignCardFromBeneficiary handler;
        private readonly Organization organization;
        private readonly ProductGroup productGroup;
        private readonly Subscription subscription1;
        private readonly Subscription subscription2;
        private readonly SubscriptionType subscriptionType1;
        private readonly BudgetAllowance budgetAllowance1;
        private readonly BudgetAllowance budgetAllowance2;
        private readonly Beneficiary beneficiary;
        private readonly Card card;

        public UnassignCardToBeneficiaryTest()
        {
            organization = new Organization();

            budgetAllowance1 = new BudgetAllowance()
            {
                Id = 1,
                AvailableFund = 100,
                Organization = organization
            };

            productGroup = new ProductGroup();
            
            subscriptionType1 = new SubscriptionType()
            {
                ProductGroup = productGroup
            };
            
            subscription1 = new Subscription()
            {
                BudgetAllowances = new List<BudgetAllowance>() { budgetAllowance1 },
                Types = new List<SubscriptionType>() { subscriptionType1 },
            };
            DbContext.Subscriptions.Add(subscription1);
            
            budgetAllowance2 = new BudgetAllowance()
            {
                Id = 2,
                AvailableFund = 100,
                Organization = organization
            };
            
            subscription2 = new Subscription()
            {
                BudgetAllowances = new List<BudgetAllowance>() { budgetAllowance2 }
            };
            DbContext.Subscriptions.Add(subscription2);
            
            card = new Card()
            {
                Status = CardStatus.Unassigned
            };
            
            card.Transactions = new List<Transaction>()
            {
                new SubscriptionAddingFundTransaction()
                {
                    Amount = 100,
                    AvailableFund = 50,
                    SubscriptionType = subscriptionType1,
                    ProductGroup = productGroup
                },
                new ManuallyAddingFundTransaction()
                {
                    Amount = 100,
                    AvailableFund = 25,
                    Subscription = subscription2,
                    ProductGroup = productGroup
                },
                new PaymentTransaction()
                {
                    Amount = 100
                }
            };

            DbContext.Cards.Add(card);

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Card = card,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new UnassignCardFromBeneficiary(NullLogger<UnassignCardFromBeneficiary>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task UnassignCardToBeneficiary()
        {
            var input = new UnassignCardFromBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = DbContext.Beneficiaries.First();
            localBeneficiary.CardId.Should().Be(null);

            var transactionLogCreated = await DbContext.TransactionLogs.Where(x =>
                x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromUnassignedCardTransactionLog).ToListAsync();
            transactionLogCreated.Count.Should().Be(2);
        }
        
        [Fact]
        public async Task UnassignCardRefundsBudgetAllowance()
        {
            var input = new UnassignCardFromBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);
            
            var budgetAllowanceAfterRefund = DbContext.BudgetAllowances.First(x => x.Id == 1);
            budgetAllowanceAfterRefund.AvailableFund.Should().Be(150);
            var budgetAllowance2AfterRefund = DbContext.BudgetAllowances.First(x => x.Id == 2);
            budgetAllowance2AfterRefund.AvailableFund.Should().Be(125);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new UnassignCardFromBeneficiary.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                CardId = card.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnassignCardFromBeneficiary.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotFound()
        {
            var input = new UnassignCardFromBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = Id.New<Card>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnassignCardFromBeneficiary.CardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotAssignToBeneficiaryException()
        {
            var localCard = new Card()
            {
                Status = CardStatus.Unassigned
            };

            DbContext.Cards.Add(localCard);

            await DbContext.SaveChangesAsync();

            var input = new UnassignCardFromBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = localCard.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnassignCardFromBeneficiary.CardNotAssignToBeneficiaryException>();
        }
    }
}
