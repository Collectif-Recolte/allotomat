using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Services.QRCode;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class CardGraphType
    {
        private readonly Card card;

        public Id Id => card.GetIdentifier();
        public CardStatus Status => card.Status;
        public long ProgramCardId => card.ProgramCardId;
        public string CardNumber => card.CardNumber;

        public CardGraphType(Card card)
        {
            this.card = card;
        }

        public async Task<decimal> AddedFund(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadTransactionByCardId(card.Id).GetResultAsync();
            return transactions.Where(x => x.GetType() == typeof(AddingFundTransaction)).Sum(x => x.Amount);
        }

        public async Task<decimal> SpentFund(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadTransactionByCardId(card.Id).GetResultAsync();
            return transactions.Where(x => x.GetType() == typeof(PaymentTransaction)).Sum(x => x.Amount);
        }

        public string QrCode([Inject] IQRCodeService qrCodeService)
        {
            return qrCodeService.GenerateQRCode(Id.ToString());
        }

        public IDataLoaderResult<IBeneficiaryGraphType> Beneficiary(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadCardBeneficiary(card.Id);
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(card.ProjectId);
        }

        public async Task<OffsetDateTime?> LastTransactionDate(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadTransactionByCardId(card.Id).GetResultAsync();
            var paymentTransactions = transactions.Where(x => x.GetType() == typeof(PaymentTransaction));
            if (paymentTransactions.Count() > 0)
            {
                return paymentTransactions.OrderBy(x => x.CreatedAtUtc).Last().CreatedAtUtc.FromUtcToOffsetDateTime();
            }
            return null;
        }

        public async Task<IEnumerable<IAddingFundTransactionGraphType>> AddingFundTransactions(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadTransactionByCardId(card.Id).GetResultAsync();
            var addingFundTransactions = transactions.Where(x => x.GetType() == typeof(ManuallyAddingFundTransaction) || x.GetType() == typeof(SubscriptionAddingFundTransaction) || x.GetType() == typeof(OffPlatformAddingFundTransaction));
            if (addingFundTransactions.Count() > 0)
            {
                return addingFundTransactions.Select(x =>
                {
                    switch (x)
                    {
                        case ManuallyAddingFundTransaction maft:
                            return new ManuallyAddingFundTransactionGraphType(maft);
                        case SubscriptionAddingFundTransaction saft:
                            return new SubscriptionAddingFundTransactionGraphType(saft);
                        case OffPlatformAddingFundTransaction opaft:
                            return new OffPlatformAddingFundTransactionGraphType(opaft) as IAddingFundTransactionGraphType;
                    }

                    return null;
                });
            }

            return null;
        }

        public IDataLoaderResult<IEnumerable<FundGraphType>> Funds(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionCardFunds(Id.LongIdentifierForType<Card>());
        }

        public async Task<decimal> TotalFund(IAppUserContext ctx)
        {
            var funds = await ctx.DataLoader.LoadSubscriptionCardFunds(Id.LongIdentifierForType<Card>()).GetResultAsync();
            return funds.Sum(x => x.Amount);
        }

        public async Task<FundGraphType> LoyaltyFund(IAppUserContext ctx)
        {
            var loyaltyFund = await ctx.DataLoader.LoadLoyaltyCardFund(Id.LongIdentifierForType<Card>()).GetResultAsync();
            if (loyaltyFund == null)
            {
                return null;
            }

            return loyaltyFund;
        }
    }
}
