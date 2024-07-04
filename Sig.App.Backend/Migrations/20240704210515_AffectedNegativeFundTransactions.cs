using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AffectedNegativeFundTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AddingFundTransactionManuallyAddingFundTransaction",
                columns: table => new
                {
                    AffectedNegativeFundTransactionsId = table.Column<long>(type: "bigint", nullable: false),
                    ManuallNegativeFundTransactionsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddingFundTransactionManuallyAddingFundTransaction", x => new { x.AffectedNegativeFundTransactionsId, x.ManuallNegativeFundTransactionsId });
                    table.ForeignKey(
                        name: "FK_AddingFundTransactionManuallyAddingFundTransaction_Transactions_AffectedNegativeFundTransactionsId",
                        column: x => x.AffectedNegativeFundTransactionsId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AddingFundTransactionManuallyAddingFundTransaction_Transactions_ManuallNegativeFundTransactionsId",
                        column: x => x.ManuallNegativeFundTransactionsId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddingFundTransactionManuallyAddingFundTransaction_ManuallNegativeFundTransactionsId",
                table: "AddingFundTransactionManuallyAddingFundTransaction",
                column: "ManuallNegativeFundTransactionsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddingFundTransactionManuallyAddingFundTransaction");
        }
    }
}
