using System.Collections.Generic;
using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class TransactionLogGraphType
    {
        private readonly TransactionLog transactionLog;

        public Id Id => transactionLog.GetIdentifier();
        public TransactionLogDiscriminator Discriminator => transactionLog.Discriminator;
        public decimal TotalAmount => transactionLog.TotalAmount;
        public long? BeneficiaryId => transactionLog.BeneficiaryId;
        public string BeneficiaryFirstname => transactionLog.BeneficiaryFirstname;
        public string BeneficiaryLastname => transactionLog.BeneficiaryLastname;
        public string BeneficiaryEmail => transactionLog.BeneficiaryEmail;
        public string BeneficiaryPhone => transactionLog.BeneficiaryPhone;
        public string BeneficiaryID1 => transactionLog.BeneficiaryID1;
        public string BeneficiaryID2 => transactionLog.BeneficiaryID2;
        public long? BeneficiaryTypeId => transactionLog.BeneficiaryTypeId;
        public bool BeneficiaryIsOffPlatform => transactionLog.BeneficiaryIsOffPlatform;
        public long? CardProgramCardId => transactionLog.CardProgramCardId;
        public string CardNumber => transactionLog.CardNumber;
        public long? FundTransferredFromProgramCardId => transactionLog.FundTransferredFromProgramCardId;
        public string FundTransferredFromCardNumber => transactionLog.FundTransferredFromCardNumber;
        public long? MarketId => transactionLog.MarketId;
        public string MarketName => transactionLog.MarketName;
        public long? OrganizationId => transactionLog.OrganizationId;
        public string OrganizationName => transactionLog.OrganizationName;
        public long? SubscriptionId => transactionLog.SubscriptionId;
        public string SubscriptionName => transactionLog.SubscriptionName;
        public string TransactionInitiatorId => transactionLog.TransactionInitiatorId;
        public string TransactionInitiatorFirstname => transactionLog.TransactionInitiatorFirstname;
        public string TransactionInitiatorLastname => transactionLog.TransactionInitiatorLastname;
        public string TransactionInitiatorEmail => transactionLog.TransactionInitiatorEmail;

        public TransactionLogGraphType(TransactionLog transactionLog)
        {
            this.transactionLog = transactionLog;
        }
        
        public OffsetDateTime CreatedAt()
        {
            return transactionLog.CreatedAtUtc.FromUtcToOffsetDateTime();
        }
        
        public IDataLoaderResult<IEnumerable<TransactionLogProductGroupGraphType>> TransactionLogByProductGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadTransactionLogProductGroupByTransactionLogId(transactionLog.Id);
        }
    }
    
    public class TransactionLogProductGroupGraphType
    {
        private readonly TransactionLogProductGroup transactionLogProductGroup;

        public Id Id => transactionLogProductGroup.GetIdentifier();
        public decimal Amount => transactionLogProductGroup.Amount;
        public long? ProductGroupId => transactionLogProductGroup.ProductGroupId;
        public string ProductGroupName => transactionLogProductGroup.ProductGroupName;

        public TransactionLogProductGroupGraphType(TransactionLogProductGroup transactionLogProductGroup)
        {
            this.transactionLogProductGroup = transactionLogProductGroup;
        }
    }
}