using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class OffPlatformBeneficiary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ProductGroups_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_ProductGroups_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions");

            migrationBuilder.AddColumn<bool>(
                name: "AdministrationSubscriptionsOffPlatform",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Beneficiaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Beneficiaries",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyPaymentMoment",
                table: "Beneficiaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Beneficiaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentFunds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductGroupId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentFunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentFunds_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentFunds_ProductGroups_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "ProductGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFunds_BeneficiaryId",
                table: "PaymentFunds",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentFunds_ProductGroupId",
                table: "PaymentFunds",
                column: "ProductGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentFunds");

            migrationBuilder.DropColumn(
                name: "AdministrationSubscriptionsOffPlatform",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "MonthlyPaymentMoment",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Beneficiaries");

            migrationBuilder.AddColumn<long>(
                name: "ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "ManuallyAddingFundTransaction_ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "SubscriptionAddingFundTransaction_ProductGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ProductGroups_ManuallyAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "ManuallyAddingFundTransaction_ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_ProductGroups_SubscriptionAddingFundTransaction_ProductGroupId",
                table: "Transactions",
                column: "SubscriptionAddingFundTransaction_ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");
        }
    }
}
