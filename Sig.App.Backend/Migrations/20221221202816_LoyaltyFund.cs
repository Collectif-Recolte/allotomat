using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class LoyaltyFund : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationId",
                table: "Transactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

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

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyFund",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions",
                column: "LoyaltyAddingFundTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_LoyaltyAddingFundTransactionId",
                table: "Transactions",
                column: "LoyaltyAddingFundTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions");

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

            migrationBuilder.DropColumn(
                name: "LoyaltyFund",
                table: "Cards");

            migrationBuilder.AlterColumn<long>(
                name: "OrganizationId",
                table: "Transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
