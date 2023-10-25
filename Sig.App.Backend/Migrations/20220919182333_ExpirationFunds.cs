using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class ExpirationFunds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableFund",
                table: "Transactions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FundsExpirationDate",
                table: "Subscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AddingFundTransactionPaymentTransaction",
                columns: table => new
                {
                    TransactionsId = table.Column<long>(type: "bigint", nullable: false),
                    TransactionsId1 = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddingFundTransactionPaymentTransaction", x => new { x.TransactionsId, x.TransactionsId1 });
                    table.ForeignKey(
                        name: "FK_AddingFundTransactionPaymentTransaction_Transactions_TransactionsId",
                        column: x => x.TransactionsId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AddingFundTransactionPaymentTransaction_Transactions_TransactionsId1",
                        column: x => x.TransactionsId1,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddingFundTransactionPaymentTransaction_TransactionsId1",
                table: "AddingFundTransactionPaymentTransaction",
                column: "TransactionsId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddingFundTransactionPaymentTransaction");

            migrationBuilder.DropColumn(
                name: "AvailableFund",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FundsExpirationDate",
                table: "Subscriptions");
        }
    }
}
