using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class ExpiredFundTransaction_AddingFundTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AddingFundTransactionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExpireFundTransactionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ExpiredSubscriptionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExpiredSubscriptionId",
                table: "Transactions",
                column: "ExpiredSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExpireFundTransactionId",
                table: "Transactions",
                column: "ExpireFundTransactionId",
                unique: true,
                filter: "[ExpireFundTransactionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Subscriptions_ExpiredSubscriptionId",
                table: "Transactions",
                column: "ExpiredSubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_ExpireFundTransactionId",
                table: "Transactions",
                column: "ExpireFundTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Subscriptions_ExpiredSubscriptionId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_ExpireFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExpiredSubscriptionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ExpireFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "AddingFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExpireFundTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExpiredSubscriptionId",
                table: "Transactions");
        }
    }
}
