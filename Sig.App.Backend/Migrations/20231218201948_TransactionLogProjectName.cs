using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class TransactionLogProjectName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PaymentTransaction_InitiatedByProject",
                table: "Transactions",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InitiatedByProject",
                table: "TransactionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "TransactionLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentTransaction_InitiatedByProject",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "InitiatedByProject",
                table: "TransactionLogs");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "TransactionLogs");
        }
    }
}
