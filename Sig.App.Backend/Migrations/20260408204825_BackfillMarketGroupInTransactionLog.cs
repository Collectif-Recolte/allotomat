using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMarketGroupInTransactionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE TransactionLogs
                SET
                    MarketGroupId = (
                        SELECT TOP 1 mg.Id
                        FROM CashRegisterMarketGroups cmg
                        INNER JOIN MarketGroups mg ON mg.Id = cmg.MarketGroupId
                        WHERE cmg.CashRegisterId = TransactionLogs.CashRegisterId
                          AND mg.ProjectId = TransactionLogs.ProjectId
                    ),
                    MarketGroupName = (
                        SELECT TOP 1 mg.Name
                        FROM CashRegisterMarketGroups cmg
                        INNER JOIN MarketGroups mg ON mg.Id = cmg.MarketGroupId
                        WHERE cmg.CashRegisterId = TransactionLogs.CashRegisterId
                          AND mg.ProjectId = TransactionLogs.ProjectId
                    )
                WHERE MarketGroupId IS NULL
                  AND CashRegisterId IS NOT NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
