using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class BeneficiaryType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionBeneficiaries_SubscriptionTypes_TypeId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionBeneficiaries_TypeId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropColumn(
                name: "TypeId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.AddColumn<long>(
                name: "BeneficiaryTypeId",
                table: "SubscriptionTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "BeneficiaryTypeId",
                table: "Beneficiaries",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BeneficiaryTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keys = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeneficiaryTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeneficiaryTypes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTypes_BeneficiaryTypeId",
                table: "SubscriptionTypes",
                column: "BeneficiaryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_BeneficiaryTypeId",
                table: "Beneficiaries",
                column: "BeneficiaryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BeneficiaryTypes_ProjectId",
                table: "BeneficiaryTypes",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beneficiaries_BeneficiaryTypes_BeneficiaryTypeId",
                table: "Beneficiaries",
                column: "BeneficiaryTypeId",
                principalTable: "BeneficiaryTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionTypes_BeneficiaryTypes_BeneficiaryTypeId",
                table: "SubscriptionTypes",
                column: "BeneficiaryTypeId",
                principalTable: "BeneficiaryTypes",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beneficiaries_BeneficiaryTypes_BeneficiaryTypeId",
                table: "Beneficiaries");

            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionTypes_BeneficiaryTypes_BeneficiaryTypeId",
                table: "SubscriptionTypes");

            migrationBuilder.DropTable(
                name: "BeneficiaryTypes");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionTypes_BeneficiaryTypeId",
                table: "SubscriptionTypes");

            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_BeneficiaryTypeId",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "BeneficiaryTypeId",
                table: "SubscriptionTypes");

            migrationBuilder.DropColumn(
                name: "BeneficiaryTypeId",
                table: "Beneficiaries");

            migrationBuilder.AddColumn<long>(
                name: "TypeId",
                table: "SubscriptionBeneficiaries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionBeneficiaries_TypeId",
                table: "SubscriptionBeneficiaries",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionBeneficiaries_SubscriptionTypes_TypeId",
                table: "SubscriptionBeneficiaries",
                column: "TypeId",
                principalTable: "SubscriptionTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
