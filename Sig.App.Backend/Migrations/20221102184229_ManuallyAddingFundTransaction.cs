using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class ManuallyAddingFundTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_SubscriptionTypes_SubscriptionTypeId",
                table: "Transactions");

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SubscriptionId",
                table: "Transactions",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Subscriptions_SubscriptionId",
                table: "Transactions",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_SubscriptionTypes_SubscriptionTypeId",
                table: "Transactions",
                column: "SubscriptionTypeId",
                principalTable: "SubscriptionTypes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Subscriptions_SubscriptionId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_SubscriptionTypes_SubscriptionTypeId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_SubscriptionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "Transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_SubscriptionTypes_SubscriptionTypeId",
                table: "Transactions",
                column: "SubscriptionTypeId",
                principalTable: "SubscriptionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
