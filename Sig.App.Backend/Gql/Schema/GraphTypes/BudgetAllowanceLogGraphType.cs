using GraphQL.Conventions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BudgetAllowanceLogGraphType
    {
        private readonly BudgetAllowanceLog budgetAllowanceLog;

        public Id Id => budgetAllowanceLog.GetIdentifier();

        public BudgetAllowanceLogDiscriminator Discriminator => budgetAllowanceLog.Discriminator;
        public decimal Amount => budgetAllowanceLog.Amount;
        public long? OrganizationId => budgetAllowanceLog.OrganizationId;
        public string OrganizationName => budgetAllowanceLog.OrganizationName;
        public long? SubscriptionId => budgetAllowanceLog.SubscriptionId;
        public string SubscriptionName => budgetAllowanceLog.SubscriptionName;
        public long? TargetBudgetAllowanceId => budgetAllowanceLog.TargetBudgetAllowanceId;
        public long? TargetOrganizationId => budgetAllowanceLog.TargetBudgetAllowanceId;
        public string TargetOrganizationName => budgetAllowanceLog.TargetOrganizationName;
        public long? TargetSubscriptionId => budgetAllowanceLog.TargetSubscriptionId;
        public string TargetSubscriptionName => budgetAllowanceLog.TargetSubscriptionName;

        public string InitiatorId => budgetAllowanceLog.InitiatorId;
        public string InitiatorFirstname => budgetAllowanceLog.InitiatorFirstname;
        public string InitiatorLastname => budgetAllowanceLog.InitiatorLastname;
        public string InitiatorEmail => budgetAllowanceLog.InitiatorEmail;

        public BudgetAllowanceLogGraphType(BudgetAllowanceLog budgetAllowanceLog)
        {
            this.budgetAllowanceLog = budgetAllowanceLog;
        }

        public OffsetDateTime CreatedAt()
        {
            return budgetAllowanceLog.CreatedAtUtc.FromUtcToOffsetDateTime();
        }
    }
}
