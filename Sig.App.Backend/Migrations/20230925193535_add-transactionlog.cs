using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class addtransactionlog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransactionUniqueId",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransactionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionUniqueId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CardProgramCardId = table.Column<long>(type: "bigint", nullable: true),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FundTransferredFromProgramCardId = table.Column<long>(type: "bigint", nullable: true),
                    FundTransferredFromCardNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryId = table.Column<long>(type: "bigint", nullable: true),
                    BeneficiaryID1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryID2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryFirstname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryLastname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BeneficiaryIsOffPlatform = table.Column<bool>(type: "bit", nullable: false),
                    BeneficiaryTypeId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionInitiatorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TransactionInitiatorFirstname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionInitiatorLastname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionInitiatorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarketId = table.Column<long>(type: "bigint", nullable: true),
                    MarketName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionLogProductGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionLogId = table.Column<long>(type: "bigint", nullable: false),
                    ProductGroupId = table.Column<long>(type: "bigint", nullable: false),
                    ProductGroupName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionLogProductGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionLogProductGroups_TransactionLogs_TransactionLogId",
                        column: x => x.TransactionLogId,
                        principalTable: "TransactionLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogProductGroups_ProductGroupId",
                table: "TransactionLogProductGroups",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogProductGroups_TransactionLogId",
                table: "TransactionLogProductGroups",
                column: "TransactionLogId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_BeneficiaryId",
                table: "TransactionLogs",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_BeneficiaryTypeId",
                table: "TransactionLogs",
                column: "BeneficiaryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_MarketId",
                table: "TransactionLogs",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_OrganizationId",
                table: "TransactionLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_ProjectId",
                table: "TransactionLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_SubscriptionId",
                table: "TransactionLogs",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_TransactionInitiatorId",
                table: "TransactionLogs",
                column: "TransactionInitiatorId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionLogs_TransactionUniqueId",
                table: "TransactionLogs",
                column: "TransactionUniqueId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionLogProductGroups");

            migrationBuilder.DropTable(
                name: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "TransactionUniqueId",
                table: "Transactions");
        }
    }
}
