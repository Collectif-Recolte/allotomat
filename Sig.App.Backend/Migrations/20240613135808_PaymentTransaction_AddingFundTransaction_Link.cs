using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class PaymentTransaction_AddingFundTransaction_Link : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentTransactionAddingFundTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    AddingFundTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactionAddingFundTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactionAddingFundTransactions_Transactions_AddingFundTransactionId",
                        column: x => x.AddingFundTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransactionAddingFundTransactions_Transactions_PaymentTransactionId",
                        column: x => x.PaymentTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionAddingFundTransactions_AddingFundTransactionId",
                table: "PaymentTransactionAddingFundTransactions",
                column: "AddingFundTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactionAddingFundTransactions_PaymentTransactionId",
                table: "PaymentTransactionAddingFundTransactions",
                column: "PaymentTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactionAddingFundTransactions");
        }
    }
}
