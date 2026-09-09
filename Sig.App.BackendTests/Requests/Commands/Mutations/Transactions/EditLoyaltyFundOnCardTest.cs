using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class EditLoyaltyFundOnCardTest : TestBase
    {
        private readonly EditLoyaltyFundOnCard handler;

        private readonly Project project;
        private readonly Card card;
        private readonly Organization organization;
        private readonly Beneficiary beneficiary;

        public EditLoyaltyFundOnCardTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            card = new Card()
            {
                Project = project,
                ProgramCardId = 1,
                Funds = new List<Fund>()
            };

            beneficiary = new Beneficiary()
            {
                Organization = organization
            };

            project.Cards = new List<Card> { card };

            var productGroupLoyalty = new ProductGroup()
            {
                Name = ProductGroupType.LOYALTY,
                Color = ProductGroupColor.Color_0,
                OrderOfAppearance = -1,
                Project = project
            };

            var fund = new Fund()
            {
                ProductGroup = productGroupLoyalty,
                Amount = 10,
                Card = card
            };
            card.Funds.Add(fund);

            DbContext.Funds.Add(fund);
            DbContext.ProductGroups.Add(productGroupLoyalty);
            DbContext.Cards.Add(card);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new EditLoyaltyFundOnCard(NullLogger<EditLoyaltyFundOnCard>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task EditLoyaltyFundOnCard()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 20
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(20);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task RemoveLoyaltyFundOnCard()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 0
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(0);
            card.Status.Should().Be(CardStatus.Unassigned);
            card.Funds.Count.Should().Be(0);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(-10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task RemoveLoyaltyFundOnCardWithBeneficiary()
        {
            card.Beneficiary = beneficiary;
            card.Status = CardStatus.Assigned;
            DbContext.SaveChanges();

            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 0
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(0);
            card.Status.Should().Be(CardStatus.Assigned);
            card.Funds.Count.Should().Be(0);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(-10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task EditLoyaltyFundOnCardCreatesTransactionLogWithCorrectFields()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 20
            };

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            var transactionLog = await DbContext.TransactionLogs.FirstAsync(x => x.TransactionUniqueId == transaction.TransactionUniqueId);

            transactionLog.Discriminator.Should().Be(TransactionLogDiscriminator.LoyaltyEditFundTransactionLog);
            transactionLog.TotalAmount.Should().Be(10);
            transactionLog.CardProgramCardId.Should().Be(card.ProgramCardId);
            transactionLog.ProjectId.Should().Be(project.Id);
        }

        // CRCL-2669 - Cette mutation fixe un solde ABSOLU. Passée au joint, elle était relue comme un
        // mouvement (voulu - lu) et rejouée sur la valeur en base : un achat concurrent faisait
        // atterrir la carte ailleurs que là où l'admin l'avait demandée, pendant que la transaction
        // et le journal enregistraient le montant demandé. Le solde et le grand livre se
        // contredisaient. L'édition est maintenant replanifiée sur des données fraîches.
        [Fact]
        public async Task EditOnAStaleBalance_IsRePlannedOnFreshData_AndTheLedgerMatchesTheCard()
        {
            // Le contexte de l'admin lit la carte pendant qu'elle vaut encore 10.
            var stale = CreateDbContext();
            await stale.Cards.Include(x => x.Funds).ThenInclude(x => x.ProductGroup).FirstAsync(x => x.Id == card.Id);

            // Un achat de 3 est commis entre-temps, depuis un autre contexte.
            var purchase = CreateDbContext();
            var purchasedFund = await purchase.Funds.FirstAsync(x => x.CardId == card.Id);
            purchasedFund.Amount -= 3;
            await purchase.SaveChangesAsync();

            var staleHandler = new EditLoyaltyFundOnCard(NullLogger<EditLoyaltyFundOnCard>.Instance, stale, Clock, HttpContextAccessor);
            await staleHandler.Handle(
                new EditLoyaltyFundOnCard.Input() { CardId = card.GetIdentifier(), Amount = 4 },
                CancellationToken.None);

            var verify = CreateDbContext();
            var persisted = await verify.Funds.AsNoTracking().Where(x => x.CardId == card.Id).Select(x => x.Amount).SingleAsync();

            // L'admin a demandé 4, la carte vaut 4. Avant : 1, le joint ayant rejoué -6 sur 7.
            persisted.Should().Be(4);

            // Et le mouvement enregistré est celui qui a vraiment eu lieu, 7 -> 4. Avant : -6, un
            // mouvement que la carte n'a jamais fait.
            var transaction = await verify.Transactions.OfType<LoyaltyEditFundTransaction>()
                .AsNoTracking().OrderBy(x => x.Id).LastAsync();
            transaction.AvailableFund.Should().Be(4);
            transaction.Amount.Should().Be(-3);

            var transactionLog = await verify.TransactionLogs.AsNoTracking()
                .FirstAsync(x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task ThrowsIfLoyaltyFundCantBeNegativeException()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = -1
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditLoyaltyFundOnCard.LoyaltyFundCantBeNegativeException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotFoundException()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = Id.New<Card>(123456),
                Amount = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditLoyaltyFundOnCard.CardNotFoundException>();
        }
    }
}
