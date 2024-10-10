using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CashRegister_Transaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CashRegisterId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentTransaction_CashRegisterId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CashRegisterId",
                table: "TransactionLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashRegisterName",
                table: "TransactionLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CashRegisterId",
                table: "Transactions",
                column: "CashRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaymentTransaction_CashRegisterId",
                table: "Transactions",
                column: "PaymentTransaction_CashRegisterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_CashRegisters_CashRegisterId",
                table: "Transactions",
                column: "CashRegisterId",
                principalTable: "CashRegisters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_CashRegisters_PaymentTransaction_CashRegisterId",
                table: "Transactions",
                column: "PaymentTransaction_CashRegisterId",
                principalTable: "CashRegisters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_CashRegisters_CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_CashRegisters_PaymentTransaction_CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PaymentTransaction_CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentTransaction_CashRegisterId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CashRegisterId",
                table: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "CashRegisterName",
                table: "TransactionLogs");
        }
    }
}
