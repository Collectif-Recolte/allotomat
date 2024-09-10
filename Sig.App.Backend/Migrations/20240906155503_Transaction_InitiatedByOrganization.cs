using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class Transaction_InitiatedByOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InitiatedByOrganization",
                table: "Transactions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentTransaction_InitiatedByOrganization",
                table: "Transactions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InitiatedByOrganization",
                table: "TransactionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitiatedByOrganization",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PaymentTransaction_InitiatedByOrganization",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InitiatedByOrganization",
                table: "TransactionLogs");
        }
    }
}
