﻿using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class LoyaltyAddingFundTransactionGraphType : IAddingFundTransactionGraphType
    {
        private readonly LoyaltyAddingFundTransaction transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public decimal AvailableFund => transaction.AvailableFund;

        // LoyaltyAddingFundTransaction is always Actived
        public FundTransactionStatus Status => FundTransactionStatus.Actived;

        public LoyaltyAddingFundTransactionGraphType(LoyaltyAddingFundTransaction transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (transaction.CardId.HasValue) return ctx.DataLoader.LoadCardById(transaction.CardId.Value);
            return null;
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(transaction.ProductGroupId);
        }

        public OffsetDateTime CreatedAt()
        {
            return transaction.CreatedAtUtc.FromUtcToOffsetDateTime();
        }

        // LoyaltyAddingFundTransaction does not have ExpirationDate
        public OffsetDateTime? ExpirationDate()
        {
            return null;
        }
    }
}
