using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class AddOrganizationToTransaction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrganizationId",
                table: "Transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrganizationId",
                table: "Transactions",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Organizations_OrganizationId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrganizationId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Transactions");
        }
    }
}
