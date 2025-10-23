using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class BudgetAllowanceLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetAllowanceLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Discriminator = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BudgetAllowanceId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubscriptionId = table.Column<long>(type: "bigint", nullable: true),
                    SubscriptionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetBudgetAllowanceId = table.Column<long>(type: "bigint", nullable: false),
                    TargetOrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    TargetOrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetSubscriptionId = table.Column<long>(type: "bigint", nullable: true),
                    TargetSubscriptionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatorFirstname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatorLastname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InitiatorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAllowanceLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAllowanceLogs");
        }
    }
}
