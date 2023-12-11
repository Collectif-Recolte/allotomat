using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class RefundTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "InitialTransactionId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "PaymentTransactionProductGroups",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RefundTransactionProductGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RefundTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    PaymentTransactionProductGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductGroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundTransactionProductGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundTransactionProductGroups_PaymentTransactionProductGroups_PaymentTransactionProductGroupId",
                        column: x => x.PaymentTransactionProductGroupId,
                        principalTable: "PaymentTransactionProductGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundTransactionProductGroups_ProductGroups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "ProductGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RefundTransactionProductGroups_Transactions_RefundTransactionId",
                        column: x => x.RefundTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InitialTransactionId",
                table: "Transactions",
                column: "InitialTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactionProductGroups_PaymentTransactionProductGroupId",
                table: "RefundTransactionProductGroups",
                column: "PaymentTransactionProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactionProductGroups_ProductGroupId",
                table: "RefundTransactionProductGroups",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundTransactionProductGroups_RefundTransactionId",
                table: "RefundTransactionProductGroups",
                column: "RefundTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Transactions_InitialTransactionId",
                table: "Transactions",
                column: "InitialTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Transactions_InitialTransactionId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "RefundTransactionProductGroups");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_InitialTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InitialTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "PaymentTransactionProductGroups");
        }
    }
}
