using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ShortenCaissePrefixedCashRegisterNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ;WITH ToUpdate AS (
                    SELECT DISTINCT cr.Id AS CashRegisterId, mg.Name AS NewName
                    FROM CashRegisters AS cr
                    INNER JOIN Markets AS m ON m.Id = cr.MarketId
                    INNER JOIN CashRegisterMarketGroups AS crmg ON crmg.CashRegisterId = cr.Id
                    INNER JOIN MarketGroups AS mg ON mg.Id = crmg.MarketGroupId
                    WHERE cr.Name = CONCAT(N'Caisse - ', mg.Name, N' / ', m.Name)
                )
                UPDATE cr
                SET cr.Name = u.NewName
                FROM CashRegisters AS cr
                INNER JOIN ToUpdate AS u ON u.CashRegisterId = cr.Id");

            migrationBuilder.Sql(@"
                ;WITH LogsToUpdate AS (
                    SELECT DISTINCT tl.Id AS TransactionLogId, mg.Name AS NewName
                    FROM TransactionLogs AS tl
                    INNER JOIN CashRegisters AS cr ON tl.CashRegisterId = cr.Id
                    INNER JOIN Markets AS m ON m.Id = cr.MarketId
                    INNER JOIN CashRegisterMarketGroups AS crmg ON crmg.CashRegisterId = cr.Id
                    INNER JOIN MarketGroups AS mg ON mg.Id = crmg.MarketGroupId
                    WHERE tl.CashRegisterName = CONCAT(N'Caisse - ', mg.Name, N' / ', m.Name)
                )
                UPDATE tl
                SET tl.CashRegisterName = u.NewName
                FROM TransactionLogs AS tl
                INNER JOIN LogsToUpdate AS u ON u.TransactionLogId = tl.Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
