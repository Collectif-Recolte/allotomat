using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class DefaultCashRegisterNameFromMarketGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE cr
                SET cr.Name = mg.Name
                FROM CashRegisters AS cr
                INNER JOIN (
                    SELECT crmg.CashRegisterId, MIN(crmg.MarketGroupId) AS MarketGroupId
                    FROM CashRegisterMarketGroups AS crmg
                    GROUP BY crmg.CashRegisterId
                ) AS pick ON pick.CashRegisterId = cr.Id
                INNER JOIN MarketGroups AS mg ON mg.Id = pick.MarketGroupId
                WHERE cr.Name = N'Caisse par défaut / Default cash register'");

            migrationBuilder.Sql(@"
                UPDATE tl
                SET tl.CashRegisterName = cr.Name
                FROM TransactionLogs AS tl
                INNER JOIN CashRegisters AS cr ON tl.CashRegisterId = cr.Id
                WHERE tl.CashRegisterName = N'Caisse par défaut / Default cash register'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
