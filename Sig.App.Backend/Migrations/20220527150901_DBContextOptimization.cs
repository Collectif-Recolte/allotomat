using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class DBContextOptimization : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardBeneficiaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionBeneficiaries",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionBeneficiaries_BeneficiaryId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMarkets",
                table: "ProjectMarkets");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMarkets_MarketId",
                table: "ProjectMarkets");

            migrationBuilder.AddColumn<long>(
                name: "CardId",
                table: "Beneficiaries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionBeneficiaries",
                table: "SubscriptionBeneficiaries",
                columns: new[] { "BeneficiaryId", "SubscriptionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMarkets",
                table: "ProjectMarkets",
                columns: new[] { "MarketId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionBeneficiaries_SubscriptionId",
                table: "SubscriptionBeneficiaries",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMarkets_ProjectId",
                table: "ProjectMarkets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_CardId",
                table: "Beneficiaries",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_Beneficiaries_Cards_CardId",
                table: "Beneficiaries",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Beneficiaries_Cards_CardId",
                table: "Beneficiaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionBeneficiaries",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionBeneficiaries_SubscriptionId",
                table: "SubscriptionBeneficiaries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectMarkets",
                table: "ProjectMarkets");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMarkets_ProjectId",
                table: "ProjectMarkets");

            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_CardId",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "Beneficiaries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionBeneficiaries",
                table: "SubscriptionBeneficiaries",
                columns: new[] { "SubscriptionId", "BeneficiaryId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectMarkets",
                table: "ProjectMarkets",
                columns: new[] { "ProjectId", "MarketId" });

            migrationBuilder.CreateTable(
                name: "CardBeneficiaries",
                columns: table => new
                {
                    CardId = table.Column<long>(type: "bigint", nullable: false),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardBeneficiaries", x => new { x.CardId, x.BeneficiaryId });
                    table.ForeignKey(
                        name: "FK_CardBeneficiaries_Beneficiaries_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "Beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardBeneficiaries_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionBeneficiaries_BeneficiaryId",
                table: "SubscriptionBeneficiaries",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMarkets_MarketId",
                table: "ProjectMarkets",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBeneficiaries_BeneficiaryId",
                table: "CardBeneficiaries",
                column: "BeneficiaryId",
                unique: true);
        }
    }
}
