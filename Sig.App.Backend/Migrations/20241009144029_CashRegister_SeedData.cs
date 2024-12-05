using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CashRegister_SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO MarketGroups (ProjectId, Name, IsArchived) SELECT p.Id, 'Groupe de commerce par défaut', 0 FROM Projects p WHERE p.Id NOT IN (SELECT mg.ProjectId FROM MarketGroups mg)");

            migrationBuilder.Sql("INSERT INTO MarketGroupMarkets (MarketGroupId, MarketId) SELECT mg.Id AS MarketGroupId, pm.MarketId FROM ProjectMarkets pm JOIN MarketGroups mg ON mg.ProjectId = pm.ProjectId");

            migrationBuilder.Sql("INSERT INTO CashRegisters (Name, MarketId, IsArchived) SELECT 'Caisse par défaut', m.Id, 0 FROM Markets m WHERE m.Id NOT IN (SELECT cr.MarketId FROM CashRegisters cr)");

            migrationBuilder.Sql("INSERT INTO CashRegisterMarketGroups (MarketGroupId, CashRegisterId) SELECT mg.Id AS MarketGroupId, cr.Id AS CashRegisterId FROM MarketGroupMarkets mgm JOIN CashRegisters cr ON cr.MarketId = mgm.MarketId JOIN MarketGroups mg ON mg.Id = mgm.MarketGroupId WHERE cr.Name = 'Caisse par défaut'");

            migrationBuilder.Sql("UPDATE tl SET tl.CashRegisterId = cr.Id, tl.CashRegisterName = cr.Name FROM TransactionLogs tl JOIN CashRegisters cr ON tl.MarketId = cr.MarketId");

            migrationBuilder.Sql("UPDATE t SET t.PaymentTransaction_CashRegisterId = cr.Id FROM Transactions t JOIN CashRegisters cr ON t.MarketId = cr.MarketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
