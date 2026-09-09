using FluentAssertions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Xunit;

namespace Sig.App.BackendTests.Gql.Schema.GraphTypes
{
    public class BudgetAllowanceLogGraphTypeTest
    {
        private const long OrganizationId = 1;
        private const long OtherOrganizationId = 2;

        [Fact]
        public void FlagsTheTransferReceivedByTheOrganizationLookingAtTheReport()
        {
            // CRCL-2684 : le tableau inverse le signe de tout transfert, ce qui décrivait le retrait
            // côté source. Vu du groupe destinataire, la même ligne est une entrée.
            var log = MoveLog(from: OtherOrganizationId, to: OrganizationId);

            var graphType = new BudgetAllowanceLogGraphType(log, OrganizationId);

            graphType.IsIncomingTransfer.Should().BeTrue();
        }

        [Fact]
        public void DoesNotFlagTheTransferSentByTheOrganizationLookingAtTheReport()
        {
            var log = MoveLog(from: OrganizationId, to: OtherOrganizationId);

            var graphType = new BudgetAllowanceLogGraphType(log, OrganizationId);

            graphType.IsIncomingTransfer.Should().BeFalse();
        }

        [Fact]
        public void DoesNotFlagAnythingWithoutAnOrganizationPointOfView()
        {
            // Le rapport programme voit les deux côtés du transfert : aucun n'est entrant pour lui.
            var log = MoveLog(from: OtherOrganizationId, to: OrganizationId);

            var graphType = new BudgetAllowanceLogGraphType(log);

            graphType.IsIncomingTransfer.Should().BeFalse();
        }

        [Fact]
        public void DoesNotFlagAnAdjustmentThatIsNotATransfer()
        {
            var log = MoveLog(from: OtherOrganizationId, to: OrganizationId);
            log.Discriminator = BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog;

            var graphType = new BudgetAllowanceLogGraphType(log, OrganizationId);

            graphType.IsIncomingTransfer.Should().BeFalse();
        }

        [Fact]
        public void ExposesTheTargetOrganizationRatherThanTheTargetBudgetAllowance()
        {
            var log = MoveLog(from: OtherOrganizationId, to: OrganizationId);
            log.TargetBudgetAllowanceId = 99;

            var graphType = new BudgetAllowanceLogGraphType(log, OrganizationId);

            graphType.TargetOrganizationId.Should().Be(OrganizationId);
        }

        private static BudgetAllowanceLog MoveLog(long from, long to)
        {
            return new BudgetAllowanceLog()
            {
                Discriminator = BudgetAllowanceLogDiscriminator.MoveBudgetAllowanceLog,
                Amount = 300,
                OrganizationId = from,
                TargetOrganizationId = to
            };
        }
    }
}
