using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class LoyaltyFundRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AddingFundTransaction_AvailableFund",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LoyaltyAddingFundTransactionId",
                table: "Transactions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AddingFundTransaction_AvailableFund",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LoyaltyAddingFundTransactionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions",
                column: "LoyaltyAddingFundTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions",
                column: "LoyaltyAddingFundTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }
    }
}
