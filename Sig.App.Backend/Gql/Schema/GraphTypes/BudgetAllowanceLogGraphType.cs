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

        // Null pour le rapport programme, qui voit les deux côtés d'un transfert; renseigné pour le
        // rapport d'un groupe, seul point de vue d'où un transfert peut être entrant.
        private readonly long? viewpointOrganizationId;

        public Id Id => budgetAllowanceLog.GetIdentifier();

        public BudgetAllowanceLogDiscriminator Discriminator => budgetAllowanceLog.Discriminator;
        public decimal Amount => budgetAllowanceLog.Amount;
        public long? OrganizationId => budgetAllowanceLog.OrganizationId;
        public string OrganizationName => budgetAllowanceLog.OrganizationName;
        public long? SubscriptionId => budgetAllowanceLog.SubscriptionId;
        public string SubscriptionName => budgetAllowanceLog.SubscriptionName;
        public long? TargetBudgetAllowanceId => budgetAllowanceLog.TargetBudgetAllowanceId;
        public long? TargetOrganizationId => budgetAllowanceLog.TargetOrganizationId;
        public string TargetOrganizationName => budgetAllowanceLog.TargetOrganizationName;
        public long? TargetSubscriptionId => budgetAllowanceLog.TargetSubscriptionId;
        public string TargetSubscriptionName => budgetAllowanceLog.TargetSubscriptionName;

        // Un transfert est entrant quand l'enveloppe destinataire appartient au groupe qui consulte :
        // son enveloppe augmente, alors que Amount décrit le retrait du côté source.
        public bool IsIncomingTransfer =>
            budgetAllowanceLog.Discriminator == BudgetAllowanceLogDiscriminator.MoveBudgetAllowanceLog
            && viewpointOrganizationId.HasValue
            && budgetAllowanceLog.TargetOrganizationId == viewpointOrganizationId;

        public string InitiatorId => budgetAllowanceLog.InitiatorId;
        public string InitiatorFirstname => budgetAllowanceLog.InitiatorFirstname;
        public string InitiatorLastname => budgetAllowanceLog.InitiatorLastname;
        public string InitiatorEmail => budgetAllowanceLog.InitiatorEmail;

        public BudgetAllowanceLogGraphType(BudgetAllowanceLog budgetAllowanceLog, long? viewpointOrganizationId = null)
        {
            this.budgetAllowanceLog = budgetAllowanceLog;
            this.viewpointOrganizationId = viewpointOrganizationId;
        }

        public OffsetDateTime CreatedAt()
        {
            return budgetAllowanceLog.CreatedAtUtc.FromUtcToOffsetDateTime();
        }
    }
}
