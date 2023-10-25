using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class BudgetAllowance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BudgetAllowanceId",
                table: "SubscriptionBeneficiaries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BudgetAllowances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFund = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvailableFund = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetAllowances_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BudgetAllowances_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionBeneficiaries_BudgetAllowanceId",
                table: "SubscriptionBeneficiaries",
                column: "BudgetAllowanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAllowances_OrganizationId",
                table: "BudgetAllowances",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAllowances_SubscriptionId",
                table: "BudgetAllowances",
                column: "SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionBeneficiaries_BudgetAllowances_BudgetAllowanceId",
                table: "SubscriptionBeneficiaries",
                column: "BudgetAllowanceId",
                principalTable: "BudgetAllowances",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionBeneficiaries_BudgetAllowances_BudgetAllowanceId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropTable(
                name: "BudgetAllowances");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionBeneficiaries_BudgetAllowanceId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropColumn(
                name: "BudgetAllowanceId",
                table: "SubscriptionBeneficiaries");
        }
    }
}
