using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class TransactionProductGroupAndLoyaltyProductGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fund",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "LoyaltyFund",
                table: "Cards");

            migrationBuilder.AddColumn<long>(
                name: "ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProductGroupId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Funds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardId = table.Column<long>(type: "bigint", nullable: true),
                    ProductGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Funds_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Funds_ProductGroups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "ProductGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactionProductGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductGroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactionProductGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactionProductGroups_ProductGroups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "ProductGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentTransactionProductGroups_Transactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "ManuallyAddingFundTransaction_ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ProductGroupId",
                table: "Transactions",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "SubscriptionAddingFundTransaction_ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Funds_CardId",
                table: "Funds",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_Funds_ProductGroupId",
                table: "Funds",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionProductGroups_PaymentTransactionId",
                table: "PaymentTransactionProductGroups",
                column: "PaymentTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionProductGroups_ProductGroupId",
                table: "PaymentTransactionProductGroups",
                column: "ProductGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ProductGroups_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "ManuallyAddingFundTransaction_ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ProductGroups_ProductGroupId",
                table: "Transactions",
                column: "ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ProductGroups_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "SubscriptionAddingFundTransaction_ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ProductGroups_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ProductGroups_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ProductGroups_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "Funds");

            migrationBuilder.DropTable(
                name: "PaymentTransactionProductGroups");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.AddColumn<decimal>(
                name: "Fund",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LoyaltyFund",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
