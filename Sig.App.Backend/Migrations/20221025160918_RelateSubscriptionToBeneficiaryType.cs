using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class RelateSubscriptionToBeneficiaryType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionBeneficiaries_BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries",
                column: "BeneficiaryTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionBeneficiaries_BeneficiaryTypes_BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries",
                column: "BeneficiaryTypeId",
                principalTable: "BeneficiaryTypes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionBeneficiaries_BeneficiaryTypes_BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionBeneficiaries_BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropColumn(
                name: "BeneficiaryTypeId",
                table: "SubscriptionBeneficiaries");
        }
    }
}
